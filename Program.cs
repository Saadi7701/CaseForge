using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CaseForgeAI.Core.Entities;
using CaseForgeAI.Core.Entities.Identity;
using CaseForgeAI.Core.Interfaces;
using CaseForgeAI.Infrastructure.Data;
using CaseForgeAI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String Setup (LocalDB is default for local Visual Studio run)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=CaseForgeAI;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Identity Service Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication (Cookies for Razor Views, JWT for APIs if requested)
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_FOR_CASEFORGE_AI_1234567890";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CaseForgeAI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CaseForgeAI",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 3. DI Registrations
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<GameplayService>();
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Database Setup & Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Automatically apply migrations and create database
        context.Database.Migrate();

        // Seed Roles
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roleNames = { "Admin", "Player" };
        foreach (var roleName in roleNames)
        {
            if (!roleManager.RoleExistsAsync(roleName).Result)
            {
                roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
            }
        }

        // Seed Admin User
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@caseforge.com";
        var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Chief Inspector",
                DetectiveRank = "Master Detective",
                EmailConfirmed = true
            };
            var createAdmin = userManager.CreateAsync(user, "NoirAdmin123!").Result;
            if (createAdmin.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Admin").Wait();
            }
        }

        // Seed Player User
        var playerEmail = "player@caseforge.com";
        var playerUser = userManager.FindByEmailAsync(playerEmail).Result;
        if (playerUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = playerEmail,
                Email = playerEmail,
                FullName = "Sherlock Holmes",
                DetectiveRank = "Novice Sleuth",
                EmailConfirmed = true
            };
            var createPlayer = userManager.CreateAsync(user, "Player123!").Result;
            if (createPlayer.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Player").Wait();
            }
        }

        // Seed an Initial Case
        if (!context.Stories.Any())
        {
            var story = new CaseStory
            {
                Title = "The Sterling Safe Heist",
                Description = "Lord Sterling's study safe was cleaned out overnight. A staged break-in hides a deeper inside job.",
                VictimName = "Lord Alistair Sterling",
                CrimeSceneDescription = "A luxurious oak-paneled study in Sterling Manor. Glass fragments from the balcony door glitter on the Persian rug. An empty gold-accented safe door hangs ajar.",
                Difficulty = "Medium",
                IsPublished = true,
                QualityScore = 9.2
            };

            context.Stories.Add(story);
            context.SaveChanges();

            // Suspects
            var s1 = new Suspect("Charles Sterling", "The estranged disinherited nephew. Heavily in debt and smells of imported whiskey.", "I was at the Blackwood Gentlemen's Club in London all evening.", "Desperately needed funds to pay off underground bookmakers.", false, "/images/suspect1.jpg") { StoryId = story.Id };
            var s2 = new Suspect("Julian Sterling", "The estate gardener, who is secretly Lord Sterling's unrecognized son. Has hands stained with gold-leaf dye.", "I was working late in the greenhouse, potting winter flora.", "Lord Sterling discovered his identity and threatened to disown him.", true, "/images/suspect2.jpg") { StoryId = story.Id };
            var s3 = new Suspect("Eleanor Vance", "Personal Secretary. Quiet, calculating, and carries herself with a poise that suggests she knows all the estate secrets.", "I was in my quarters reviewing the yearly accounts ledger.", "Expected to inherit a substantial portion of the family jewels.", false, "/images/suspect3.jpg") { StoryId = story.Id };

            context.Suspects.AddRange(s1, s2, s3);

            // Clues
            var c1 = new Clue { StoryId = story.Id, Name = "Balcony Shattered Glass", Description = "Shard of glass showing impact lines coming from the INSIDE of the room, suggesting a staged break-in.", LocationName = "Study Balcony Door", ClueType = "Physical", IsHidden = false, ConnectionInfo = "Staged break-in by someone already inside.", HotspotX = 18, HotspotY = 70 };
            var c2 = new Clue { StoryId = story.Id, Name = "Golden Dye Stains", Description = "Traces of a rare gold dye, the exact brand used to polish the safe's intricate brass details, found on a hand-drawn map.", LocationName = "Greenhouse Path", ClueType = "Physical", IsHidden = true, ConnectionInfo = "Connects the thief directly to Julian's dye stains.", HotspotX = 72, HotspotY = 35 };
            var c3 = new Clue { StoryId = story.Id, Name = "Club Guest Ledger Page", Description = "A torn page from the Blackwood Club guests ledger showing Charles Sterling's signature was entered at 11:30 PM, but crossed out.", LocationName = "Library Rubbish Bin", ClueType = "Document", IsHidden = false, ConnectionInfo = "Exposes Charles' fake alibi.", HotspotX = 45, HotspotY = 82 };

            context.Clues.AddRange(c1, c2, c3);

            // Puzzles
            var p1 = new PuzzleEntity
            {
                StoryId = story.Id,
                Title = "The Safe Lock Decipher",
                PuzzleType = "Cipher",
                Question = "Decrypt the safe combination code: 'KHOO HP'. It uses a Caesar Cipher of shift 3 (shift backward by 3 letters).",
                CorrectAnswer = "HELP ME",
                Hint = "Subtract 3 from the alphabetical positions of K-H-O-O H-P.",
                PointsValue = 250,
                PuzzleDataJson = "{\"shift\":3}"
            };

            var p2 = new PuzzleEntity
            {
                StoryId = story.Id,
                Title = "Timeline Reconstruction",
                PuzzleType = "Timeline",
                Question = "Reconstruct the chronological timeline of events (Enter order e.g. 1,2,3). Events: [1] The Safe is opened, [2] Lord Sterling enters study, [3] Staged window break-in.",
                CorrectAnswer = "2,1,3",
                Hint = "The staging of the break-in was the last thing the thief did to hide their traces after Lord Sterling was incapacitated.",
                PointsValue = 300,
                PuzzleDataJson = "{\"events\":[\"Lord Sterling enters\",\"Safe is opened\",\"Window broken\"]}"
            };

            context.Puzzles.AddRange(p1, p2);

            // Analytics
            var analytics = new Analytics { StoryId = story.Id, TotalPlays = 120, SolvedCount = 85, AverageScore = 1150.0, AverageSolveTimeSeconds = 480.0 };
            context.Analytics.Add(analytics);

            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during database migration or seeding: {ex.Message}");
    }
}

// 5. HTTP Request Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

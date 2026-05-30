using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Core.Entities;
using CaseForgeAI.Core.Entities.Identity;

namespace CaseForgeAI.Infrastructure.Data
{
    // Inheritance: ApplicationDbContext inherits from IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CaseStory> Stories { get; set; } = null!;
        public DbSet<StoryVersion> StoryVersions { get; set; } = null!;
        public DbSet<Suspect> Suspects { get; set; } = null!;
        public DbSet<Clue> Clues { get; set; } = null!;
        public DbSet<PuzzleEntity> Puzzles { get; set; } = null!;
        public DbSet<PlayerProgress> PlayerProgresses { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<EvidenceLink> EvidenceLinks { get; set; } = null!;
        public DbSet<Analytics> Analytics { get; set; } = null!;
        public DbSet<StoryRating> StoryRatings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure cascade deletes or key setups
            builder.Entity<CaseStory>()
                .HasMany(s => s.Suspects)
                .WithOne(su => su.Story)
                .HasForeignKey(su => su.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CaseStory>()
                .HasMany(s => s.Clues)
                .WithOne(c => c.Story)
                .HasForeignKey(c => c.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CaseStory>()
                .HasMany(s => s.Puzzles)
                .WithOne(p => p.Story)
                .HasForeignKey(p => p.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CaseStory>()
                .HasMany(s => s.Versions)
                .WithOne(v => v.Story)
                .HasForeignKey(v => v.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlayerProgress>()
                .HasMany(p => p.Inventory)
                .WithOne(i => i.PlayerProgress)
                .HasForeignKey(i => i.PlayerProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlayerProgress>()
                .HasMany(p => p.EvidenceLinks)
                .WithOne(l => l.PlayerProgress)
                .HasForeignKey(l => l.PlayerProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            // Turn off multiple cascade paths if needed
            builder.Entity<EvidenceLink>()
                .HasOne(el => el.ClueA)
                .WithMany()
                .HasForeignKey(el => el.ClueIdA)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<EvidenceLink>()
                .HasOne(el => el.ClueB)
                .WithMany()
                .HasForeignKey(el => el.ClueIdB)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<InventoryItem>()
                .HasOne(i => i.Clue)
                .WithMany()
                .HasForeignKey(i => i.ClueId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

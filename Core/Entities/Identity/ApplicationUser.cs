using Microsoft.AspNetCore.Identity;

namespace CaseForgeAI.Core.Entities.Identity
{
    // Inheritance: ApplicationUser inherits from IdentityUser (ASP.NET Identity)
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        
        public int TotalScore { get; set; } = 0;
        
        public string DetectiveRank { get; set; } = "Novice Sleuth"; // Novice Sleuth, Senior Inspector, Master Detective
        
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseForgeAI.Core.OOP.Characters;

namespace CaseForgeAI.Core.Entities
{
    // Inheritance: Suspect is a Character
    [Table("Suspects")]
    public class Suspect : Character
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid StoryId { get; set; }
        
        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        public string Alibi { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Motive { get; set; } = string.Empty;
        public bool IsKiller { get; set; }
        public string AvatarUrl { get; set; } = "/images/default_suspect.jpg";

        // Constructor calling base abstract constructor
        public Suspect() : base("Unknown Suspect", "No background details available.")
        {
        }

        public Suspect(string name, string description, string alibi, string motive, bool isKiller, string avatarUrl) 
            : base(name, description)
        {
            Alibi = alibi;
            Motive = motive;
            IsKiller = isKiller;
            AvatarUrl = avatarUrl;
        }

        // Method Overriding (Polymorphism)
        public override string Speak()
        {
            return $"Under interrogation, {Name} says: \"{Alibi}\" They appear anxious.";
        }
    }
}

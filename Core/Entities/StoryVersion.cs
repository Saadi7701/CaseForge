using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("StoryVersions")]
    public class StoryVersion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        public int VersionNumber { get; set; }

        [Required]
        public string ContentJson { get; set; } = string.Empty; // Complete generated JSON

        public string PromptUsed { get; set; } = string.Empty; // Feedback prompt given by admin

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = "System";
    }
}

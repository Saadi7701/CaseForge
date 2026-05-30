using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("StoryRatings")]
    public class StoryRating
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Review { get; set; } = string.Empty;

        public DateTime RatedAt { get; set; } = DateTime.UtcNow;
    }
}

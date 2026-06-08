using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("Clues")]
    public class Clue
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LocationName { get; set; } = "Crime Scene";

        [Required]
        [MaxLength(50)]
        public string ClueType { get; set; } = "Physical"; // Physical, Document, Testimonial

        public bool IsHidden { get; set; } = false;

        public bool IsCorrect { get; set; } = false;

        public string ConnectionInfo { get; set; } = string.Empty;

        // Hotspot coordinates for visual investigation scene (normalized 0-100 values)
        public int HotspotX { get; set; } = 50;
        public int HotspotY { get; set; } = 50;
    }
}

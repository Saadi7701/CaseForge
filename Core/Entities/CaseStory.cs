using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("Stories")]
    public class CaseStory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string VictimName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string CrimeSceneDescription { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quality score computed from AI content quality
        public double QualityScore { get; set; } = 5.0;

        // The case solution/ending narrative
        public string Ending { get; set; } = string.Empty;

        // Composition: CaseStory is composed of suspects, clues, and puzzles
        public virtual ICollection<Suspect> Suspects { get; set; } = new List<Suspect>();
        public virtual ICollection<Clue> Clues { get; set; } = new List<Clue>();
        public virtual ICollection<PuzzleEntity> Puzzles { get; set; } = new List<PuzzleEntity>();
        
        public virtual ICollection<StoryVersion> Versions { get; set; } = new List<StoryVersion>();
        public virtual ICollection<Analytics> AnalyticsRecords { get; set; } = new List<Analytics>();
        public virtual ICollection<StoryRating> Ratings { get; set; } = new List<StoryRating>();
    }
}

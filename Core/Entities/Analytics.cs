using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("Analytics")]
    public class Analytics
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        public int TotalPlays { get; set; } = 0;

        public int SolvedCount { get; set; } = 0;

        public double AverageScore { get; set; } = 0.0;

        public double AverageSolveTimeSeconds { get; set; } = 0.0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

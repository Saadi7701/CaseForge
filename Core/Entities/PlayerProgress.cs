using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("PlayerProgress")]
    public class PlayerProgress
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        public int Score { get; set; } = 1000; // Starts with high score, decreases with hints

        [Required]
        [MaxLength(50)]
        public string CurrentStage { get; set; } = "Investigation"; // Investigation, Interrogation, Puzzles, Accusation, Finished

        public bool IsCompleted { get; set; } = false;

        public bool CaseSolved { get; set; } = false;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

        // Composition / Aggregation
        public virtual ICollection<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        public virtual ICollection<EvidenceLink> EvidenceLinks { get; set; } = new List<EvidenceLink>();
    }
}

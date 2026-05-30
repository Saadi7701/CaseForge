using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("InventoryItems")]
    public class InventoryItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PlayerProgressId { get; set; }

        [ForeignKey("PlayerProgressId")]
        public virtual PlayerProgress? PlayerProgress { get; set; }

        public Guid ClueId { get; set; }

        [ForeignKey("ClueId")]
        public virtual Clue? Clue { get; set; }

        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    }
}

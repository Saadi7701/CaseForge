using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseForgeAI.Core.Entities
{
    [Table("EvidenceLinks")]
    public class EvidenceLink
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PlayerProgressId { get; set; }

        [ForeignKey("PlayerProgressId")]
        public virtual PlayerProgress? PlayerProgress { get; set; }

        public Guid ClueIdA { get; set; }

        [ForeignKey("ClueIdA")]
        public virtual Clue? ClueA { get; set; }

        public Guid ClueIdB { get; set; }

        [ForeignKey("ClueIdB")]
        public virtual Clue? ClueB { get; set; }

        [MaxLength(500)]
        public string LinkNotes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

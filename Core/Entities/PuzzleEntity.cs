using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseForgeAI.Core.OOP.Puzzles;

namespace CaseForgeAI.Core.Entities
{
    [Table("Puzzles")]
    public class PuzzleEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoryId { get; set; }

        [ForeignKey("StoryId")]
        public virtual CaseStory? Story { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;

        public string Hint { get; set; } = string.Empty;

        public int PointsValue { get; set; } = 100;

        [Required]
        [MaxLength(50)]
        public string PuzzleType { get; set; } = "Cipher"; // Cipher, Timeline, Logic

        public string PuzzleDataJson { get; set; } = "{}";

        // Factory Method showing Polymorphism and mapping DB to OOP domain
        public Puzzle ToConcretePuzzle()
        {
            if (string.Equals(PuzzleType, "Cipher", StringComparison.OrdinalIgnoreCase))
            {
                return new CipherPuzzle(Title, Question, CorrectAnswer, Hint, PointsValue, PuzzleDataJson);
            }
            else if (string.Equals(PuzzleType, "Timeline", StringComparison.OrdinalIgnoreCase))
            {
                return new TimelinePuzzle(Title, Question, CorrectAnswer, Hint, PointsValue, PuzzleDataJson);
            }
            else
            {
                return new BasicPuzzle(Title, Question, CorrectAnswer, Hint, PointsValue);
            }
        }
    }

    // Concrete OOP Puzzle Type 1: Cipher
    public class CipherPuzzle : Puzzle
    {
        public string ShiftOrKey { get; set; } = string.Empty;

        public CipherPuzzle(string title, string question, string correctAnswer, string hint, int pointsValue, string dataJson)
            : base(title, question, correctAnswer, hint, pointsValue)
        {
            ShiftOrKey = dataJson; // simple store
        }

        // Method Overriding (Polymorphism)
        public override bool VerifyAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;
            IsSolved = string.Equals(answer.Trim(), CorrectAnswer, StringComparison.OrdinalIgnoreCase);
            return IsSolved;
        }
    }

    // Concrete OOP Puzzle Type 2: Timeline
    public class TimelinePuzzle : Puzzle
    {
        public string TimelineEventsJson { get; set; }

        public TimelinePuzzle(string title, string question, string correctAnswer, string hint, int pointsValue, string dataJson)
            : base(title, question, correctAnswer, hint, pointsValue)
        {
            TimelineEventsJson = dataJson;
        }

        // Method Overriding (Polymorphism)
        public override bool VerifyAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;
            // E.g. answer can be a comma-separated order, compared case-insensitively
            var cleanedAnswer = answer.Replace(" ", "").Trim();
            var cleanedCorrect = CorrectAnswer.Replace(" ", "").Trim();
            IsSolved = string.Equals(cleanedAnswer, cleanedCorrect, StringComparison.OrdinalIgnoreCase);
            return IsSolved;
        }
    }

    // Concrete OOP Puzzle Type 3: Fallback basic riddle
    public class BasicPuzzle : Puzzle
    {
        public BasicPuzzle(string title, string question, string correctAnswer, string hint, int pointsValue)
            : base(title, question, correctAnswer, hint, pointsValue)
        {
        }

        public override bool VerifyAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;
            IsSolved = string.Equals(answer.Trim(), CorrectAnswer, StringComparison.OrdinalIgnoreCase);
            return IsSolved;
        }
    }
}

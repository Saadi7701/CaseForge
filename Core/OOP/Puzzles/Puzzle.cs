using System;
using CaseForgeAI.Core.Interfaces;

namespace CaseForgeAI.Core.OOP.Puzzles
{
    // Inheritance, Abstraction & Interfaces (OOP)
    public abstract class Puzzle : ISolvable
    {
        private string _title = string.Empty;
        private string _question = string.Empty;
        private string _correctAnswer = string.Empty;
        private string _hint = string.Empty;
        private int _pointsValue;

        protected Puzzle(string title, string question, string correctAnswer, string hint, int pointsValue)
        {
            Title = title;
            Question = question;
            CorrectAnswer = correctAnswer;
            Hint = hint;
            PointsValue = pointsValue;
        }

        public string Title
        {
            get => _title;
            set => _title = string.IsNullOrWhiteSpace(value) ? "Unnamed Puzzle" : value.Trim();
        }

        public string Question
        {
            get => _question;
            set => _question = string.IsNullOrWhiteSpace(value) ? "No question prompt available." : value.Trim();
        }

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set => _correctAnswer = value?.Trim() ?? string.Empty;
        }

        public string Hint
        {
            get => _hint;
            set => _hint = string.IsNullOrWhiteSpace(value) ? "No hint is available for this riddle." : value.Trim();
        }

        public int PointsValue
        {
            get => _pointsValue;
            set => _pointsValue = value < 0 ? 0 : value;
        }

        public bool IsSolved { get; protected set; } = false;

        // Abstract method for verification: child puzzles will implement customized verification rules.
        public abstract bool VerifyAnswer(string answer);

        public virtual string GetHint()
        {
            return $"Hint: {Hint}";
        }
    }
}

namespace CaseForgeAI.Core.OOP.Characters
{
    // Abstraction (SOLID / OOP)
    public abstract class Character
    {
        // Encapsulation: private backing fields with controlled access via public properties
        private string _name = string.Empty;
        private string _description = string.Empty;

        protected Character(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unknown Person" : value.Trim();
        }

        public string Description
        {
            get => _description;
            set => _description = string.IsNullOrWhiteSpace(value) ? "No details available." : value.Trim();
        }

        // Abstract method defining polymorphic behavior for speech/interaction
        public abstract string Speak();
    }
}

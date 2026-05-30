namespace CaseForgeAI.Core.Interfaces
{
    public interface ISolvable
    {
        string Question { get; }
        string CorrectAnswer { get; }
        bool IsSolved { get; }
        bool VerifyAnswer(string answer);
        string GetHint();
    }
}

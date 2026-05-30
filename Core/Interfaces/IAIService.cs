using System.Threading.Tasks;

namespace CaseForgeAI.Core.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateStoryJsonAsync(string prompt, string? model = null);
        Task<string> RefineStoryJsonAsync(string originalStoryJson, string feedbackPrompt, string? model = null);
        double CalculateQualityScore(string storyJson);
    }
}

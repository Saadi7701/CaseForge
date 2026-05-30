using System.Threading.Tasks;

namespace CaseForgeAI.Core.Interfaces
{
    public interface IOpenRouterStrategy
    {
        string ModelName { get; }
        Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl);
    }
}

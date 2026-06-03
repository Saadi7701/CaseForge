using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CaseForgeAI.Core.Interfaces;

namespace CaseForgeAI.Services
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _defaultModel;

        public AIService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["OpenRouter:ApiKey"] ?? string.Empty;
            _baseUrl = _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            _defaultModel = _configuration["OpenRouter:DefaultModel"] ?? "google/gemma-2-9b-it:free";
        }

        public async Task<string> GenerateStoryJsonAsync(string prompt, string? model = null)
        {
            var selectedModel = model ?? _defaultModel;
            var strategy = GetStrategy(selectedModel);

            string systemPrompt = GetSystemPrompt();
            string userPrompt = $"Generate a complete detective mystery case based on this prompt: '{prompt}'";

            try
            {
                string rawJson = await strategy.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
                return CleanResponseJson(rawJson);
            }
            catch (Exception ex)
            {
                // Fallback to Mock if API call fails
                Console.WriteLine($"AI Generation failed: {ex.Message}. Falling back to mock data.");
                var mock = new MockStrategy();
                return await mock.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
            }
        }

        public async Task<string> RefineStoryJsonAsync(string originalStoryJson, string feedbackPrompt, string? model = null)
        {
            var selectedModel = model ?? _defaultModel;
            var strategy = GetStrategy(selectedModel);

            string systemPrompt = GetRefinementSystemPrompt();
            string userPrompt = $"Original Case JSON:\n{originalStoryJson}\n\nRefinement Request:\n{feedbackPrompt}";

            try
            {
                string rawJson = await strategy.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
                return CleanResponseJson(rawJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Refinement failed: {ex.Message}. Falling back to mock refined data.");
                var mock = new MockStrategy();
                return await mock.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
            }
        }

        public double CalculateQualityScore(string storyJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(storyJson);
                var root = doc.RootElement;

                double score = 10.0;

                // Validate critical structural fields
                if (!root.TryGetProperty("title", out _)) score -= 2.0;
                if (!root.TryGetProperty("victim", out _)) score -= 2.0;
                
                if (root.TryGetProperty("suspects", out var suspects) && suspects.ValueKind == JsonValueKind.Array)
                {
                    if (suspects.GetArrayLength() < 2) score -= 2.0;
                }
                else
                {
                    score -= 3.0;
                }

                if (root.TryGetProperty("clues", out var clues) && clues.ValueKind == JsonValueKind.Array)
                {
                    if (clues.GetArrayLength() < 2) score -= 2.0;
                }
                else
                {
                    score -= 3.0;
                }

                if (root.TryGetProperty("puzzles", out var puzzles) && puzzles.ValueKind == JsonValueKind.Array)
                {
                    if (puzzles.GetArrayLength() < 1) score -= 1.5;
                }

                return Math.Clamp(score, 1.0, 10.0);
            }
            catch
            {
                return 1.0; // Invalid JSON
            }
        }

        private IOpenRouterStrategy GetStrategy(string model)
        {
            // Abstraction & Polymorphism: Dynamically select strategy based on API key presence and model name
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Equals("YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                return new MockStrategy();
            }

            if (model.Contains("gemma", StringComparison.OrdinalIgnoreCase))
            {
                return new GemmaStrategy();
            }
            else if (model.Contains("llama", StringComparison.OrdinalIgnoreCase))
            {
                return new LlamaStrategy();
            }

            // Default fallback Strategy
            return new GemmaStrategy();
        }

        private static string GetSystemPrompt()
        {
            return @"You are a master mystery writer. Generate a complete detective mystery case in JSON format.
IMPORTANT: Use simple, easy-to-understand everyday English. Avoid overly complex words or confusing sentence structures so that anyone can easily read the story, clues, and suspect descriptions.
You MUST output ONLY a valid JSON object. No markdown tags, no wrap around text, no starting with ```json.
The JSON object must contain the following keys exactly:
{
  ""title"": ""Case Title (Cinematic/Noir style)"",
  ""victim"": ""Victim Name"",
  ""difficulty"": ""Easy, Medium, or Hard"",
  ""crimeSceneDescription"": ""Atmospheric description of the crime scene room including detail elements."",
  ""suspects"": [
    {
      ""name"": ""Suspect Name"",
      ""role"": ""Relationship to victim/estate (e.g. Butler, Nephew)"",
      ""description"": ""Aesthetic description including their attire, mannerism, and physical details."",
      ""alibi"": ""Their claim of where they were during the murder."",
      ""motive"": ""Their hidden reason for wanting the victim dead."",
      ""isKiller"": true or false (Only ONE suspect can be the killer),
      ""avatarUrl"": ""/images/default_suspect.jpg""
    }
  ],
  ""clues"": [
    {
      ""name"": ""Clue name (e.g. Torn Pocket Watch)"",
      ""description"": ""Detailed description of what the clue looks like and what it reveals when inspected closely."",
      ""locationName"": ""Specific section of the crime scene where it is found"",
      ""clueType"": ""Physical, Document, or Testimonial"",
      ""isHidden"": true or false (true means they must search hotspots, false means it's lying in plain sight),
      ""connectionInfo"": ""A sentence indicating how this clue links to a suspect or contradicts an alibi."",
      ""hotspotX"": 10 to 90 (represents percentage coordinate X for clicking in 100x100 crime scene room),
      ""hotspotY"": 10 to 90 (represents percentage coordinate Y for clicking)
    }
  ],
  ""puzzles"": [
    {
      ""title"": ""Puzzle Title (e.g. Decrypting the Safe)"",
      ""puzzleType"": ""Cipher or Timeline"",
      ""question"": ""A clear question. If Cipher, provide ciphertext and cipher type (e.g. Caesar or rot13). If Timeline, list 3 events out of order."",
      ""correctAnswer"": ""The decrypted text (UPPERCASE) or correct sequence list (e.g. 2,1,3)"",
      ""hint"": ""A helpful clue to guide the player without giving it away."",
      ""pointsValue"": 100 to 500,
      ""puzzleDataJson"": ""Additional JSON config if needed""
    }
  ],
  ""ending"": ""A dramatic cinematic description explaining exactly how the killer committed the crime and how the clues tie together to prove it.""
}";
        }

        private static string GetRefinementSystemPrompt()
        {
            return @"You are a master mystery editor. You will receive an existing detective case JSON and a refinement request.
Your job is to regenerate the case incorporating the refinement request (e.g. making the killer less obvious, adding a clue, editing a suspect motive) while keeping the overall JSON structure intact.
You MUST output ONLY a valid JSON object. No markdown tags, no wrap around text, no starting with ```json. Follow the exact same JSON format schema as the original.";
        }

        private static string CleanResponseJson(string raw)
        {
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            return cleaned.Trim();
        }
    }
}

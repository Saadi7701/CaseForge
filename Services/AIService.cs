using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CaseForgeAI.Core.Interfaces;

namespace CaseForgeAI.Services
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _defaultModel;
        private readonly List<string> _fallbackModels;
        private readonly bool _hasValidApiKey;

        public AIService(IConfiguration configuration, ILogger<AIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenRouter:ApiKey"] ?? string.Empty;
            _baseUrl = _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            _defaultModel = _configuration["OpenRouter:DefaultModel"] ?? "openrouter/auto";

            // Load fallback model chain from config
            _fallbackModels = _configuration.GetSection("OpenRouter:FallbackModels")
                .Get<List<string>>() ?? new List<string>();

            _hasValidApiKey = !string.IsNullOrWhiteSpace(_apiKey) &&
                              !_apiKey.Equals("YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GenerateStoryJsonAsync(string prompt, string? model = null)
        {
            string systemPrompt = GetSystemPrompt();
            string userPrompt = $"Generate a complete detective mystery case based on this prompt: '{prompt}'";

            return await ExecuteWithFallbackAsync(systemPrompt, userPrompt);
        }

        public async Task<string> RefineStoryJsonAsync(string originalStoryJson, string feedbackPrompt, string? model = null)
        {
            string systemPrompt = GetRefinementSystemPrompt();
            string userPrompt = $"Original Case JSON:\n{originalStoryJson}\n\nRefinement Request:\n{feedbackPrompt}";

            return await ExecuteWithFallbackAsync(systemPrompt, userPrompt);
        }

        /// <summary>
        /// Tries the default model first, then iterates through fallback models.
        /// If API key is configured, errors are thrown so the admin sees what went wrong.
        /// Mock fallback only used when NO API key is set at all.
        /// </summary>
        private async Task<string> ExecuteWithFallbackAsync(string systemPrompt, string userPrompt)
        {
            // If no valid API key, go straight to mock
            if (!_hasValidApiKey)
            {
                _logger.LogWarning("No valid OpenRouter API key configured. Using mock data.");
                var mock = new MockStrategy();
                return await mock.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
            }

            // Build ordered list: default model + fallbacks
            var modelsToTry = new List<string> { _defaultModel };
            modelsToTry.AddRange(_fallbackModels.Where(m => !m.Equals(_defaultModel, StringComparison.OrdinalIgnoreCase)));

            var errors = new List<string>();

            foreach (var modelName in modelsToTry)
            {
                try
                {
                    _logger.LogInformation("Attempting AI generation with model: {Model}", modelName);
                    var strategy = new OpenRouterStrategy(modelName);
                    string rawJson = await strategy.ExecutePromptAsync(systemPrompt, userPrompt, _apiKey, _baseUrl);
                    string cleaned = CleanResponseJson(rawJson);

                    // Validate that it's parseable JSON with required fields
                    using var testDoc = JsonDocument.Parse(cleaned);
                    var root = testDoc.RootElement;

                    // Basic validation: must have at least title
                    if (!root.TryGetProperty("title", out _))
                    {
                        throw new InvalidOperationException("AI response is valid JSON but missing required 'title' field. The model may not have followed the schema correctly.");
                    }

                    _logger.LogInformation("Successfully generated response with model: {Model}", modelName);
                    return cleaned;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"[{modelName}]: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogWarning(ex, "Model {Model} failed: {Error}", modelName, ex.Message);
                }
            }

            // All models failed — throw with details so the admin sees the real error
            var allErrors = string.Join("\n", errors);
            throw new InvalidOperationException(
                $"All AI models failed to generate the case. Errors:\n{allErrors}\n\nPlease check your OpenRouter API key and network connection."
            );
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

        private static string GetSystemPrompt()
        {
            return @"You are a master mystery writer. Generate a complete detective mystery case in JSON format.
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
      ""locationName"": ""Specific section of the crime scene where it is found (MUST be unique for each of the 5 clues)"",
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

            // Strip markdown code fences
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(3);
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            cleaned = cleaned.Trim();

            // Try to extract JSON object if there's leading/trailing text
            int firstBrace = cleaned.IndexOf('{');
            int lastBrace = cleaned.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return cleaned;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CaseForgeAI.Core.Interfaces;

namespace CaseForgeAI.Services
{
    /// <summary>
    /// Unified OpenRouter strategy that calls any model via the OpenRouter API.
    /// Supports automatic fallback through a chain of free models.
    /// </summary>
    public class OpenRouterStrategy : IOpenRouterStrategy
    {
        private readonly string _modelName;

        public OpenRouterStrategy(string modelName)
        {
            _modelName = modelName;
        }

        public string ModelName => _modelName;

        public async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/Saadi7701/CaseForge");
            client.DefaultRequestHeaders.Add("X-Title", "CaseForge AI");

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.75,
                max_tokens = 4096
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{baseUrl}/chat/completions", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"OpenRouter API error ({(int)response.StatusCode} {response.StatusCode}): {errorBody}"
                );
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Validate that we got choices
            if (!root.TryGetProperty("choices", out var choices) ||
                choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("OpenRouter returned no choices in the response.");
            }

            var content = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenRouter returned an empty response.");
            }

            return content;
        }
    }

    /// <summary>
    /// MockStrategy for offline/demo mode when no API key is configured.
    /// </summary>
    public class MockStrategy : IOpenRouterStrategy
    {
        public string ModelName => "mock/local-sandbox";

        public async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            await Task.Delay(1500); // Simulate API latency

            if (userPrompt.Contains("refine", StringComparison.OrdinalIgnoreCase) ||
                userPrompt.Contains("Refinement", StringComparison.OrdinalIgnoreCase))
            {
                return GetMockRefinedCase();
            }

            return GetMockCase();
        }

        private static string GetMockCase()
        {
            var caseObj = new
            {
                title = "The House Robbery",
                victim = "Mr. Sterling",
                difficulty = "Easy",
                crimeSceneDescription = "A nice study room in a big house. There is broken glass from the window on the floor. An empty safe is open.",
                suspects = new[]
                {
                    new { name = "Charles Sterling", role = "Nephew", description = "The nephew who needs money. He smells like whiskey and cheap cologne.", alibi = "I was at a club in the city all night.", motive = "He needed money to pay off his gambling debts.", isKiller = false, avatarUrl = "/images/suspect1.jpg" },
                    new { name = "Julian", role = "Gardener", description = "A quiet man who works in the garden. He has gold paint on his hands.", alibi = "I was working late in the greenhouse with the plants.", motive = "Mr. Sterling found out a secret about him and wanted to fire him.", isKiller = true, avatarUrl = "/images/suspect2.jpg" },
                    new { name = "Eleanor", role = "Secretary", description = "A smartly dressed woman who looks very organized and serious.", alibi = "I was in my room looking at the money books.", motive = "She wanted to steal the family jewelry.", isKiller = false, avatarUrl = "/images/suspect3.jpg" }
                },
                clues = new[]
                {
                    new { name = "Broken Glass", description = "Glass pieces that fell outside, showing someone broke the window from the inside to fake a break-in.", locationName = "Window", clueType = "Physical", isHidden = false, connectionInfo = "Shows the break-in was faked.", hotspotX = 18, hotspotY = 70 },
                    new { name = "Gold Paint Stains", description = "Marks of gold paint on a map. It is the same paint used on the safe.", locationName = "Garden Path", clueType = "Physical", isHidden = true, connectionInfo = "Connects the thief to the gardener's paint.", hotspotX = 72, hotspotY = 35 },
                    new { name = "Crossed Out Name", description = "A ripped page from the club's guest book showing Charles crossed out his name.", locationName = "Trash Can", clueType = "Document", isHidden = false, connectionInfo = "Shows Charles lied about where he was.", hotspotX = 45, hotspotY = 82 },
                    new { name = "Muddy Shoes", description = "Fresh mud from the garden on the floor.", locationName = "Hallway", clueType = "Physical", isHidden = true, connectionInfo = "Shows someone walked in from the garden.", hotspotX = 50, hotspotY = 90 },
                    new { name = "Empty Glass", description = "A drinking glass that smells like alcohol.", locationName = "Table", clueType = "Physical", isHidden = false, connectionInfo = "Someone was drinking here.", hotspotX = 85, hotspotY = 20 }
                },
                puzzles = new[]
                {
                    new { title = "Number Puzzle", puzzleType = "Cipher", question = "Find the code for the safe: '1-2-3'.", correctAnswer = "123", hint = "Just type the numbers without dashes.", pointsValue = 250, puzzleDataJson = "{}" },
                    new { title = "Order of Events", puzzleType = "Timeline", question = "Put these in order: [1] The safe is opened, [2] Mr. Sterling walks in, [3] The window is broken.", correctAnswer = "2,1,3", hint = "The window was broken last to fake the crime.", pointsValue = 300, puzzleDataJson = "{}" }
                },
                ending = "Julian, the gardener, used the safe code he found. He had gold paint on his hands from working on the safe. He broke the window from the inside to make it look like a stranger broke in."
            };

            return JsonSerializer.Serialize(caseObj);
        }

        private static string GetMockRefinedCase()
        {
            var caseObj = new
            {
                title = "The Apartment Mystery",
                victim = "The Manager",
                difficulty = "Medium",
                crimeSceneDescription = "A dark room with old furniture. The manager is on the floor next to a broken glass case. It smells like tobacco smoke.",
                suspects = new[]
                {
                    new { name = "Vincent", role = "Buyer", description = "A man wearing a nice jacket. He smells like vanilla pipe smoke.", alibi = "I was at a party with many people.", motive = "The manager would not sell him a special coin.", isKiller = true, avatarUrl = "/images/suspect_vincent.jpg" },
                    new { name = "Clara", role = "Worker", description = "A smart young woman with ink on her hands. She looks scared.", alibi = "I was working downstairs in the basement.", motive = "She wanted to tell the police about the manager stealing money.", isKiller = false, avatarUrl = "/images/suspect_clara.jpg" }
                },
                clues = new[]
                {
                    new { name = "Tobacco Ash", description = "A small pile of sweet-smelling pipe ash on the floor.", locationName = "Floor", clueType = "Physical", isHidden = false, connectionInfo = "Matches what Vincent smokes.", hotspotX = 35, hotspotY = 75 },
                    new { name = "Torn Ticket", description = "A party ticket with Vincent's name on it, found in the desk.", locationName = "Desk Drawer", clueType = "Document", isHidden = true, connectionInfo = "Shows Vincent was here before the party.", hotspotX = 80, hotspotY = 40 },
                    new { name = "Old Coin", description = "A gold coin dropped near the door.", locationName = "Doorway", clueType = "Physical", isHidden = true, connectionInfo = "Might belong to Clara, but it's just to trick you.", hotspotX = 10, hotspotY = 90 },
                    new { name = "Broken Glass", description = "Glass from the broken display case.", locationName = "Display Case", clueType = "Physical", isHidden = false, connectionInfo = "Shows where the thief stole the item.", hotspotX = 60, hotspotY = 50 },
                    new { name = "Sales Book", description = "A book showing who bought things.", locationName = "Bookshelf", clueType = "Document", isHidden = false, connectionInfo = "Has names of buyers, but not the killer.", hotspotX = 90, hotspotY = 20 }
                },
                puzzles = new[]
                {
                    new { title = "Secret Code", puzzleType = "Cipher", question = "Solve the code: 'B-C-D'. Move each letter back 1 space in the alphabet.", correctAnswer = "ABC", hint = "A comes before B.", pointsValue = 350, puzzleDataJson = "{}" }
                },
                ending = "Vincent killed the manager because he would not sell him the coin. Vincent tried to pretend he was at a party, but his tobacco ash and torn ticket showed he was there."
            };

            return JsonSerializer.Serialize(caseObj);
        }
    }

    // Keep legacy classes as thin wrappers for backward compatibility
    public class GemmaStrategy : OpenRouterStrategy
    {
        public GemmaStrategy() : base("google/gemma-2-9b-it:free") { }
    }

    public class LlamaStrategy : OpenRouterStrategy
    {
        public LlamaStrategy() : base("meta-llama/llama-3.1-8b-instruct:free") { }
    }
}

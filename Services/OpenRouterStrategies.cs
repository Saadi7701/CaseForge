using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CaseForgeAI.Core.Interfaces;

namespace CaseForgeAI.Services
{
    public class GemmaStrategy : IOpenRouterStrategy
    {
        public string ModelName => "google/gemma-2-9b-it:free";

        public async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            return await CallOpenRouterAsync(ModelName, systemPrompt, userPrompt, apiKey, baseUrl);
        }

        private static async Task<string> CallOpenRouterAsync(string model, string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/Saadi7701/elite_rings");
            client.DefaultRequestHeaders.Add("X-Title", "CaseForge AI");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                response_format = new { type = "json_object" }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/chat/completions", jsonContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var choice = doc.RootElement.GetProperty("choices")[0];
            return choice.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
    }

    public class LlamaStrategy : IOpenRouterStrategy
    {
        public string ModelName => "meta-llama/llama-3-8b-instruct:free";

        public async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            // Reuses the HttpClient logic from Gemma or has custom formatting if needed
            return await CallOpenRouterAsync(ModelName, systemPrompt, userPrompt, apiKey, baseUrl);
        }

        private static async Task<string> CallOpenRouterAsync(string model, string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/Saadi7701/elite_rings");
            client.DefaultRequestHeaders.Add("X-Title", "CaseForge AI");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.8
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/chat/completions", jsonContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var choice = doc.RootElement.GetProperty("choices")[0];
            return choice.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
    }

    // MockStrategy facilitates off-line demonstration/test runs out-of-the-box
    public class MockStrategy : IOpenRouterStrategy
    {
        public string ModelName => "mock/local-sandbox";

        public async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, string apiKey, string baseUrl)
        {
            await Task.Delay(1500); // Simulate API latency

            // Check if it's refinement or original generation
            if (userPrompt.Contains("refine", StringComparison.OrdinalIgnoreCase) || userPrompt.Contains("Make", StringComparison.OrdinalIgnoreCase))
            {
                // Return refined case
                return GetMockRefinedCase(userPrompt);
            }

            return GetMockCase();
        }

        private static string GetMockCase()
        {
            var caseObj = new
            {
                title = "The Emerald Estate Robbery",
                victim = "Lord Alistair Sterling",
                difficulty = "Medium",
                crimeSceneDescription = "A luxurious oak-paneled study in Sterling Manor. Glass fragments from the balcony door glitter on the Persian rug. An empty gold-accented safe door hangs ajar.",
                suspects = new[]
                {
                    new { name = "Charles Sterling", role = "Disinherited Nephew", description = "The estranged heir, heavily in debt. Smells faintly of imported whiskey and expensive cologne.", alibi = "I was at the Blackwood Gentlemen's Club in London all evening.", motive = "Desperately needed funds to pay off underground bookmakers before they acted.", isKiller = false, avatarUrl = "/images/suspect1.jpg" },
                    new { name = "Julian Sterling", role = "Secret Son / Heir", description = "A quiet man who worked as the estate gardener. Has hands stained with gold-leaf dye.", alibi = "I was working late in the greenhouse, potting winter flora.", motive = "Lord Sterling discovered his identity and threatened to disown him.", isKiller = true, avatarUrl = "/images/suspect2.jpg" },
                    new { name = "Eleanor Vance", role = "Personal Secretary", description = "Sharply dressed, organized, and carries herself with a poise that belies her modest background.", alibi = "I was in my quarters reviewing the yearly accounts ledger.", motive = "Expected to inherit a substantial portion of the family jewels.", isKiller = false, avatarUrl = "/images/suspect3.jpg" }
                },
                clues = new[]
                {
                    new { name = "Balcony Shattered Glass", description = "Shard of glass showing impact lines coming from the INSIDE of the room, suggesting a staged break-in.", locationName = "Study Balcony Door", clueType = "Physical", isHidden = false, connectionInfo = "Staged break-in by someone already inside.", hotspotX = 18, hotspotY = 70 },
                    new { name = "Golden Dye Stains", description = "Traces of a rare gold dye, the exact brand used to polish the safe's intricate brass details, found on a hand-drawn map.", locationName = "Greenhouse Path", clueType = "Physical", isHidden = true, connectionInfo = "Connects the thief directly to Julian's dye stains.", hotspotX = 72, hotspotY = 35 },
                    new { name = "Club Guest Ledger Page", description = "A torn page from the Blackwood Club guests ledger showing Charles Sterling's signature was entered at 11:30 PM, but crossed out.", locationName = "Library Rubbish Bin", clueType = "Document", isHidden = false, connectionInfo = "Exposes Charles' fake alibi.", hotspotX = 45, hotspotY = 82 }
                },
                puzzles = new[]
                {
                    new { title = "The Safe Lock Decipher", puzzleType = "Cipher", question = "Decrypt the safe combination code: 'KHOO HP'. It uses a Caesar Cipher of shift 3 (shift backward by 3 letters).", correctAnswer = "HELP ME", hint = "Subtract 3 from the alphabetical positions of K-H-O-O H-P.", pointsValue = 250, puzzleDataJson = "{\"shift\":3}" },
                    new { title = "Timeline Reconstruction", puzzleType = "Timeline", question = "Reconstruct the chronological timeline of events (Enter order e.g. 1,2,3). Events: [1] The Safe is opened, [2] Lord Sterling enters study, [3] Staged window break-in.", correctAnswer = "2,1,3", hint = "The staging of the break-in was the last thing the thief did to hide their traces after Lord Sterling was incapacitated.", pointsValue = 300, puzzleDataJson = "{\"events\":[\"Lord Sterling enters\",\"Safe is opened\",\"Window broken\"]}" }
                },
                ending = "Lord Alistair Sterling's safe was opened by someone who had both access to the safe combination and gold-polishing dye on their hands. Julian, the estate gardener and secret son, used the lock combination he found in Lord Sterling's letters. To conceal his crime, he staged a break-in by smashing the balcony glass from the inside."
            };

            return JsonSerializer.Serialize(caseObj);
        }

        private static string GetMockRefinedCase(string userPrompt)
        {
            var caseObj = new
            {
                title = "The Crimson Apartment (Refined)",
                victim = "Museum Curator",
                difficulty = "Hard",
                crimeSceneDescription = "A dimly lit room adorned with fine mahogany furniture and oil paintings. The curator lies beside a shattered mahogany display case. A distinct aroma of vintage pipe tobacco fills the air.",
                suspects = new[]
                {
                    new { name = "Vincent Vance", role = "Antique Dealer", description = "A collector with an intense gaze, wearing a velvet smoking jacket. Smells of vanilla tobacco.", alibi = "I was hosting an auction in Chelsea. Dozens of collectors saw me.", motive = "The curator refused to sell him the rare Crimson Medallion.", isKiller = true, avatarUrl = "/images/suspect_vincent.jpg" },
                    new { name = "Clara Sterling", role = "Curator's Assistant", description = "An intelligent young woman with ink stains on her fingers, looking visibly shaken.", alibi = "I was cataloging the new Roman coin shipments in the basement archive.", motive = "Wanted to expose the curator's forgery trade, which was ruining her academic career.", isKiller = false, avatarUrl = "/images/suspect_clara.jpg" }
                },
                clues = new[]
                {
                    new { name = "Vanilla Pipe Tobacco Ash", description = "A pinch of sweet-scented pipe ash scattered near the curator's body.", locationName = "Beside Victim", clueType = "Physical", isHidden = false, connectionInfo = "Ties Vincent Vance's habit to the crime scene.", hotspotX = 35, hotspotY = 75 },
                    new { name = "Torn Auction Ticket", description = "A VIP ticket to Vincent's Chelsea auction, dated tonight, but stained with Crimson dye matching the curator's ink.", locationName = "Desk Drawer", clueType = "Document", isHidden = true, connectionInfo = "Ties Vincent's presence to the office earlier than the auction time.", hotspotX = 80, hotspotY = 40 }
                },
                puzzles = new[]
                {
                    new { title = "Victim's Cryptic Safe", puzzleType = "Cipher", question = "Decrypt the curator's note: 'TIVT'. It uses a rot13 cipher (shift 13).", correctAnswer = "GIVG", hint = "Shift each character by 13 spaces in the alphabet.", pointsValue = 350, puzzleDataJson = "{\"shift\":13}" }
                },
                ending = "Vincent Vance killed the curator when the curator refused to sell him the stolen Crimson Medallion. Vincent staged the alibi by attending the auction later, but the vanilla pipe ash left at the scene and the torn ticket in the desk proved he visited the curator first."
            };

            return JsonSerializer.Serialize(caseObj);
        }
    }
}

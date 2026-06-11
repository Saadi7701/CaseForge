# Functions Overview

## GameplayService.cs

| Method | Description | Important Details |
|--------|-------------|-------------------|
| `StartCaseAsync(Guid caseId, string userId)` | Initializes a new player progress for the given case and user. If an existing progress is found and already completed, it is cleared. Creates a new `PlayerProgress` entity, saves it, and updates the `Analytics.TotalPlays` counter. | Returns the newly created `PlayerProgress`. Updates or creates an `Analytics` record for the story. |
| `CollectClueAsync(Guid progressId, Guid clueId)` | Adds a clue to the player’s inventory if not already collected. Increments the score by 50 (capped at 1500) and updates `LastSavedAt`. | Returns `true` if the operation succeeded. Checks for duplicate collection. |
| `AddEvidenceLinkAsync(Guid progressId, Guid clueA, Guid clueB, string notes)` | Creates a bidirectional link between two clues (evidence link). Prevents duplicate links. Saves the link and updates `LastSavedAt`. | Returns `true` on successful insertion. Uses `EvidenceLink` entity. |
| `RemoveEvidenceLinkAsync(Guid progressId, Guid linkId)` | Deletes a specific evidence link belonging to the progress. | Returns `true` when the link is removed. |
| `SolvePuzzleAsync(Guid progressId, Guid puzzleId, string answer)` | Retrieves the puzzle, converts it to a concrete puzzle type, validates the answer, and updates the score (adds points for correct answer, penalises for wrong answer). | Returns a `bool` indicating whether the answer was correct. Adjusts score (`+points` or `-50`). |
| `UseHintAsync(Guid progressId)` | Applies a hint penalty to the current progress by reducing the score by 100 (minimum 100). | Returns `true` on success. |
| `AccuseSuspectAsync(Guid progressId, Guid suspectId)` | Finalizes the case: marks progress as completed, records the accusation outcome, updates user‑wide total score and possible rank promotion, and updates analytics (solved count, average solve time, average score). | Returns an `AccusationResult` containing `Success`, `Message`, and `Details`. |
| `GetProgressAsync(Guid progressId)` *(implicit via other methods)* | Helper pattern used throughout to load `PlayerProgress` with related entities (`Inventory`, `EvidenceLinks`, `Story`). | Not a public method but important for context. |

## AIService.cs

| Method | Description | Important Details |
|--------|-------------|-------------------|
| `GenerateStoryJsonAsync(string prompt, string? model = null)` | Generates a full detective case JSON from a free‑form prompt using the AI model stack. | Calls `ExecuteWithFallbackAsync` with a system prompt that defines the required JSON schema. |
| `RefineStoryJsonAsync(string originalStoryJson, string feedbackPrompt, string? model = null)` | Takes an existing case JSON and a refinement request, then regenerates the JSON applying the requested changes. | Uses a different system prompt (`GetRefinementSystemPrompt`). |
| `CalculateQualityScore(string storyJson)` | Computes a heuristic quality score (1‑10) based on presence of required keys and minimal array lengths in the JSON. | Returns a clamped double. |
| `ExecuteWithFallbackAsync(string systemPrompt, string userPrompt)` | Core orchestration: attempts the default model, then each fallback model until one succeeds. Handles missing API key by using a mock strategy. Cleans the raw response (`CleanResponseJson`) and validates that at least a `title` field exists. | Throws aggregated `InvalidOperationException` if all models fail. |
| `CleanResponseJson(string raw)` | Strips markdown fences, `<think>` tags, and extracts the pure JSON object from any surrounding text. | Used internally by `ExecuteWithFallbackAsync`. |
| `GetSystemPrompt()` | Returns the system prompt that defines the required JSON schema for a new case. |
| `GetRefinementSystemPrompt()` | Returns the system prompt for refining an existing case. |
| `MockStrategy` *(inner class, not shown here)* | Provides deterministic placeholder JSON when no API key is configured. |
| `OpenRouterStrategy` *(inner class, not shown here)* | Handles actual HTTP calls to OpenRouter for a given model name. |

---

*All method signatures and behaviours are defined in the respective source files:* 
- [GameplayService.cs](file:///d:/Detective%20x/Services/GameplayService.cs)
- [AIService.cs](file:///d:/Detective%20x/Services/AIService.cs)

Feel free to ask for deeper code walkthroughs or specific implementation details for any of these functions.

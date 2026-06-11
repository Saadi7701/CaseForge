# Architecture Overview

## Gameplay Module
- **File:** [GameplayService.cs](file:///d:/Detective%20x/Services/GameplayService.cs)
- Handles player session lifecycle: start case, collect clues, link evidence, solve puzzles, use hints, and final accusation.
- Updates `PlayerProgress` and persists changes via `ApplicationDbContext`.
- **Analytics updates:** increments `TotalPlays` on case start and updates `SolvedCount`, `AverageSolveTimeSeconds`, and `AverageScore` on a correct accusation.
- Exposes methods used by `GameplayController` (e.g., `StartCaseAsync`, `CollectClueAsync`, `AddEvidenceLinkAsync`, `RemoveEvidenceLinkAsync`, `SolvePuzzleAsync`, `UseHintAsync`, `AccuseSuspectAsync`).

## AI Editor
- **File:** [AIService.cs](file:///d:/Detective%20x/Services/AIService.cs)
- Generates and refines detective‑case JSON using OpenRouter models (or a mock strategy when no API key is configured).
- Core methods:
  - `GenerateStoryJsonAsync` – creates a new case from a free‑form prompt.
  - `RefineStoryJsonAsync` – edits an existing case based on a refinement request.
  - `CalculateQualityScore` – simple heuristic to score JSON completeness.
- Uses `ExecuteWithFallbackAsync` to try the default model then any configured fallback models, cleaning and validating the JSON response.

## Analytics
- Stored in an `Analytics` entity (table) keyed by `StoryId`.
- **When a case starts** (`StartCaseAsync`):
  - Increments `TotalPlays` and updates `LastUpdated`.
- **When a suspect is correctly accused** (`AccuseSuspectAsync`):
  - Increments `SolvedCount`.
  - Recalculates `AverageSolveTimeSeconds` and `AverageScore` using the new play data.
- These metrics feed dashboards/leaderboards to show story popularity and player performance.

---

*This file provides a concise, high‑level description of how the gameplay workflow, AI editor, and analytics components work together in the CaseForge application.*

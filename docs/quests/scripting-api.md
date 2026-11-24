# Quests Scripting API

Complete reference for `ContentContent.Quests` (pure C# core) and `ContentContent.Quests.Unity` (Unity adapters).

---

## Namespace: `ContentContent.Quests`

### Models (`QuestModels.cs`)

- **`QuestStatus`**: `NotStarted = 0`, `Active = 1`, `Completed = 2`, `Failed = 3`.
- **`ObjectiveMode`**: `AllParallel = 0`, `Sequential = 1`.
- **`ObjectiveDefinition`**: `Id`, `Description`, `TargetCount`, `IsOptional`, `SignalKey`.
- **`QuestDefinition`**: `Id`, `Title`, `Description`, `ObjectiveMode`, `PrerequisiteQuestIds`, `Objectives`, `Repeatable`, `Validate()`.
- **`ObjectiveProgress`**: `Definition`, `CurrentCount`, `IsComplete`, `NormalizedProgress`.
- **`QuestState`**: `Definition`, `Status`, `Objectives`, `CompletionCount`, `AreRequiredObjectivesComplete`, `NormalizedProgress`, `IsObjectiveUnlocked(int index)`.
- **`QuestSnapshot`**: `QuestId`, `Status`, `CompletionCount`, `ObjectiveCounts`.

### `QuestJournal` (`sealed class`)

- Methods: `RegisterQuest`, `TryGetDefinition`, `GetStatus`, `TryGetState`, `CanStart`, `TryStartQuest`, `ReportProgress`, `ReportSignal`, `TryFailQuest`, `AbandonQuest`, `GetActiveQuests`, `GetCompletedQuests`, `ExportSnapshots`, `ImportSnapshots`.
- Events: `OnQuestStarted`, `OnObjectiveProgressed`, `OnQuestCompleted`, `OnQuestFailed`.

---

## Namespace: `ContentContent.Quests.Unity`

- **`QuestDefinitionAsset` & `QuestCatalogAsset`**: ScriptableObject assets with `ValidateAsset()` and `RegisterAll(QuestJournal)`.
- **`QuestLogComponent`** (`MonoBehaviour`): Hosts `QuestJournal`, loads `_catalog` and `_startingQuests`, exposes `StartQuest`, `StartQuestById`, `ReportSignal`, `ReportSignalAmount`, `FailQuestById`, plus `OnQuestStarted`, `OnObjectiveProgressed`, `OnQuestCompleted`, `OnQuestFailed` UnityEvents.
- **`QuestSignalEmitter`** (`MonoBehaviour`): Auto-wires scene `QuestLogComponent` and exposes parameterless `Emit()` and `EmitAmount(int)` for zero-code UnityEvent bindings.

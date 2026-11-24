# Quests Design & Best Practices

A common beginner trap is checking `if (QuestManager.Instance.currentQuest == "SlimeHunt" && step == 1)` inside enemy death handlers, dialogue buttons, and item pickups. That tight coupling guarantees spaghetti code every time two quests care about the same enemy or a player tackles side quests in parallel. `com.contentcontent.quests` models quests around **decoupled gameplay signals**.

---

## 1. Signal Fan-Out Journal

```text
   Enemy.OnDied / Dialog Command / Chest Opened ──► QuestSignalEmitter.Emit("slime_defeated")
                                                                 │
                                                                 ▼
                                                     QuestJournal.ReportSignal
                                          ┌──────────────────────┴──────────────────────┐
                                          ▼                                             ▼
                             Active Quest A (Unlocked Obj)                 Active Quest B (Locked Step: Ignored)
```

### Why This Architecture Works
- **Senders know nothing about quests:** An enemy just emits `"slime_defeated"` (or `"boss_slain"`). Whether 0 quests or 3 overlapping quests listen is completely handled by `QuestJournal`.
- **Sequential locking prevents out-of-order breaks:** In `ObjectiveMode.Sequential`, `ReportSignal` checks unlocked steps before applying deltas — so Step 2 never accidentally steals progress meant for Step 1.
- **Portable snapshots:** `ExportSnapshots()` / `ImportSnapshots()` serialize quest status, completion counts, and per-objective counts cleanly without storing live scene references.

---

## 2. Best Practices & Common Pitfalls

- **Use domain event names as `SignalKey`s** (e.g., `"talk_merchant"`, `"ore_mined"`, `"bridge_repaired"`) so multiple quests can share reusable triggers.
- **Keep `QuestDefinitionAsset` validation green** (avoid self-prerequisites or quests with only optional objectives).

---

## 3. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Quests`) runs directly in Godot C#, driven by a thin AutoLoad `QuestJournal` node listening to signals.
- **Unreal:** Mirrors Unreal Gameplay Message Subsystem / Gameplay Tag events feeding a `UQuestSubsystem` with structured `UDataAsset` objective definitions.

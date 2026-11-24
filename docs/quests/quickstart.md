# Quests Quickstart

Track sequential and parallel mission objectives, prerequisite chains, optional bonus goals, and decoupled gameplay signals without coupling enemies or chests to your quest UI.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.quests`.
2. Open **Window → Package Manager**, select **ContentContent - Quests**, and click **Download** under **Downloadable Samples**.
3. Click **Open Scene** (`QuestsPlayground.unity`) and press **Play**.

---

## 2. Author & Wire Quests (Code-Free)

1. Right-click in the Project window: **Create → ContentContent → Quests → Quest Definition**.
   - Pick **Objective Mode**: `AllParallel` (any order) or `Sequential` (must finish required steps in order).
   - Add **Objectives** with **Target Count** and a **Signal Key** (e.g., `slime_defeated` or `talk_warden`).
   - Bundle definitions into a **Quest Catalog** (**Create → ContentContent → Quests → Quest Catalog**).
2. In the Hierarchy menu, click **GameObject → ContentContent → Quests → Quest Log** and assign your **Quest Catalog** and any **Starting Quests**.
3. Add **Quest Signal Emitter** (**GameObject → ContentContent → Quests → Quest Signal Emitter**) to enemies, chests, or doors:
   - Set **Signal Key** to `slime_defeated` and wire `HealthComponent.OnDied` or `InteractableBehaviour.OnInteracted` directly to `QuestSignalEmitter.Emit()`.
4. Watch **PlayMode Live Quest Tracker** inside the `QuestLogComponent` Inspector to inspect progress bars and test **+1** / **Fail** states live!

---

## 3. Code Usage

```csharp
using ContentContent.Quests;

var journal = new QuestJournal();
journal.RegisterQuest(new QuestDefinition(
    "slime_hunt",
    objectives: new[] { new ObjectiveDefinition("slimes", "Slay Slimes", targetCount: 3, signalKey: "slime_defeated") }));

journal.TryStartQuest("slime_hunt");
journal.ReportSignal("slime_defeated", 1);
```

---

## Next Steps

- Learn *why* signal fan-out decouples quest logic from enemies and dialogue in [Design & Best Practices](design.md).
- Browse all C# and Unity classes in [Scripting API](scripting-api.md).

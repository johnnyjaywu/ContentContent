# Dialog System Quickstart

Run branching NPC conversations in minutes without writing dialog state-machine code — using either Yarn-style `.txt` scripts or Inspector `DialogGraphAsset` assets, code-free `Command Bindings`, live syntax validation, and downloadable uGUI / UI Toolkit samples.

---

## 1. Try a Playable Sample First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.dialog`.
2. Open **Window → Package Manager**, select **ContentContent - Dialog System**, and scroll to **Downloadable Samples**.
3. Click **Download** on **uGUI Dialog Sample** or **UI Toolkit Dialog Sample**, then click **Open Scene** and press **Play**.

---

## 2. Create a Dialog Runner (Code-Free)

1. In the Hierarchy menu, click **GameObject → ContentContent → Dialog → Dialog Runner**.
2. Choose how to author your dialog:

### Option A: Yarn-Style `.txt` Script (Fastest for Writers)

Create `MerchantDialog.txt` anywhere under `Assets/`:

```text
<<declare $gold = 60>>
<<declare $gate_open = false>>

title: guard_greeting
---
Gate Guard: Halt, traveler. You carry {$gold} gold.
-> Offer 50 gold <<if $gold >= 50 and !$gate_open>>
    <<set $gold -= 50>>
    <<play_sfx coins_clink>>
    <<jump guard_bribed>>
-> Leave
    Gate Guard: Wise choice.
===

title: guard_bribed
---
<<set $gate_open = true>>
<<open_gate north_gate>>
Gate Guard: Gate unlocked. You have {$gold} gold left.
===
```

Assign `MerchantDialog.txt` to **Initial Script** on `DialogRunnerBehaviour`. The Inspector immediately parses the script in EditMode and reports any syntax errors (with exact line numbers) or broken jump targets before you even hit Play.

### Option B: `DialogGraphAsset` (Inspector Data Asset)

1. Right-click in the Project window: **Create → ContentContent → Dialog → Dialog Graph**.
2. Set **Start Node Id** (e.g., `intro`), add **Default Variables** (the custom property drawer displays only the matching bool/int/float/string input), and add **Nodes** with `isEnd` or `choices`.
3. Assign the asset to **Initial Graph** on `DialogRunnerBehaviour`. The Asset Inspector continuously runs `ValidateGraph()` and surfaces warnings before runtime.

---

## 3. Hook Dialog to Gameplay Without Code

- **Spoken lines (`OnLineShown`):** Wire `DialogRunnerBehaviour.OnLineShown (string speaker, string text)` directly to UI text components, or add a child component implementing `IDialogView` (auto-discovered by `DialogRunnerBehaviour` on awake).
- **Commands (`Command Bindings`):** In **Command Hooks**, map `<<open_gate north_gate>>` by adding an entry with `commandName = open_gate` and wiring `onInvoked (string)` to your door script, audio player, or quest manager.
- **PlayMode Live Debugger:** Select the `DialogRunner` in PlayMode to inspect the active node, see `{variables}` update live, click choices, and step forward without even building temporary UI buttons first.

---

## 4. Optional Localization

1. **Create → ContentContent → Dialog → Localization Table**.
2. Set line keys such as `GUARD_GREETING` in your graph or `#line:GUARD_GREETING` tags in `.txt` scripts, map them in the table, and assign it to **Localization Table** on `DialogRunnerBehaviour`.

---

## Next Steps

- Understand *why* dialog runs as data + commands and how it maps to Godot/Unreal in [Design & Best Practices](design.md).
- Browse all C# APIs in [Scripting API](scripting-api.md).

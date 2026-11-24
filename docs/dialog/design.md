# Dialog Design & Best Practices

Beginners often build dialog by writing nested `if/else` switch statements or hardcoding UI `Button.onClick` callbacks directly inside NPC scripts. Within a few conversations, save/load breaks, branching choices corrupt shared quest flags, and every typo requires recompiling C#. `com.contentcontent.dialog` separates **graph content**, **story state**, **command side effects**, and **UI rendering**.

---

## 1. Core Architecture: UI-Agnostic Runner & Commands

```text
  [.txt Script / DialogGraphAsset] ──► DialogGraph (Immutable Data)
                                              │
                                              ▼
     DialogVariableStore ◄──────────► DialogRunner (Pure C# State Machine)
     (Saveable Key/Value)                │         │
                                         ▼         ▼
                              IDialogView      CommandEventBinding /
                         (uGUI / UI Toolkit)   C# Command Handlers
```

### Why This Separation Matters
- **UI stays swappable:** `DialogRunner` produces `DialogLineViewModel` and `DialogChoiceViewModel` structs. You can prototype with the PlayMode Live Debugger, swap in uGUI, or migrate to UI Toolkit without rewriting a single conversation line.
- **Gameplay stays decoupled:** A line never references `DoorScript.Open()` directly; it emits `<<open_gate north_gate>>`. Scene objects subscribe via Inspector `Command Bindings` or `RegisterCommandHandler`, so the same script runs cleanly in headless unit tests.
- **Saves become trivial:** All persistent narrative state lives inside `DialogVariableStore`. Calling `Snapshot()` and `Restore(...)` captures every conversation branch in one dictionary.

---

## 2. Best Practices & Common Pitfalls

- **Attach mutations to choices, not every revisit of a greeting node:** In `DialogRunner`, node `onEnterMutations` execute every time the node is entered, whereas choice `mutations` run only when that branch is selected.
- **Always end branches cleanly:** Either mark terminal nodes with `isEnd = true` (`<<stop>>`) or provide at least one outgoing `nextNodeId` / `choice`. Use the Inspector's built-in validation warnings to catch dangling targets early.
- **Separate line keys from display copy if translating:** Using `#line:KEY` tags and a `DialogLocalizationTableAsset` lets writers edit dialogue strings cleanly per locale without changing graph logic or variable names.

---

## 3. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Dialog`) runs in Godot C# unchanged; bind `IDialogView` to Godot `Control` nodes and `DialogRunner.OnCommandExecuted` to Godot signals. Directly mirrors Dialogue Manager / Yarn Spinner architectures.
- **Unreal:** Mirrors the separation between a Narrative `DataTable` / dialogue asset, a typed variable BlackBoard/Subsystem, and `UUserWidget` view presenters driven by delegates (`OnLineShown`, `OnChoicesShown`) rather than embedding gameplay logic inside widget Blueprints.

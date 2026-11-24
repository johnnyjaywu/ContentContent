# How the Dialog System Works

`com.contentcontent.dialog` is a modular, UI-agnostic branching dialog system designed around a clean separation between **pure C# narrative logic**, **Unity Inspector/lifecycle adapters**, and **interchangeable UI views** (uGUI, UI Toolkit, or headless test doubles).

## Package Architecture

```text
Packages/com.contentcontent.dialog/
├── Runtime/
│   ├── ContentContent.Dialog.asmdef          # Pure C# (noEngineReferences: true)
│   ├── DialogValue.cs                        # Typed immutable value (Bool/Int/Float/String)
│   ├── DialogVariableStore.cs                # Shared narrative variable store + snapshots
│   ├── DialogCondition.cs                    # Data-driven predicate
│   ├── DialogMutation.cs                     # Data-driven variable write (Set/Add/Subtract/Toggle)
│   ├── DialogCommand.cs                      # Named engine hook dispatched to game code
│   ├── DialogChoice.cs                       # Branching option with conditions, mutations, commands
│   ├── DialogNode.cs                         # Single graph node (speaker, line key, exits)
│   ├── DialogGraph.cs                        # Immutable graph + Validate() static analyzer
│   ├── IDialogLocalizer.cs                   # Localization abstraction + fallback table
│   ├── IDialogView.cs                        # UI presentation contract + ViewModels
│   ├── DialogRunner.cs                       # State machine driving execution
│   ├── DialogScriptParser.cs                 # Yarn-style plain text script parser
│   └── Unity/
│       ├── ContentContent.Dialog.Unity.asmdef
│       ├── DialogGraphAsset.cs               # ScriptableObject graph authoring asset
│       ├── DialogLocalizationTableAsset.cs   # ScriptableObject string table
│       └── DialogRunnerBehaviour.cs          # MonoBehaviour adapter + UnityEvents
└── Tests/EditMode/
    └── DialogSystemTests.cs                  # NUnit EditMode test suite
```
*(Downloadable uGUI and UI Toolkit samples live in top-level `Samples/dialog/`.)*

Because `ContentContent.Dialog.asmdef` sets `"noEngineReferences": true`, the entire dialog engine compiles fast, runs deterministically outside Unity, and is unit-tested in EditMode without scenes or GameObjects.

---

## Core Concepts

### 1. Dialog Graph (`DialogGraph` & `DialogNode`)

A conversation is modeled as a directed graph of immutable `DialogNode` instances indexed by string `Id`:

- **Speaker & Line Key**: Each node holds a `Speaker` key and a `LineKey`. Both pass through `IDialogLocalizer` and `{variable}` interpolation before reaching the UI.
- **Exits**: Every node should define **exactly one** exit mode:
  - **Choices** (`Choices.Count > 0`): Pauses and presents available options to the player.
  - **Linear Next** (`NextNodeId` non-empty): Pauses on the line until the UI calls `Continue()`, then advances to `NextNodeId`.
  - **End** (`IsEnd == true`): Displays the final line (if `LineKey` is non-empty) and ends the conversation when `Continue()` is called—or ends immediately if `LineKey` is empty.
- **Default Variables**: A `DialogGraph` can declare `DefaultVariables` that seed the `DialogVariableStore` on `Start()` without overwriting values that already exist in the store.

### 2. Variable Store (`DialogVariableStore` & `DialogValue`)

Narrative state lives outside the UI in an `IDialogVariableStore` holding typed `DialogValue` structs (`Bool`, `Int`, `Float`, `String`):

- Sharing one `DialogVariableStore` across conversations lets NPCs react to choices made earlier in the game.
- `Snapshot()` and `Restore(...)` make saving and loading narrative state straightforward.
- `OnVariableChanged` notifies gameplay systems or HUDs whenever a variable changes.

### 3. Conditions, Mutations & Revisit Safety

- **Conditions (`DialogCondition`)** gate whether a `DialogChoice` is offered to the player (`Equal`, `NotEqual`, `GreaterThan`, `GreaterOrEqual`, `LessThan`, `LessOrEqual`, `Exists`, `NotExists`). Choices whose conditions fail are filtered out before `ShowChoices` is called.
- **Mutations (`DialogMutation`)** modify variables (`Set`, `Add`, `Subtract`, `Toggle`).
- **Transition vs. Entry Mutations**:
  - Put **one-time costs or rewards** (such as spending 50 gold to bribe a guard) on `DialogChoice.Mutations`. They run only when the player selects that choice, so revisiting the target node never charges the player twice.
  - Put **idempotent state flags** (such as `met_guard = true` or `gate_open = true`) on `DialogNode.OnEnterMutations`.

### 4. Engine Commands (`DialogCommand`)

Nodes (`OnEnterCommands`) and choices (`Commands`) can trigger named `DialogCommand`s with string arguments (for example `play_sfx("coins_clink")` or `open_gate("north_gate")`).

Game code registers handlers via `DialogRunner.RegisterCommandHandler(name, handler)`. Handlers are looked up case-insensitively, and `DialogRunner.OnCommandExecuted` fires after dispatch so audio, quests, cutscenes, or debug HUDs can react cleanly.

### 5. Localization & Variable Interpolation

Text formatting runs in two stages inside `DialogRunner.FormatText(key)`:

1. **Localization (`IDialogLocalizer`)**: Looks up `key` in the active localizer (`DictionaryLocalizer` or `DialogLocalizationTableAsset`). Missing keys return `<MISSING:key>` rather than throwing, making translation gaps immediately visible during QA. `PassthroughLocalizer` returns keys unchanged for rapid prototyping.
2. **Variable Interpolation**: Any `{variableName}` or `{$variableName}` token matching a key in `Variables` is replaced with `DialogValue.ToDisplayString()`. Unknown placeholders remain untouched.

### 6. Text Asset Authoring (`DialogScriptParser`)

As an alternative to `DialogGraphAsset` ScriptableObjects or C# graph construction, dialogs can be authored in plain `.txt` files (`TextAsset`) using Yarn-Spinner-style syntax:

- **Nodes**: `title: <id>`, body between `---` and `===`. Multi-line nodes automatically chain into sequential `DialogNode`s.
- **Variables**: `<<declare $gold = 60>>`, `<<set $gold = $gold - 50>>`, `<<set $gold += 20>>`, `<<set $flag = !$flag>>`.
- **Choices**: `-> Choice text <<if $gold >= 50 and !$gate_open>>` with indented transition `<<set>>`, `<<command>>`, `<<jump NodeId>>`, `<<stop>>`, or inline spoken replies (which auto-synthesize target nodes).
- **Localization Tags**: Append `#line:KEY` to any spoken line or choice to emit `KEY` as the `LineKey`/`TextKey` while keeping human-readable text in the script.
- **Comments**: Full-line `//` comments are ignored.

---

## Execution Lifecycle

When `DialogRunner.Start(graph)` is called, the runner executes the following state machine:

```text
Start(graph)
  │
  ├─► Seed missing DefaultVariables into Variables
  ├─► View.OnDialogStarted(graph) & OnStarted
  │
  ▼
EnterNode(nodeId)
  ├─► Apply node.OnEnterMutations
  ├─► Execute node.OnEnterCommands
  ├─► If node.IsEnd && LineKey is empty ──► EndDialog()
  ├─► Filter node.Choices via choice.IsAvailable(Variables)
  ├─► Format speaker, line, and visible choice labels (Localize + {var})
  ├─► View.ShowLine(lineViewModel) & OnLineShown
  └─► If visible choices exist:
        View.ShowChoices(choiceViewModels) & OnChoicesShown
        ──► Wait for SelectChoice(visibleIndex)
              ├─► Apply chosen.Mutations
              ├─► Execute chosen.Commands
              └─► EnterNode(chosen.TargetNodeId)  (or EndDialog if empty)
      Else:
        ──► Wait for Continue()
              └─► If node.IsEnd or NextNodeId empty ──► EndDialog()
                  Else ──► EnterNode(node.NextNodeId)
```

---

## Graph Validation

Calling `graph.Validate()` performs a static pass over the graph and returns a `List<DialogValidationIssue>`. An empty list guarantees:

- `StartNodeId` exists in `Nodes`.
- Every node has **exactly one** exit (`Choices`, `NextNodeId`, or `IsEnd`), preventing silent dead ends or ambiguous exits.
- Every `NextNodeId` and `DialogChoice.TargetNodeId` points to an existing node.
- Every node is reachable from `StartNodeId` (depth-first traversal).

Run `Validate()` in EditMode tests or editor tooling to catch authoring errors before playtesting.

---

## Decoupled UI (`IDialogView`)

`DialogRunner` never references `UnityEngine.UI` or `UnityEngine.UIElements`. Instead, any UI framework implements the four methods of `IDialogView`:

- `OnDialogStarted(DialogGraph graph)`
- `ShowLine(DialogLineViewModel line)`
- `ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices)`
- `OnDialogEnded(DialogGraph graph)`

The included samples demonstrate two complete implementations sharing the exact same `DialogRunnerBehaviour` and `SampleDialogData`:

- **uGUI (`UGUIDialogView`)**: Drives a `Canvas` (`Scale With Screen Size`), `TextMeshProUGUI` typewriter reveal (`maxVisibleCharacters`), `VerticalLayoutGroup` choice buttons, and `InputSystemUIInputModule`.
- **UI Toolkit (`UIToolkitDialogView`)**: Drives a `UIDocument` with `DialogView.uxml` and `DialogView.uss`, toggling visibility via USS classes (`.is-hidden`) and spawning styled `Button` elements into `choicesContainer`.

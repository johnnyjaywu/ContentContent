# Dialog System Scripting API

Complete reference for `ContentContent.Dialog` (pure C# core) and `ContentContent.Dialog.Unity` (Unity adapters).

---

## Namespace: `ContentContent.Dialog`

### `DialogValueType` (enum)

| Value | Description |
| --- | --- |
| `Bool = 0` | Boolean value. |
| `Int = 1` | 32-bit signed integer. |
| `Float = 2` | Single-precision floating-point number. |
| `String = 3` | UTF-16 string. |

---

### `DialogValue` (readonly struct)

Implements `IEquatable<DialogValue>`. Immutable typed value stored in `DialogVariableStore`.

#### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Type` | `DialogValueType` | Underlying value type. |
| `BoolValue` | `bool` | Raw boolean representation. |
| `IntValue` | `int` | Raw integer representation. |
| `FloatValue` | `float` | Raw float representation. |
| `StringValue` | `string` | Raw string representation (never `null`). |

#### Factory Methods

```csharp
DialogValue.FromBool(bool value)
DialogValue.FromInt(int value)
DialogValue.FromFloat(float value)
DialogValue.FromString(string value)
```

#### Methods

| Method | Return | Description |
| --- | --- | --- |
| `AsBool()` | `bool` | Converts to boolean (`Int != 0`, `|Float| > epsilon`, non-empty string). |
| `AsInt()` | `int` | Converts to integer (parses invariant string if `String`). |
| `AsFloat()` | `float` | Converts to float (parses invariant string if `String`). |
| `ToDisplayString()` | `string` | Invariant display string used during `{var}` interpolation. |
| `Equals(DialogValue other)` | `bool` | Compares values (allows `Int` vs `Float` numeric equality). |
| `CompareNumeric(DialogValue other)` | `int` | Returns `-1`, `0`, or `1` comparing `AsFloat()` values. |

---

### `IDialogVariableStore` & `DialogVariableStore`

Flat dictionary-backed variable store with ordinal key lookup.

#### Events

| Event | Signature | Description |
| --- | --- | --- |
| `OnVariableChanged` | `Action<string, DialogValue>` | Invoked when a variable is added, changed, or restored. |

#### Methods

| Method | Return | Description |
| --- | --- | --- |
| `Has(string name)` | `bool` | Returns `true` if the variable exists. |
| `TryGet(string name, out DialogValue value)` | `bool` | Attempts to read a variable. |
| `Set(string name, DialogValue value)` | `void` | Sets a variable; fires `OnVariableChanged` if the value changed. |
| `SetBool(string name, bool value)` | `void` | Convenience setter on `DialogVariableStore`. |
| `SetInt(string name, int value)` | `void` | Convenience setter on `DialogVariableStore`. |
| `SetFloat(string name, float value)` | `void` | Convenience setter on `DialogVariableStore`. |
| `SetString(string name, string value)` | `void` | Convenience setter on `DialogVariableStore`. |
| `GetBool(string name, bool defaultValue = false)` | `bool` | Reads as bool or returns `defaultValue`. |
| `GetInt(string name, int defaultValue = 0)` | `int` | Reads as int or returns `defaultValue`. |
| `GetFloat(string name, float defaultValue = 0f)` | `float` | Reads as float or returns `defaultValue`. |
| `GetString(string name, string defaultValue = "")` | `string` | Reads as display string or returns `defaultValue`. |
| `Snapshot()` | `IReadOnlyDictionary<string, DialogValue>` | Returns a shallow copy for serialization/save systems. |
| `Restore(IReadOnlyDictionary<string, DialogValue> values)` | `void` | Replaces all variables with the provided snapshot. |
| `Clear()` | `void` | Removes all stored variables. |

---

### `DialogComparisonOperator` (enum) & `DialogCondition`

Predicate evaluated against an `IDialogVariableStore`.

**Enum values:** `Equal`, `NotEqual`, `GreaterThan`, `GreaterOrEqual`, `LessThan`, `LessOrEqual`, `Exists`, `NotExists`.

#### Constructor

```csharp
public DialogCondition(
    string variableName,
    DialogComparisonOperator op,
    DialogValue compareValue = default)
```

#### Properties & Methods

| Member | Type | Description |
| --- | --- | --- |
| `VariableName` | `string` | Name of the variable to inspect. |
| `Operator` | `DialogComparisonOperator` | Comparison operator. |
| `CompareValue` | `DialogValue` | Target value (unused for `Exists`/`NotExists`). |
| `Evaluate(IDialogVariableStore store)` | `bool` | Evaluates the condition against `store`. |

---

### `DialogMutationOperator` (enum) & `DialogMutation`

Variable write applied on node entry or choice transition.

**Enum values:** `Set`, `Add`, `Subtract`, `Toggle`.

#### Constructor

```csharp
public DialogMutation(
    string variableName,
    DialogMutationOperator op,
    DialogValue value = default)
```

#### Methods

| Method | Return | Description |
| --- | --- | --- |
| `Apply(IDialogVariableStore store)` | `void` | Applies the mutation to `store`. `Add`/`Subtract` preserve `Int` unless either operand is `Float`. |

---

### `DialogCommand`

Named engine command dispatched to registered handlers.

#### Constructor

```csharp
public DialogCommand(string name, params string[] arguments)
```

#### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Command name (matched case-insensitively by `DialogRunner`). |
| `Arguments` | `IReadOnlyList<string>` | Optional positional string arguments. |

---

### `DialogChoice`

Branching option on a `DialogNode`.

#### Constructor

```csharp
public DialogChoice(
    string textKey,
    string targetNodeId,
    IReadOnlyList<DialogCondition> conditions = null,
    IReadOnlyList<DialogMutation> mutations = null,
    IReadOnlyList<DialogCommand> commands = null)
```

#### Properties & Methods

| Member | Type | Description |
| --- | --- | --- |
| `TextKey` | `string` | Localization key (or raw text) for the choice label. |
| `TargetNodeId` | `string` | Node ID to enter when chosen. |
| `Conditions` | `IReadOnlyList<DialogCondition>` | All conditions must pass for the choice to be shown. |
| `Mutations` | `IReadOnlyList<DialogMutation>` | Mutations applied on transition. |
| `Commands` | `IReadOnlyList<DialogCommand>` | Commands executed on transition. |
| `IsAvailable(IDialogVariableStore store)` | `bool` | Returns `true` if every condition evaluates to `true`. |

---

### `DialogNode`

Single node inside a `DialogGraph`.

#### Constructor

```csharp
public DialogNode(
    string id,
    string speaker = "",
    string lineKey = "",
    string nextNodeId = "",
    bool isEnd = false,
    IReadOnlyList<DialogChoice> choices = null,
    IReadOnlyList<DialogMutation> onEnterMutations = null,
    IReadOnlyList<DialogCommand> onEnterCommands = null)
```

#### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique node identifier within its graph. |
| `Speaker` | `string` | Speaker localization key or display name. |
| `LineKey` | `string` | Line localization key or display text. |
| `NextNodeId` | `string` | Next node ID for linear nodes. |
| `IsEnd` | `bool` | Marks this node as a conversation exit. |
| `HasChoices` | `bool` | `true` when `Choices.Count > 0`. |
| `Choices` | `IReadOnlyList<DialogChoice>` | Branching options. |
| `OnEnterMutations` | `IReadOnlyList<DialogMutation>` | Mutations applied on node entry. |
| `OnEnterCommands` | `IReadOnlyList<DialogCommand>` | Commands executed on node entry. |

---

### `DialogGraph` & `DialogValidationIssue`

Immutable container of `DialogNode`s and default variables.

#### Constructor

```csharp
public DialogGraph(
    string id,
    string startNodeId,
    IEnumerable<DialogNode> nodes,
    IReadOnlyDictionary<string, DialogValue> defaultVariables = null)
```

#### Properties & Methods

| Member | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Graph identifier. |
| `StartNodeId` | `string` | Default entry node ID. |
| `Nodes` | `IReadOnlyDictionary<string, DialogNode>` | Node lookup by ID. |
| `DefaultVariables` | `IReadOnlyDictionary<string, DialogValue>` | Initial variable defaults seeded on `Start()`. |
| `TryGetNode(string nodeId, out DialogNode node)` | `bool` | Looks up a node by ID. |
| `Validate()` | `List<DialogValidationIssue>` | Checks start node, exit integrity, broken targets, dead ends, and reachability. |

---

### `IDialogLocalizer`, `PassthroughLocalizer`, `DictionaryLocalizer`

```csharp
public interface IDialogLocalizer
{
    string Localize(string key);
}
```

- `PassthroughLocalizer.Instance`: Returns `key` unchanged.
- `new DictionaryLocalizer(primary, fallback = null)`: Looks up `key` in `primary`, then `fallback`, otherwise returns `"<MISSING:key>"`.

---

### `IDialogView`, `DialogLineViewModel`, `DialogChoiceViewModel`

```csharp
public interface IDialogView
{
    void OnDialogStarted(DialogGraph graph);
    void ShowLine(DialogLineViewModel line);
    void ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices);
    void OnDialogEnded(DialogGraph graph);
}
```

- **`DialogLineViewModel`**: `NodeId`, `Speaker`, `Text`, `HasChoices`, `IsEnd`.
- **`DialogChoiceViewModel`**: `Index` (0-based visible index passed to `SelectChoice`), `Text`, `TargetNodeId`.

---

### `DialogRunner`

Core state machine executing a `DialogGraph`.

#### Constructor

```csharp
public DialogRunner(
    IDialogVariableStore variables = null,
    IDialogLocalizer localizer = null,
    IDialogView view = null)
```

#### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Variables` | `IDialogVariableStore` | Active variable store. |
| `Localizer` | `IDialogLocalizer` | Active localizer (defaults to `PassthroughLocalizer.Instance`). |
| `View` | `IDialogView` | Active UI view (optional). |
| `CurrentGraph` | `DialogGraph` | Running graph, or `null` when idle. |
| `CurrentNode` | `DialogNode` | Active node, or `null` when idle. |
| `IsRunning` | `bool` | `true` while a dialog session is active. |
| `CurrentChoices` | `IReadOnlyList<DialogChoiceViewModel>` | Currently visible choices on the active node. |

#### Events

| Event | Signature | Description |
| --- | --- | --- |
| `OnStarted` | `Action<DialogGraph>` | Fired when a dialog starts. |
| `OnLineShown` | `Action<DialogLineViewModel>` | Fired whenever a node line is presented. |
| `OnChoicesShown` | `Action<IReadOnlyList<DialogChoiceViewModel>>` | Fired when visible choices are presented. |
| `OnCommandExecuted` | `Action<DialogCommand>` | Fired after each command executes. |
| `OnEnded` | `Action<DialogGraph>` | Fired when a dialog ends or is stopped. |

#### Methods

| Method | Return | Description |
| --- | --- | --- |
| `RegisterCommandHandler(string commandName, Action<DialogCommand> handler)` | `void` | Registers a case-insensitive command handler. |
| `UnregisterCommandHandler(string commandName)` | `bool` | Removes a command handler. |
| `Start(DialogGraph graph, string startNodeId = null, bool applyDefaultVariables = true)` | `void` | Starts execution at `startNodeId` (or `graph.StartNodeId`). |
| `Continue()` | `void` | Advances a linear node or ends an `IsEnd` node. Ignored while waiting for a choice. |
| `SelectChoice(int visibleChoiceIndex)` | `void` | Selects a visible choice, applies its mutations/commands, and enters its target node. |
| `Stop()` | `void` | Immediately terminates the active dialog and notifies `View`/`OnEnded`. |
| `FormatText(string key)` | `string` | Localizes `key` and interpolates `{variableName}` tokens. |

---

## Namespace: `ContentContent.Dialog.Unity`

### `DialogGraphAsset` (`ScriptableObject`)

Create via **Assets → Create → ContentContent → Dialog → Dialog Graph**.

| Member | Type | Description |
| --- | --- | --- |
| `GraphId` | `string` | Custom graph ID, falling back to asset `name`. |
| `StartNodeId` | `string` | Entry node ID. |
| `BuildGraph()` | `DialogGraph` | Compiles Inspector data into a runtime `DialogGraph`. |

---

### `DialogLocalizationTableAsset` (`ScriptableObject`, `IDialogLocalizer`)

Create via **Assets → Create → ContentContent → Dialog → Localization Table**.

| Member | Type | Description |
| --- | --- | --- |
| `Localize(string key)` | `string` | Looks up `key`, falling back to `_fallbackTable`, then `<MISSING:key>`. |
| `RebuildLookup()` | `void` | Rebuilds internal dictionary cache after runtime edits. |

---

### `DialogRunnerBehaviour` (`MonoBehaviour`)

Thin component wrapper around `DialogRunner` and `DialogVariableStore`.

#### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Runner` | `DialogRunner` | Underlying pure C# runner (lazily initialized). |
| `Variables` | `DialogVariableStore` | Underlying variable store. |
| `IsRunning` | `bool` | `true` while a dialog is active. |

#### Methods

```csharp
void SetView(IDialogView view)
void SetLocalizer(IDialogLocalizer localizer)
void StartDialog(DialogGraphAsset graphAsset, string startNodeId = null)
void StartDialog(DialogGraph graph, string startNodeId = null, bool applyDefaultVariables = true)
void Continue()
void SelectChoice(int visibleChoiceIndex)
void StopDialog()
void RegisterCommandHandler(string commandName, Action<DialogCommand> handler)
```

#### Inspector Events

- `_onDialogStarted` (`UnityEvent`)
- `_onDialogEnded` (`UnityEvent`)
- `_onCommandExecuted` (`UnityEvent<string>`) — passes the command name.

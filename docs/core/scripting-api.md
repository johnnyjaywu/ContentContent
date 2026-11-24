# Core Scripting API

Complete reference for `com.contentcontent.core`:
- **HFSM**: `ContentContent.Core.Fsm` and `ContentContent.Core.Fsm.Unity`
- **Grid**: `ContentContent.Core.Grid` and `ContentContent.Core.Grid.Unity`
- **EventBus**: `ContentContent.Core.Events` and `ContentContent.Core.Events.Unity`
- **Timers**: `ContentContent.Core.Timers` and `ContentContent.Core.Timers.Unity`
- **Random**: `ContentContent.Core.Random`
- **Commands**: `ContentContent.Core.Commands`
- **Blackboard**: `ContentContent.Core.Blackboards` and `ContentContent.Core.Blackboards.Unity`

---

## Namespace: `ContentContent.Core.Fsm`

### `IState` (interface)

| Method | Return | Description |
| --- | --- | --- |
| `OnEnter()` | `void` | Called once when the state becomes active. |
| `OnTick(float deltaTime)` | `void` | Called every update tick while active. |
| `OnFixedTick(float fixedDeltaTime)` | `void` | Called every fixed-timestep tick while active. |
| `OnExit()` | `void` | Called once before transitioning away. |

---

### `StateBase` & `StateBase<TContext>` (abstract classes)

- `StateBase`: Implements `IState` with virtual no-op methods.
- `StateBase<TContext>`: Extends `StateBase` with a `protected TContext Context { get; }` property initialized via `StateBase(TContext context)`.

---

### `ActionState` (`sealed class : IState`)

```csharp
public ActionState(
    string name = null,
    Action onEnter = null,
    Action<float> onTick = null,
    Action<float> onFixedTick = null,
    Action onExit = null)
```

Exposes `string Name { get; }` and overrides `ToString()` to return `Name`.

---

### `ITransition` & `Transition`

| Member | Type / Return | Description |
| --- | --- | --- |
| `TargetState` | `IState` | Destination state when triggered. |
| `Priority` | `int` | Higher values are evaluated first. |
| `AllowSelfTransition` | `bool` | Whether transitioning to the already-active state is permitted. |
| `Evaluate()` | `bool` | Returns `true` when the transition condition is satisfied. |

```csharp
public Transition(
    IState targetState,
    Func<bool> condition,
    int priority = 0,
    bool allowSelfTransition = false)
```

---

### `StateMachine` (`class : IState`)

```csharp
public StateMachine(IState initialState = null)
```

| Member | Type / Return | Description |
| --- | --- | --- |
| `CurrentState` | `IState` | Currently active state, or `null` if not entered. |
| `InitialState` | `IState` | Default state entered on `OnEnter()`. |
| `OnStateChanged` | `event Action<IState, IState>` | Invoked as `(previousState, nextState)`. |
| `AddTransition(IState from, IState to, Func<bool> condition, int priority = 0, bool allowSelfTransition = false)` | `void` | Registers a state-specific predicate transition. |
| `AddTransition(IState from, ITransition transition)` | `void` | Registers a custom state-specific `ITransition`. |
| `AddAnyTransition(IState to, Func<bool> condition, int priority = 0, bool allowSelfTransition = false)` | `void` | Registers a global transition evaluated before state-specific ones. |
| `AddAnyTransition(ITransition transition)` | `void` | Registers a custom global `ITransition`. |
| `SetState(IState nextState, bool allowSelfTransition = false)` | `void` | Immediately switches to `nextState`. |
| `OnEnter()` / `OnTick(float)` / `OnFixedTick(float)` / `OnExit()` | `void` | `IState` lifecycle (enables nesting inside parent `StateMachine`s). |

---

## Namespace: `ContentContent.Core.Fsm.Unity`

### `StateMachineRunner` (`MonoBehaviour`)

Serialized fields: `_useUnscaledTime`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Machine` | `StateMachine` | Currently bound state machine. |
| `CurrentStateName` | `string` | `Machine?.CurrentState?.ToString() ?? "None"`. |
| `Initialize(StateMachine machine, bool enterImmediately = true)` | `void` | Exits any previous machine, binds `machine`, and optionally calls `OnEnter()`. |

---

## Namespace: `ContentContent.Core.Grid`

### `GridCoord` (`readonly struct`, `IEquatable<GridCoord>`)

Immutable 2D integer coordinate.

#### Constants & Direction Tables

| Member | Value |
| --- | --- |
| `Zero` | `(0, 0)` |
| `Up`, `Down`, `Left`, `Right` | `(0, 1)`, `(0, -1)`, `(-1, 0)`, `(1, 0)` |
| `CardinalDirections` | `[Up, Right, Down, Left]` (4-way) |
| `AllDirections` | 8-way clockwise directions starting at `Up` |

#### Properties, Methods & Operators

| Member | Type / Return | Description |
| --- | --- | --- |
| `X`, `Y` | `int` | Integer coordinates. |
| `SqrMagnitude` | `int` | `X * X + Y * Y`. |
| `ManhattanDistance(GridCoord a, GridCoord b)` | `int` | `|a.X - b.X| + |a.Y - b.Y|`. |
| `ChebyshevDistance(GridCoord a, GridCoord b)` | `int` | `max(|a.X - b.X|, |a.Y - b.Y|)`. |
| `+`, `-`, `* int`, `==`, `!=` | `GridCoord` / `bool` | Component-wise arithmetic and equality operators. |

---

### `IGrid<TCell>` (interface)

| Member | Type / Return | Description |
| --- | --- | --- |
| `Width`, `Height`, `Count` | `int` | Grid dimensions and total cell count. |
| `this[int x, int y]` / `this[GridCoord coord]` | `TCell` | Bounds-checked indexer. |
| `OnCellChanged` | `event Action<GridCoord, TCell, TCell>` | Invoked as `(coord, oldValue, newValue)`. |
| `IsInBounds(int x, int y)` / `IsInBounds(GridCoord coord)` | `bool` | Returns `true` when inside `[0, Width) x [0, Height)`. |
| `TryGet(GridCoord coord, out TCell value)` | `bool` | Safe bounds-checked read. |
| `TrySet(GridCoord coord, TCell value)` | `bool` | Safe bounds-checked write. |
| `GetNeighbors(GridCoord coord, List<GridCoord> results, bool includeDiagonals = false)` | `void` | Clears and populates `results` with in-bounds neighbors. |

---

### `Grid2D<TCell>` (`class : IGrid<TCell>`)

```csharp
public Grid2D(int width, int height, Func<GridCoord, TCell> initialFactory = null)
```

In addition to `IGrid<TCell>` members:

| Method | Return | Description |
| --- | --- | --- |
| `Fill(TCell value, bool notify = true)` | `void` | Fills every cell with `value`. |
| `Fill(Func<GridCoord, TCell> factory, bool notify = true)` | `void` | Fills every cell using `factory(coord)`. |
| `ToIndex(int x, int y)` / `ToIndex(GridCoord coord)` | `int` | Converts a 2D coordinate to a flat array index (`y * Width + x`). |
| `FromIndex(int index)` | `GridCoord` | Converts a flat array index back to a `GridCoord`. |

---

## Namespace: `ContentContent.Core.Grid.Unity`

### `GridPlane` (enum)

| Value | Description |
| --- | --- |
| `XY` | 2D games / vertical grids (`X` = right, `Y` = up). |
| `XZ` | 3D top-down / strategy ground grids (`X` = right, `Z` = forward). |

---

### `UnityGridTransform` (`[Serializable] struct`)

```csharp
public UnityGridTransform(Vector3 origin, float cellSize = 1f, GridPlane plane = GridPlane.XZ)
```

Fields: `origin` (`Vector3`), `cellSize` (`float`), `plane` (`GridPlane`).

| Method | Return | Description |
| --- | --- | --- |
| `GridToWorldCorner(GridCoord coord)` | `Vector3` | World-space bottom-left corner of `coord`. |
| `GridToWorldCenter(GridCoord coord)` | `Vector3` | World-space center of `coord`. |
| `WorldToGrid(Vector3 worldPosition)` | `GridCoord` | Containing `GridCoord` for `worldPosition`. |
| `ToGridCoord(Vector2Int v)` | `GridCoord` | Static conversion from `Vector2Int`. |
| `ToVector2Int(GridCoord c)` | `Vector2Int` | Static conversion to `Vector2Int`. |

---

### `GridBehaviour` (`MonoBehaviour`)

Serialized fields: `_width`, `_height`, `_cellSize`, `_plane`, `_gizmoColor`. Draws wireframe Gizmos in `OnDrawGizmosSelected()`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Width`, `Height`, `CellSize`, `Plane` | `int` / `float` / `GridPlane` | Configured grid parameters. |
| `GridTransform` | `UnityGridTransform` | Transform anchored at `transform.position`. |
| `CreateGrid<TCell>(Func<GridCoord, TCell> initialFactory = null)` | `Grid2D<TCell>` | Creates a new `Grid2D<TCell>` sized to this component. |
| `GridToWorldCenter(GridCoord coord)` | `Vector3` | Delegates to `GridTransform.GridToWorldCenter`. |
| `GridToWorldCorner(GridCoord coord)` | `Vector3` | Delegates to `GridTransform.GridToWorldCorner`. |
| `WorldToGrid(Vector3 worldPos)` | `GridCoord` | Delegates to `GridTransform.WorldToGrid`. |

---

### `GridCoord3D`, `IGrid3D<TCell>`, `Grid3D<TCell>` & `UnityGridTransform3D`

3D counterparts to the 2D grid types:
- **`ContentContent.Core.Grid.GridCoord3D`**: Immutable `(X, Y, Z)` struct with `Zero`, `Right`, `Left`, `Up`, `Down`, `Forward`, `Back`, `CardinalDirections` (6-way), `AllDirections` (26-way), `ManhattanDistance`, `ChebyshevDistance`, and `+`/`-`/`*`/`==`/`!=`.
- **`ContentContent.Core.Grid.IGrid3D<TCell>` & `Grid3D<TCell>`**: Flat-array 3D grid (`x + Width * (y + Height * z)`) with `Width`, `Height`, `Depth`, `Count`, indexers, `OnCellChanged`, `IsInBounds`, `TryGet`, `TrySet`, `Fill`, `GetNeighbors`, `ToIndex`, and `FromIndex`.
- **`ContentContent.Core.Grid.Unity.UnityGridTransform3D`**: `[Serializable]` struct mapping `GridCoord3D` ↔ `Vector3` (`GridToWorldCorner`, `GridToWorldCenter`, `WorldToGrid`, `ToGridCoord3D`, `ToVector3Int`).

---

## Namespace: `ContentContent.Core.Events` & `.Unity`

- **`EventCallback<T>`**: `delegate void EventCallback<T>(in T evt)` for zero-copy struct event dispatch.
- **`IEventBus` / `EventBus`**: `EventBus(Action<Exception> errorHandler = null)` with `Subscribe<T>(EventCallback<T>, int priority = 0)`, `Subscribe<T>(Action<T>, int priority = 0)`, `Publish<T>(in T evt)`, `GetSubscriberCount<T>()`, `Clear<T>()`, and `Clear()`.
- **`EventSubscription`** (`readonly struct : IDisposable`): `IsValid`, `Dispose()` (idempotent, re-entrancy safe).
- **`EventBusBehaviour`** (`MonoBehaviour`, `.Unity`): Exposes lazily initialized `EventBus Bus` and clears listeners in `OnDestroy()`.

---

## Namespace: `ContentContent.Core.Timers` & `.Unity`

- **`TimerScheduler`**: `TimerScheduler(int initialCapacity = 16, int maxFiresPerTick = 16)` with `ActiveCount`, `TimeScale`, `MaxFiresPerTick`, `After(float delay, Action callback)`, `Every(float interval, Action callback, int repeatCount = -1)`, `Tick(float deltaTime)`, `IsActive`, `IsPaused`, `GetRemaining`, `Cancel`, `Pause`, `Resume`, `CancelAll()`.
- **`TimerHandle`** (`readonly struct`): `IsActive`, `IsPaused`, `Remaining`, `Cancel()`, `Pause()`, `Resume()`.
- **`Cooldown`** (`struct`): `Cooldown(float duration, bool startReady = true)` with `Duration`, `Remaining`, `IsReady`, `Progress`, `Trigger()`, `Trigger(float)`, `TryConsume()`, `Tick(float)`, `Reset()`.
- **`TimerSchedulerRunner`** (`MonoBehaviour`, `.Unity`): Exposes `TimerScheduler Scheduler` and ticks it in `Update()` with optional unscaled time.

---

## Namespace: `ContentContent.Core.Random`

- **`IRandom`**: `NextUInt()`, `NextInt()`, `NextInt(int maxExclusive)`, `NextInt(int minInclusive, int maxExclusive)`, `NextFloat()`, `NextFloat(float minInclusive, float maxExclusive)`, `NextDouble()`, `NextBool(float probability = 0.5f)`.
- **`RngState`** (`[Serializable] readonly struct`): `S0`, `S1`, `S2`, `S3`, `IsAllZero`.
- **`Xoshiro128StarStar`** (`IRandom`): Constructors for `ulong`, `int`, `RngState`; `Seed(ulong)`, `GetState()`, `SetState(RngState)`, `Fork()`.
- **`RandomExtensions`**: `Shuffle<T>(IList<T>)`, `Pick<T>(IReadOnlyList<T>)`, `RollDice(int count, int sides)`.
- **`WeightedTable<T>`**: `Count`, `TotalWeight`, `this[int]`, `Add(T, float)`, `RemoveAt(int)`, `Clear()`, `Pick(IRandom)`.

---

## Namespace: `ContentContent.Core.Commands`

- **`ICommand`**: `Execute()`, `Undo()`.
- **`IMergeableCommand`** (`ICommand`): `TryMerge(ICommand next)`.
- **`ActionCommand`** (`ICommand`): Delegate-backed `(Action execute, Action undo)`.
- **`CompositeCommand`** (`ICommand`): Executes in order, undoes in reverse order.
- **`CommandHistory`**: `MaxCapacity`, `UndoCount`, `RedoCount`, `CanUndo`, `CanRedo`, `IsInTransaction`, `OnHistoryChanged`, `Execute(ICommand)`, `Undo()`, `Redo()`, `BeginTransaction()` (`TransactionScope` with `Rollback()`/`Dispose()`), `Clear()`.
- **`CommandQueue`**: `CommandQueue(CommandHistory history = null)` with `Count`, `IsEmpty`, `Enqueue(ICommand)`, `ExecuteNext()`, `ExecuteAll()`, `Clear()`.

---

## Namespace: `ContentContent.Core.Blackboards` & `.Unity`

- **`BlackboardKey<T>`** (`readonly struct`) & **`BlackboardKey.Create<T>(string name)`**: Stateless typed key with precomputed FNV-1a `Hash`.
- **`BlackboardSubscription`** (`readonly struct : IDisposable`): `IsValid`, `Dispose()`.
- **`Blackboard`**: `Blackboard(Blackboard parent = null)` with `Parent`, `Set<T>`, `TryGet<T>`, `Get<T>`, `ContainsLocal<T>`, `Contains<T>`, `Remove<T>`, `Subscribe<T>(key, Action<T, T>)`, `ClearValues()`, `ClearAll()`.
- **`BlackboardFsmExtensions`**: `AddTransitionWhen<T>(...)` and `AddAnyTransitionWhen<T>(...)` on `StateMachine`.
- **`BlackboardBehaviour`** (`MonoBehaviour`, `.Unity`): Exposes `Blackboard Blackboard` with optional `_parentBlackboard` and clears on `OnDestroy()`.

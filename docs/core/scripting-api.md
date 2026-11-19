# Core Scripting API

Complete reference for `com.contentcontent.core`:
- **Pooling**: `ContentContent.Core.Pooling` and `ContentContent.Core.Pooling.Unity`
- **HFSM**: `ContentContent.Core.Fsm` and `ContentContent.Core.Fsm.Unity`
- **Grid**: `ContentContent.Core.Grid` and `ContentContent.Core.Grid.Unity`

---

## Namespace: `ContentContent.Core.Pooling`

### `IPoolable` (interface)

Optional lifecycle interface automatically invoked by `ObjectPool<T>`.

| Method | Return | Description |
| --- | --- | --- |
| `OnSpawned()` | `void` | Called when the object is retrieved from a pool. |
| `OnDespawned()` | `void` | Called when the object is returned to a pool. |

---

### `IObjectPool<T>` (interface, `where T : class`)

| Member | Type / Return | Description |
| --- | --- | --- |
| `CountActive` | `int` | Number of instances currently leased out. |
| `CountInactive` | `int` | Number of idle instances stored in the pool. |
| `CountAll` | `int` | Total active + inactive instances. |
| `Get()` | `T` | Retrieves an instance or creates one if empty. |
| `Get(out T instance)` | `PooledHandle<T>` | Retrieves an instance wrapped in a disposable scope guard. |
| `Release(T instance)` | `void` | Returns an instance to the pool. |
| `Prewarm(int count)` | `void` | Pre-allocates up to `count` inactive instances (clamped to max size). |
| `Clear()` | `void` | Destroys and removes all inactive instances. |

---

### `ObjectPool<T>` (`IObjectPool<T>`, `IDisposable`)

Stack-backed generic pool.

```csharp
public ObjectPool(
    Func<T> createFunc,
    Action<T> onGet = null,
    Action<T> onRelease = null,
    Action<T> onDestroy = null,
    bool collectionCheck = true,
    int defaultCapacity = 16,
    int maxSize = 1024)
```

Implements all `IObjectPool<T>` members plus `Dispose()` (calls `Clear()`).

---

### `PooledHandle<T>` (`readonly struct`, `IDisposable`)

| Member | Type / Return | Description |
| --- | --- | --- |
| `Value` | `T` | The leased instance. |
| `Dispose()` | `void` | Releases `Value` back to its owning pool. |

---

### `PoolRegistry<TKey>` (`IDisposable`)

Keyed container for managing multiple `IObjectPool<T>` instances without static singletons.

| Method | Return | Description |
| --- | --- | --- |
| `Register<T>(TKey key, IObjectPool<T> pool)` | `void` | Registers a pool under `key`. Throws if already registered. |
| `TryGetPool<T>(TKey key, out IObjectPool<T> pool)` | `bool` | Attempts to retrieve a typed pool by key. |
| `GetPool<T>(TKey key)` | `IObjectPool<T>` | Retrieves a typed pool by key or throws `KeyNotFoundException`. |
| `Contains(TKey key)` | `bool` | Returns `true` if `key` is registered. |
| `ClearAll()` | `void` | Calls `Clear()` on every registered pool. |
| `Dispose()` | `void` | Calls `ClearAll()` and removes all registrations. |

---

## Namespace: `ContentContent.Core.Pooling.Unity`

### `ComponentPool<T>` (`IObjectPool<T>`, `IDisposable`, `where T : Component`)

```csharp
public ComponentPool(
    T prefab,
    Transform defaultParent = null,
    int defaultCapacity = 16,
    int maxSize = 256,
    bool collectionCheck = true)
```

In addition to `IObjectPool<T>` members:
- `Get(Vector3 position, Quaternion rotation, Transform parent = null)` → `T`: Retrieves an instance and sets its transform.

---

### `PooledObject` (`MonoBehaviour`)

| Method | Return | Description |
| --- | --- | --- |
| `DespawnAfter(float seconds)` | `void` | Schedules automatic return to its owning pool after `seconds`. |
| `CancelTimedDespawn()` | `void` | Cancels any pending timed despawn. |
| `ReleaseToPool()` | `void` | Returns this GameObject to its pool, or destroys it if unbound. |

---

### `PrefabPoolBehaviour` (`MonoBehaviour`)

Serialized fields: `_prefab`, `_container`, `_prewarmCount` (default `8`), `_maxSize` (default `128`), `_collectionCheck` (default `true`).

| Member | Type / Return | Description |
| --- | --- | --- |
| `CountActive` | `int` | Active instances currently spawned. |
| `CountInactive` | `int` | Inactive instances stored in the pool. |
| `Spawn(Vector3 position, Quaternion rotation, Transform parent = null)` | `GameObject` | Spawns and positions a pooled instance. |
| `Despawn(GameObject instance)` | `void` | Returns an instance to the pool. |

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

# How Core Works

`com.contentcontent.core` provides foundational, zero-dependency C# utilities organized into three cohesive sub-domains — **Object Pooling**, **Hierarchical Finite State Machine (HFSM)**, and **2D Grid** — each paired with thin Unity lifecycle and scene adapters.

## Package Architecture

```text
Packages/com.contentcontent.core/
├── Runtime/
│   ├── ContentContent.Core.asmdef            # Pure C# (noEngineReferences: true)
│   ├── Pooling/
│   │   ├── IPoolable.cs                      # Optional OnSpawned / OnDespawned hooks
│   │   ├── IObjectPool.cs                    # Generic pool contract
│   │   ├── ObjectPool.cs                     # Stack-backed pool with collection checks
│   │   ├── PooledHandle.cs                   # IDisposable scope guard for auto-release
│   │   └── PoolRegistry.cs                   # Keyed container for multiple pools
│   ├── Fsm/
│   │   ├── IState.cs                         # OnEnter / OnTick / OnFixedTick / OnExit
│   │   ├── ITransition.cs                    # Priority-ordered conditional transition
│   │   ├── StateBase.cs                      # Virtual no-op base (+ typed Context variant)
│   │   ├── ActionState.cs                    # Delegate-based lightweight state
│   │   ├── Transition.cs                     # Func<bool> transition implementation
│   │   └── StateMachine.cs                   # Hierarchical FSM (implements IState)
│   ├── Grid/
│   │   ├── GridCoord.cs                      # Immutable 2D integer coordinate + directions
│   │   ├── GridCoord3D.cs                    # Immutable 3D integer coordinate + 6/26-way directions
│   │   ├── IGrid.cs                          # Generic bounded 2D grid contract
│   │   ├── IGrid3D.cs                        # Generic bounded 3D grid contract
│   │   ├── Grid2D.cs                         # Flat-array 2D grid + OnCellChanged
│   │   └── Grid3D.cs                         # Flat-array 3D grid + OnCellChanged
│   └── Unity/
│       ├── ContentContent.Core.Unity.asmdef  # Thin Unity adapters referencing Core
│       ├── Pooling/
│       │   ├── ComponentPool.cs              # Prefab Component pool adapter
│       │   ├── PooledObject.cs               # Self-release & timed despawn component
│       │   └── PrefabPoolBehaviour.cs        # Inspector-driven single-prefab pool
│       ├── Fsm/
│       │   └── StateMachineRunner.cs         # MonoBehaviour Update/FixedUpdate driver
│       └── Grid/
│           ├── UnityGridTransform.cs         # XY / XZ world-to-grid coordinate mapping
│           ├── UnityGridTransform3D.cs       # 3D XYZ world-to-grid coordinate mapping
│           └── GridBehaviour.cs              # Scene MonoBehaviour + Gizmos wireframe
└── Tests/EditMode/
    ├── ContentContent.Core.Tests.asmdef
    ├── ObjectPoolTests.cs                    # Pooling NUnit tests
    ├── StateMachineTests.cs                  # HFSM NUnit tests
    ├── GridTests.cs                          # Grid2D NUnit tests
    └── Grid3DTests.cs                        # Grid3D NUnit tests
```

Because `ContentContent.Core.asmdef` sets `"noEngineReferences": true`, all three sub-domains compile independently of `UnityEngine`, run deterministically, and can be unit-tested in EditMode without scenes or GameObjects.

---

## Sub-Domain 1: Object Pooling (`ContentContent.Core.Pooling`)

- **`ObjectPool<T>`**: A stack-backed generic pool that reuses instances to avoid GC pressure.
  - Optional **`collectionCheck`** uses a `HashSet<T>` to throw immediately if an object is released twice.
  - Enforces **`maxSize`**: excess released items invoke `onDestroy` instead of growing unbounded.
  - Automatically invokes `IPoolable.OnSpawned()` on `Get()` and `IPoolable.OnDespawned()` on `Release()`.
- **`PooledHandle<T>`**: A `readonly struct` returned by `pool.Get(out var item)` for `using` scopes so leased objects are guaranteed to return to the pool even on early returns or exceptions.
- **`PoolRegistry<TKey>`**: Manages multiple typed pools by key without static singletons, with `ClearAll()` and `Dispose()` support.
- **Unity Adapters (`ContentContent.Core.Pooling.Unity`)**:
  - `ComponentPool<T>` wraps `ObjectPool<T>` for prefab `Component` instantiation, `SetActive` toggling, and parent re-homing.
  - `PrefabPoolBehaviour` provides a drop-in Inspector component for pooling a prefab, automatically binding any `PooledObject` component on spawned instances for manual or timed despawns (`DespawnAfter(seconds)`).

---

## Sub-Domain 2: Hierarchical State Machine (`ContentContent.Core.Fsm`)

- **Composite HFSM Design**: `StateMachine` implements `IState`, so any `StateMachine` can be nested directly as a sub-state inside a parent `StateMachine`. Entering, ticking, fixed-ticking, and exiting propagate cleanly through the hierarchy.
- **Transition Evaluation Order**: On each `OnTick(deltaTime)`:
  1. **Any-State Transitions** (`AddAnyTransition`) are evaluated first, sorted by descending `Priority`.
  2. **State-Specific Transitions** (`AddTransition`) for `CurrentState` are evaluated next, also sorted by descending `Priority`.
  3. By default, transitions to the already-active state are skipped unless `allowSelfTransition: true`.
  4. If a transition fires, `SetState` runs `previous.OnExit()`, switches `CurrentState`, runs `next.OnEnter()`, and raises `OnStateChanged`.
  5. Finally, `CurrentState.OnTick(deltaTime)` runs.
- **Lightweight Authoring**: Implement `IState`, inherit from `StateBase` / `StateBase<TContext>`, or construct inline `ActionState` instances with delegates.
- **Unity Adapter (`ContentContent.Core.Fsm.Unity`)**: `StateMachineRunner` binds a `StateMachine` to Unity's `Update`, `FixedUpdate`, and `OnDisable` callbacks with optional unscaled time.

---

## Sub-Domain 3: 2D Grid (`ContentContent.Core.Grid`)

- **`GridCoord`**: An immutable `readonly struct` representing `(X, Y)` integer coordinates with value equality, arithmetic operators (`+`, `-`, `*`), `ManhattanDistance`, `ChebyshevDistance`, and pre-allocated `CardinalDirections` (4-way) and `AllDirections` (8-way) arrays.
- **`IGrid<TCell>` & `Grid2D<TCell>`**:
  - Backed by a contiguous 1D array (`TCell[width * height]`) for cache-friendly random access and index conversion (`ToIndex` / `FromIndex`).
  - Zero-allocation neighbor queries via `GetNeighbors(coord, results, includeDiagonals)` that reuse a caller-provided `List<GridCoord>`.
  - Change notification via `OnCellChanged(coord, oldValue, newValue)`, fired only when the new value differs from the old value.
  - Decoupled from pathfinding: algorithms like A* live in `com.contentcontent.navigation` and depend only on `IGrid<TCell>` and `GridCoord`.
- **Unity Adapters (`ContentContent.Core.Grid.Unity`)**:
  - `UnityGridTransform` maps `GridCoord` to and from world-space `Vector3` corners and centers on either the `XY` (2D) or `XZ` (3D top-down) plane, plus `Vector2Int` conversions.
  - `GridBehaviour` exposes grid dimensions, cell size, plane, and Scene-view wireframe Gizmos, and acts as a factory via `CreateGrid<TCell>()`.

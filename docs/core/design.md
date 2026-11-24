# How Core Works

`com.contentcontent.core` provides foundational, zero-dependency C# utilities organized into seven cohesive sub-domains — **Hierarchical Finite State Machine (HFSM)**, **2D/3D Grid**, **EventBus**, **Timers & Cooldowns**, **Deterministic RNG**, **Commands (Undo/Redo)**, and **Blackboard** — paired with thin Unity lifecycle and scene adapters.

## Package Architecture

```text
Packages/com.contentcontent.core/
├── Runtime/
│   ├── ContentContent.Core.asmdef            # Pure C# (noEngineReferences: true)
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
│   ├── Events/
│   │   ├── IEventBus.cs                      # Typed pub-sub contract + EventCallback<T>
│   │   ├── EventSubscription.cs              # IDisposable subscription handle
│   │   └── EventBus.cs                       # Zero-boxing priority & re-entrant safe bus
│   ├── Timers/
│   │   ├── TimerHandle.cs                    # Generational value-type handle
│   │   ├── TimerScheduler.cs                 # Tick-driven scheduler with catch-up cap
│   │   └── Cooldown.cs                       # Allocation-free value-type cooldown
│   ├── Random/
│   │   ├── IRandom.cs                        # Deterministic RNG contract
│   │   ├── RngState.cs                       # Serializable 128-bit state snapshot
│   │   ├── Xoshiro128StarStar.cs             # Cross-platform PRNG + Fork()
│   │   ├── RandomExtensions.cs               # Shuffle, Pick, RollDice helpers
│   │   └── WeightedTable.cs                  # Lazy prefix-sum O(log n) weighted picker
│   ├── Commands/
│   │   ├── ICommand.cs                       # Execute / Undo contract
│   │   ├── IMergeableCommand.cs              # Coalescing consecutive edits
│   │   ├── ActionCommand.cs                  # Delegate-backed reversible command
│   │   ├── CompositeCommand.cs               # Atomic multi-command batch
│   │   ├── CommandHistory.cs                 # Bounded undo/redo + transactions + merging
│   │   └── CommandQueue.cs                   # FIFO deferred/turn-based command queue
│   ├── Blackboard/
│   │   ├── BlackboardKey.cs                  # Stateless FNV-1a typed key struct
│   │   ├── Blackboard.cs                     # Zero-boxing scoped key-value store + events
│   │   └── BlackboardFsmExtensions.cs        # Opt-in HFSM transition helpers
│   └── Unity/
│       ├── ContentContent.Core.Unity.asmdef  # Thin Unity adapters referencing Core
│       ├── Fsm/
│       │   └── StateMachineRunner.cs         # MonoBehaviour Update/FixedUpdate driver
│       ├── Grid/
│       │   ├── UnityGridTransform.cs         # XY / XZ world-to-grid coordinate mapping
│       │   ├── UnityGridTransform3D.cs       # 3D XYZ world-to-grid coordinate mapping
│       │   └── GridBehaviour.cs              # Scene MonoBehaviour + Gizmos wireframe
│       ├── Events/
│       │   └── EventBusBehaviour.cs          # Scene/GameObject-scoped EventBus adapter
│       ├── Timers/
│       │   └── TimerSchedulerRunner.cs       # MonoBehaviour Update driver for TimerScheduler
│       └── Blackboard/
│           └── BlackboardBehaviour.cs        # MonoBehaviour Blackboard + parent scope
└── Tests/EditMode/
    ├── ContentContent.Core.Tests.asmdef
    ├── StateMachineTests.cs                  # HFSM NUnit tests
    ├── GridTests.cs                          # Grid2D NUnit tests
    ├── Grid3DTests.cs                        # Grid3D NUnit tests
    ├── EventBusTests.cs                      # EventBus NUnit tests
    ├── TimersTests.cs                        # TimerScheduler & Cooldown NUnit tests
    ├── RandomTests.cs                        # Xoshiro128** & WeightedTable NUnit tests
    ├── CommandsTests.cs                      # CommandHistory & CommandQueue NUnit tests
    └── BlackboardTests.cs                    # Blackboard & HFSM integration NUnit tests
```

Because `ContentContent.Core.asmdef` sets `"noEngineReferences": true`, all seven sub-domains compile independently of `UnityEngine`, run deterministically, and can be unit-tested in EditMode or via `dotnet test` without scenes or GameObjects.

---

## Sub-Domain 1: Hierarchical State Machine (`ContentContent.Core.Fsm`)

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

## Sub-Domain 2: 2D & 3D Grid (`ContentContent.Core.Grid`)

- **`GridCoord`**: An immutable `readonly struct` representing `(X, Y)` integer coordinates with value equality, arithmetic operators (`+`, `-`, `*`), `ManhattanDistance`, `ChebyshevDistance`, and pre-allocated `CardinalDirections` (4-way) and `AllDirections` (8-way) arrays.
- **`IGrid<TCell>` & `Grid2D<TCell>`**:
  - Backed by a contiguous 1D array (`TCell[width * height]`) for cache-friendly random access and index conversion (`ToIndex` / `FromIndex`).
  - Zero-allocation neighbor queries via `GetNeighbors(coord, results, includeDiagonals)` that reuse a caller-provided `List<GridCoord>`.
  - Change notification via `OnCellChanged(coord, oldValue, newValue)`, fired only when the new value differs from the old value.
  - Decoupled from pathfinding: algorithms like A* live in `com.contentcontent.navigation` and depend only on `IGrid<TCell>` and `GridCoord`.
- **Unity Adapters (`ContentContent.Core.Grid.Unity`)**:
  - `UnityGridTransform` maps `GridCoord` to and from world-space `Vector3` corners and centers on either the `XY` (2D) or `XZ` (3D top-down) plane, plus `Vector2Int` conversions.
  - `GridBehaviour` exposes grid dimensions, cell size, plane, and Scene-view wireframe Gizmos, and acts as a factory via `CreateGrid<TCell>()`.

---

## Sub-Domain 3: EventBus (`ContentContent.Core.Events`)

- **Instance-Based & Zero-Boxing**: `EventBus` owns typed per-event channels (`Channel<T>`) with no hidden static state (safe across Unity Domain Reloads). Struct events dispatch via `in T` with zero boxing.
- **Priority & Re-entrancy Safety**: Subscribers run in descending priority order (FIFO on ties). Subscribing during `Publish` defers activation until the outermost publish finishes; unsubscribing marks the listener inactive immediately so it is skipped in the current dispatch.
- **Fail-Fast or Isolated Errors**: Exceptions propagate immediately by default unless an optional `Action<Exception> errorHandler` is passed to `new EventBus(errorHandler)`.
- **Unity Adapter**: `EventBusBehaviour` scopes an `EventBus` to a GameObject/scene and clears listeners in `OnDestroy`.

---

## Sub-Domain 4: Timers & Cooldowns (`ContentContent.Core.Timers`)

- **`TimerScheduler`**: Tick-driven scheduler (`Tick(float deltaTime)`) backed by a generational slot array and free-list for zero-allocation slot reuse.
  - Supports one-shot (`After`), infinite or finite repeating (`Every`), `TimeScale`, `Pause`/`Resume`/`Cancel`, and remainder-preserving catch-up bounded by `MaxFiresPerTick`.
  - Value-type `TimerHandle` carries a generation counter so stale handles never cancel or pause recycled slots.
- **`Cooldown`**: Allocation-free `struct` for ability or weapon cooldowns (`TryConsume`, `Trigger`, `Tick`, `Progress`, `IsReady`, `Reset`).
- **Unity Adapter**: `TimerSchedulerRunner` ticks a `TimerScheduler` in `Update` with optional unscaled time.

---

## Sub-Domain 5: Deterministic RNG (`ContentContent.Core.Random`)

- **`Xoshiro128StarStar` (`IRandom`)**: Cross-platform deterministic 128-bit PRNG seeded via `SplitMix64`, with serializable `RngState` save/restore snapshots and `Fork()` for deriving independent child streams.
- **Utilities**: `WeightedTable<T>` (lazy prefix-sum + $O(\log n)$ binary search) and `RandomExtensions` (`Shuffle`, `Pick`, `RollDice`).

---

## Sub-Domain 6: Commands & Undo/Redo (`ContentContent.Core.Commands`)

- **`CommandHistory`**: Ring-buffered bounded undo/redo history with `IMergeableCommand` coalescing (for continuous edits like drags/painting) and `BeginTransaction()` scopes that group multiple commands into a single `CompositeCommand` (with `Rollback()` support).
- **`CommandQueue`**: FIFO deferred/turn-based execution queue that optionally forwards executed commands into a `CommandHistory`.

---

## Sub-Domain 7: Blackboard (`ContentContent.Core.Blackboards`)

- **Zero-Boxing Typed Keys**: `BlackboardKey<T>` is a stateless struct with a precomputed FNV-1a hash (no static registries). Values live inside generic `Entry<T>` slots without boxing.
- **Hierarchical Fallback & Reactivity**: `new Blackboard(parent)` falls back to parent values on reads while keeping writes local; `Subscribe<T>(key, (oldVal, newVal) => ...)` notifies only when values actually change.
- **Opt-In HFSM Composition**: `BlackboardFsmExtensions` adds `AddTransitionWhen<T>` and `AddAnyTransitionWhen<T>` on `StateMachine`.
- **Unity Adapter**: `BlackboardBehaviour` exposes a `Blackboard` with an optional parent `BlackboardBehaviour` reference.

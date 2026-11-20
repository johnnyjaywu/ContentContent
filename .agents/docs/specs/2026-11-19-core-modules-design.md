# Core Package Expansion — 6 Foundational Modules Design Spec

**Date:** 2026-11-19  
**Package:** `Packages/com.contentcontent.core` (`ContentContent.Core` / `ContentContent.Core.Unity`)  
**Compatibility:** C# 9.0, `netstandard2.1`, Unity `2022.3+`, zero `UnityEngine` references in `Runtime/`

## Architecture Principles

1. **Pure C# First:** Every module lives under `Runtime/<Module>/` in `ContentContent.Core` with `"noEngineReferences": true`, deterministic behavior, and allocation-conscious hot paths.
2. **Light Opt-In Composition:** Each module works standalone with zero required coupling, while offering clean opt-in integration where valuable (e.g., `Blackboard` + `StateMachine`, `EventBus` + `ServiceContainer`).
3. **Pragmatic Thin Unity Adapters:** Only add `MonoBehaviour` adapters under `Runtime/Unity/<Module>/` where Unity lifecycle (`Update`, `OnDisable`, `OnDestroy`) or scene/Inspector integration provides real value:
   - `Runtime/Unity/Timers/TimerSchedulerRunner.cs`
   - `Runtime/Unity/Services/SceneServiceScope.cs`
   - `Runtime/Unity/Blackboard/BlackboardBehaviour.cs`
   - `Runtime/Unity/Events/EventBusBehaviour.cs`

---

## Module 1: EventBus (`ContentContent.Core.Events`)

- **Files:** `EventBus.cs`, `EventSubscription.cs`, `IEventBus.cs`
- **Design:**
  - Instance-based `EventBus : IEventBus` (no hidden static state; Unity Domain Reload safe; injectable via `ServiceContainer`).
  - Generic per-type channels (`Channel<T>`) supporting any type `T` (structs dispatch via `in T` with zero boxing).
  - Priority ordering (higher priority runs first; ties preserve registration order).
  - Re-entrancy safe: subscribing or unsubscribing during `Publish<T>` defers structural list mutations until the outermost publish depth completes, while immediately marking unsubscribed listeners inactive so they are skipped in the current dispatch.
  - Immediate `Publish<T>(in T evt)` only. Exceptions propagate immediately unless an optional `Action<Exception> errorHandler` is configured in the constructor.
  - `Subscribe<T>(EventHandler<T> handler, int priority = 0)` and `Subscribe<T>(Action<T> handler, int priority = 0)` return a value-type `EventSubscription : IDisposable` token for deterministic cleanup.
  - `Clear()` and `Clear<T>()` remove all or type-specific listeners.

## Module 2: Timers (`ContentContent.Core.Timers`)

- **Files:** `TimerScheduler.cs`, `TimerHandle.cs`, `Cooldown.cs`
- **Design:**
  - `TimerScheduler` driven by `Tick(float deltaTime)` with `TimeScale` (default `1f`) and `MaxFiresPerTick` (default `16`) safety cap.
  - Generational slot array + free-list reuse (zero-alloc after warm-up; stale `TimerHandle`s are safely ignored).
  - Scheduling APIs:
    - `After(float delay, Action callback)` — one-shot timer.
    - `Every(float interval, Action callback, int repeatCount = -1)` — repeating timer (`-1` = infinite, `> 0` = finite count). Preserves remainder across ticks and catches up up to `MaxFiresPerTick`.
  - Handle controls via `TimerHandle` (value type) or `TimerScheduler` methods: `Cancel()`, `Pause()`, `Resume()`, `IsActive`, `IsPaused`, `Remaining`.
  - Re-entrancy safe: callbacks may schedule, cancel, pause, or resume timers during `Tick`.
  - Standalone `Cooldown` struct: `Duration`, `Remaining`, `IsReady`, `Progress` (`0..1`), `Trigger()`, `Trigger(float duration)`, `Tick(float deltaTime)`, `Reset()`, `TryConsume()`.
  - Unity adapter: `TimerSchedulerRunner` (`MonoBehaviour` driving a `TimerScheduler` in `Update` with `_useUnscaledTime`).

## Module 3: Services (`ContentContent.Core.Services`)

- **Files:** `IServiceResolver.cs`, `ServiceContainer.cs`, `ServiceLifetime.cs`
- **Design:**
  - Zero-reflection, AOT/IL2CPP-safe `ServiceContainer : IServiceResolver, IDisposable`.
  - Hierarchical scopes via `CreateScope()` (child resolves locally first, then falls back to parent; disposing a child scope does not affect the parent).
  - Registration APIs:
    - `RegisterInstance<T>(T instance, bool ownsInstance = false)`
    - `RegisterSingleton<T>(Func<IServiceResolver, T> factory)` (lazy-created once per declaring scope, cached, disposed with scope)
    - `RegisterTransient<T>(Func<IServiceResolver, T> factory)` (new instance per resolve)
    - Duplicate registration throws `InvalidOperationException` by default; explicit `ReplaceInstance<T>`, `ReplaceSingleton<T>`, `ReplaceTransient<T>` allow intentional overrides.
  - Resolution APIs: `Get<T>()`, `TryGet<T>(out T service)`, `IsRegistered<T>(bool includeParent = true)`.
  - Circular dependency detection during factory resolution (throws `InvalidOperationException`).
  - Reverse-order `IDisposable` teardown of owned instances and created singletons on `Dispose()`.
  - Unity adapter: `SceneServiceScope` (`MonoBehaviour` owning a `ServiceContainer` scope and disposing it in `OnDestroy`).

## Module 4: Deterministic RNG (`ContentContent.Core.Random`)

- **Files:** `IRandom.cs`, `RngState.cs`, `Xoshiro128StarStar.cs`, `WeightedTable.cs`, `RandomExtensions.cs`
- **Design:**
  - `IRandom` interface: `NextUInt()`, `NextInt()`, `NextInt(int maxExclusive)`, `NextInt(int minInclusive, int maxExclusive)`, `NextFloat()`, `NextFloat(float minInclusive, float maxExclusive)`, `NextDouble()`, `NextBool(float probability = 0.5f)`.
  - `Xoshiro128StarStar : IRandom`:
    - Pure C# 128-bit state initialized via `SplitMix64` from a `ulong` or `int` seed (cross-platform deterministic).
    - `RngState` serializable `readonly struct` (`S0`, `S1`, `S2`, `S3`) with `GetState()` / `SetState(RngState)` for exact save/load and replay.
    - `Fork()` produces an independent deterministic child `Xoshiro128StarStar` stream.
  - `RandomExtensions`: `Shuffle<T>(IList<T>)` (Fisher-Yates), `Pick<T>(IReadOnlyList<T>)`, `RollDice(int count, int sides)`.
  - `WeightedTable<T>`: mutable weighted selection table (`Add`, `RemoveAt`, `Clear`, `Count`, `TotalWeight`, `Pick(IRandom)`) using lazy-rebuilt prefix sums + binary search ($O(\log n)$ pick).

## Module 5: Commands (`ContentContent.Core.Commands`)

- **Files:** `ICommand.cs`, `IMergeableCommand.cs`, `ActionCommand.cs`, `CompositeCommand.cs`, `CommandHistory.cs`, `CommandQueue.cs`
- **Design:**
  - `ICommand`: `void Execute()`, `void Undo()`.
  - `IMergeableCommand : ICommand`: `bool TryMerge(ICommand next)` for coalescing consecutive edits.
  - `ActionCommand : ICommand`: lightweight delegate-backed command (`Action execute`, `Action undo`).
  - `CompositeCommand : ICommand`: executes child commands in order and undoes them in reverse order.
  - `CommandHistory`:
    - Bounded undo capacity (`MaxCapacity`, default `100`, evicts oldest when exceeded).
    - `Execute(ICommand)`, `Undo()`, `Redo()`, `CanUndo`, `CanRedo`, `UndoCount`, `RedoCount`, `Clear()`.
    - Coalescing: if top undo command implements `IMergeableCommand` and `TryMerge(next)` returns `true`, merges in place instead of pushing a new entry (when not inside a transaction).
    - Transactions: `BeginTransaction()` returns an `IDisposable` scope (or `CommitTransaction()` / `RollbackTransaction()`) that groups nested `Execute` calls into a single `CompositeCommand`.
    - `event Action OnHistoryChanged`.
  - `CommandQueue`: FIFO queue (`Enqueue`, `ExecuteNext`, `ExecuteAll`, `Clear`, `Count`, optional `CommandHistory` forwarding) for deferred or turn-based execution.

## Module 6: Blackboard (`ContentContent.Core.Blackboards`)

- **Files:** `BlackboardKey.cs`, `Blackboard.cs`, `BlackboardFsmExtensions.cs`
- **Design:**
  - `BlackboardKey<T>` (`readonly struct`, `IEquatable<BlackboardKey<T>>`): holds `string Name` and precomputed deterministic FNV-1a hash — stateless and Unity Domain Reload safe.
  - `Blackboard`:
    - Zero-boxing per-key storage using generic `Entry<T> : IBlackboardEntry` slots.
    - Optional parent `Blackboard` for read fallback (`TryGet`, `Get`, `Contains` check local then parent; `Set` and `Remove` operate locally unless targeting parent explicitly).
    - `Set<T>(BlackboardKey<T> key, T value)` compares with `EqualityComparer<T>.Default` and invokes typed `Action<T, T>` (`oldValue`, `newValue`) subscribers only when changed (or on initial set).
    - `Subscribe<T>(BlackboardKey<T> key, Action<T, T> handler)` returns an `IDisposable` subscription handle.
    - `Remove<T>`, `ContainsLocal<T>`, `Contains<T>`, `Clear()`.
  - Opt-in HFSM integration (`BlackboardFsmExtensions.cs` in `ContentContent.Core.Blackboard`): helper methods on `StateMachine` such as `AddTransitionWhen<T>(from, to, blackboard, key, predicate, priority)` and `AddAnyTransitionWhen<T>(...)` so HFSM transitions can cleanly observe blackboard keys without modifying core HFSM independence.
  - Unity adapter: `BlackboardBehaviour` (`MonoBehaviour` exposing a `Blackboard` instance and optional parent `BlackboardBehaviour` reference).

---

## Verification & Documentation Plan

- Run `dotnet test tools/PureTests/GameSystems.PureTests.csproj --no-restore` after each module.
- Generate valid `.meta` files for every new folder and `.cs` file in `Packages/com.contentcontent.core/`.
- Update `Packages/com.contentcontent.core/package.json` description/keywords.
- Update `docs/core/how-it-works.md`, `docs/core/tutorial.md`, `docs/core/scripting-api.md`, `docs/README.md`, and root `README.md`.
- Run `powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1` as the final gate.

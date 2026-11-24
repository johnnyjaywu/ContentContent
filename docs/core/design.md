# Core Design & Best Practices

Game engines provide rendering, physics, and built-in object pooling (`UnityEngine.Pool`), but they rarely provide clean, deterministic building blocks for gameplay state, decoupled messaging, cooldowns, reproducible randomness, or undoable actions. `com.contentcontent.core` collects seven small, engine-free building blocks that every other ContentContent package shares.

---

## Why These Seven Modules (And Not Others)

- **Included because engines leave them out or lock them to scenes:**
  1. **HFSM (`StateMachine`):** Lets you nest sub-state machines (`StateMachine` implements `IState`) and add high-priority `Any` transitions without giant spaghetti `switch` statements.
  2. **Blackboard:** Stores typed shared AI/character data without boxing structs into `object` or allocating dictionary strings every frame, and supports parent fallback scopes (Squad → Unit).
  3. **EventBus:** Instance-based (safe across Unity Domain Reloads and tests) instead of fragile static global `event` singletons that leak across scene loads.
  4. **Timers & Cooldown:** Value-type `Cooldown` structs and generational `TimerHandle`s avoid Unity coroutine GC allocations and stale-handle bugs.
  5. **2D / 3D Grid:** Contiguous flat-array storage (`Grid2D<T>`, `Grid3D<T>`) keeps memory cache-friendly and stays strictly separate from pathfinding (`com.contentcontent.navigation`).
  6. **Deterministic RNG (`Xoshiro128StarStar` & `WeightedTable`):** Unlike `UnityEngine.Random` (a single global static), each system owns an independent, saveable `RngState` stream so replays, procedural seeds, and unit tests behave identically every run.
  7. **Commands (`CommandHistory` & `CommandQueue`):** Turns actions into reversible objects supporting atomic multi-step transactions, drag merging, and turn-based queues.
- **Intentionally omitted:** General object pooling (`UnityEngine.Pool` already covers this natively) and heavy reflection-based DI containers.

---

## Best Practices Built Into Core

1. **Prefer Instance State Over Static Singletons:** `EventBus`, `TimerScheduler`, `Blackboard`, and `Xoshiro128StarStar` are all plain instances. Attach them to scene scopes (`EventBusBehaviour`, `BlackboardBehaviour`) instead of static globals so Domain Reload disablement and automated testing stay reliable.
2. **Dispose Your Subscriptions:** `EventBus.Subscribe` and `Blackboard.Subscribe` return `IDisposable` handles (`EventSubscription`, `BlackboardSubscription`) so listeners clean up deterministically in `OnDestroy` or `using` scopes.
3. **Expect Safe Iteration & Stale Guards:** Both `EventBus` and `TimerScheduler` handle subscribing/canceling from inside callbacks without throwing "collection modified" errors, and `TimerHandle` uses a generation ID so reusing a pooled slot never cancels someone else's timer.

---

## Engine Transferability (Godot & Unreal)

Because Layer 1 (`ContentContent.Core`) compiles with zero engine references (`netstandard2.1`), you can drop the exact same C# classes into **Godot C#** projects (`Node` wrappers replacing the `MonoBehaviour` runners). In **Unreal Engine**, each module maps cleanly to familiar architectural boundaries: `StateMachine` / `Blackboard` mirror Unreal's StateTree & AI Blackboard, `EventBus` mirrors `UGameplayMessageSubsystem`, and `Xoshiro128StarStar` mirrors isolated `FRandomStream` instances.

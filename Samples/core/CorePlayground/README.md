# Core Playground Sample

Interactive single-script demo showcasing the core modules in `com.contentcontent.core`:

- **ObjectPool (`ObjectPool<T>`, `IPoolable`)** — Prewarm, lease, and release pooled tokens.
- **HFSM + Blackboard (`StateMachine`, `Blackboard`, `BlackboardFsmExtensions`)** — Toggle a typed `BlackboardKey<bool>` to drive state transitions automatically.
- **EventBus (`EventBus`)** — Zero-boxing `in T` struct event dispatch.
- **CommandHistory (`CommandHistory`, `ActionCommand`)** — Execute, undo, and redo score mutations.
- **Timers & Cooldown (`TimerScheduler`, `Cooldown`)** — Fire cooldown-gated abilities and delayed callbacks.
- **Deterministic RNG & Grid2D (`Xoshiro128StarStar`, `WeightedTable<T>`, `Grid2D<T>`)** — Roll deterministic weighted loot and update grid cells.

## Setup

1. Create an empty `GameObject` in any scene.
2. Attach `CorePlaygroundSample`.
3. Press **Play** and interact via the on-screen HUD.

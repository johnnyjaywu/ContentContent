# World & Progression Design & Best Practices

Interconnected games — adventure worlds, dungeon crawlers, action-RPG chapters, and platformers alike — suffer from three classic bugs: **soft-locks** (dropping into a ledge without a return exit), **mixed checkpoint semantics** (touching spikes resetting an entire dungeon puzzle, or dying leaving enemies half-dead), and **fragmented progression bools**. `com.contentcontent.world` models all three cleanly without engine coupling.

---

## 1. Two-Tier Checkpoints: Hard Checkpoints vs. Safe Ground

```text
   Player walks on ledge ──► RecordSafeGround(room, pos)
                                  │
       ┌──────────────────────────┴──────────────────────────┐
       ▼                                                     ▼
   Touches Hazard (Non-Fatal)                        Fatal Death / Full Reset
   RespawnFromHazard()                               RespawnFromDeath()
   ──► Returns to SafeGround                         ──► Returns to ActiveCheckpoint
   ──► Keeps room entities intact!                   ──► Resets room IResettable state!
```

### Why This Separation Matters
- **No punishing spike/pit resets:** Falling onto spikes (`HazardZone2D`) puts the player back on stable ground without resetting solved switches unless no safe ground exists yet.
- **Clean death / room reloads:** Respawning at a shrine/checkpoint or walking through a doorway (`RoomTransitionTrigger2D`) calls `ResetRoom(roomId)`, resetting every registered `IResettable` (`ResettableTransform`, custom enemy spawners) deterministically.

---

## 2. Progression-Aware Static Reachability Validation

A naive BFS check on rooms fails as soon as exits require keys, abilities, or flags earned in other rooms. `WorldReachabilityValidator` simulates monotone progression:
1. Starts at `startRoomId` with any initial abilities/flags.
2. Expands only unlocked exits, collecting `GrantedAbilities` and `GrantedFlags` from newly entered rooms, and loops until no new rooms or abilities unlock.
3. Re-runs reachability *back* toward `startRoomId` or any `hasCheckpoint` room from every reachable room to flag true one-way **soft-locks** during authoring.

---

## 3. Best Practices & Common Pitfalls

- **Always initialize `WorldGraphAsset` during EditMode tests/design:** Put one `WorldGraphAsset` at the root of each world/chapter and fix any **Soft-Lock** or **Unreachable** warning before wiring scene triggers.
- **Snapshot `WorldFlagStore` for save files:** Use `ExportFlags()` / `ExportVisitedRooms()` and `ImportFlags(...)` / `ImportVisitedRooms(...)` — all world gates, opened shortcuts, and discovered map rooms serialize cleanly in two calls.
- **Prefer `ResettableTransform` or `ResettableBehaviour` over scene reload hacks:** Resetting `IResettable` actors takes microseconds instead of tearing down and reloading additive Unity scenes.

---

## 4. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.World`) drops straight into Godot C#; trigger `RoomLifecycleController.TransitionTo(...)` from `Area2D` / `Area3D` portals and implement `IResettable` on enemy scenes.
- **Unreal:** Mirrors a persistent `UGameInstanceSubsystem` world-state & checkpoint manager paired with Gameplay Tag flags, `DataAsset` connectivity graphs, and an Editor utility validator.

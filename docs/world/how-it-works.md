# How World Works

`com.contentcontent.world` provides the Metroidvania world topology, progression validation, bench checkpoints, safe-ground hazard recovery, deterministic room entity resets, and persistent world flags — cleanly separated into a pure C# core (`noEngineReferences: true`) and thin Unity 2D adapters.

## Package Architecture

```text
Packages/com.contentcontent.world/
├── package.json                                  # Depends on com.contentcontent.core
├── Runtime/
│   ├── ContentContent.World.asmdef               # Pure C# (noEngineReferences: true)
│   ├── WorldEvents.cs                            # RespawnKind, CheckpointData & EventBus structs
│   ├── WorldFlagStore.cs                         # Typed persistent flags & visited-room map
│   ├── WorldGraph.cs                             # Directed RoomNode & gated RoomExit graph
│   ├── WorldReachabilityValidator.cs             # Static BFS reachability & soft-lock detector
│   ├── CheckpointService.cs                      # Hard checkpoints vs. safe-ground hazard anchors
│   ├── IResettable.cs                            # Deterministic room entity reset contract
│   ├── RoomLifecycleController.cs                # Room transitions, exit validation & respawn coordinator
│   └── Unity/
│       ├── ContentContent.World.Unity.asmdef
│       ├── WorldManagerBehaviour.cs              # MonoBehaviour coordinator + spawn point registry
│       ├── RoomSpawnPoint.cs                     # Named spawn anchor with Gizmo
│       ├── ResettableBehaviour.cs                # Abstract MonoBehaviour auto-registered per room
│       ├── RoomTransitionTrigger2D.cs            # 2D room portal trigger
│       ├── CheckpointBehaviour2D.cs              # Bench/checkpoint trigger
│       └── HazardZone2D.cs                       # Spike/pit hazard trigger
└── Tests/EditMode/
    ├── ContentContent.World.Tests.asmdef
    └── WorldSystemTests.cs
```

---

## Core Concepts

### 1. Room Graph & Static Soft-Lock Validation (`WorldGraph`, `WorldReachabilityValidator`)

- Model rooms (`RoomNode`) and directed connections (`RoomExit`) gated by required abilities (`WallJump`, `DoubleJump`) or world flags (`LeverPulled`).
- **`WorldReachabilityValidator`** searches `(roomId, heldAbilities, heldFlags)` states so unit tests automatically verify:
  - **Progression order (`CanReach`)**: catches keys locked behind their own doors.
  - **Dead zones (`FindUnreachableRooms`)**: catches disconnected rooms.
  - **One-way drop traps (`FindSoftLockRooms`)**: catches reachable rooms from which the player can never return to start or any `HasCheckpoint` room.

### 2. Safe-Ground Hazard Recovery vs. Hard Bench Respawns (`CheckpointService`, `RoomLifecycleController`)

Hollow Knight separates non-lethal hazard hits (falling onto spikes) from full death respawns:
- **`RespawnFromHazard()`**: Returns the player to `LastSafeGroundPosition` inside the current room with `ResetRoomEntities = false` (defeated enemies stay defeated and gates stay open), falling back to the active checkpoint only if no safe ground was recorded.
- **`RespawnFromDeath()`**: Returns the player to `ActiveCheckpoint` (the last bench) and invokes `ResetState()` on every `IResettable` in the target room (`ResetRoomEntities = true`).

### 3. Persistent World Flags & Discovery (`WorldFlagStore`)

`WorldFlagStore` maintains typed `Bool` / `Int` / `String` flags plus a visited-room set (`MarkRoomVisited` returning `isFirstVisit`) with snapshot `Export*`/`Import*` methods ready for save serialization.

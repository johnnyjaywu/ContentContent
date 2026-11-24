# World Scripting API

Complete reference for `com.contentcontent.world`:
- **Pure C# Runtime**: `ContentContent.World`
- **Unity Adapters**: `ContentContent.World.Unity`

Depends on `com.contentcontent.core`.

---

## Namespace: `ContentContent.World`

### `RespawnKind` (enum) & `CheckpointData` (`readonly struct`)

- **`RespawnKind`**: `HazardSafeGround = 0`, `HardCheckpoint = 1`.
- **`CheckpointData`**: `CheckpointId`, `RoomId`, `SpawnPointId`, `Position` (`System.Numerics.Vector3`).

### `WorldFlagType`, `WorldFlagValue` & `WorldFlagStore`

- **`WorldFlagValue`**: immutable union (`None`, `Bool`, `Int`, `String`) with `FromBool`, `FromInt`, `FromString`, `AsBool`, `AsInt`, `AsString`.
- **`WorldFlagStore`**:
  - `FlagCount`, `VisitedRoomCount`, `OnFlagChanged` (`event Action<WorldFlagChangedEvent>`)
  - `SetBool`/`GetBool`, `SetInt`/`GetInt`, `SetString`/`GetString`, `HasFlag`, `TryGetValue`, `RemoveFlag`
  - `MarkRoomVisited(string roomId)`, `HasVisitedRoom(string roomId)`, `GetVisitedRooms()`
  - `ExportFlags()`, `ImportFlags(...)`, `ExportVisitedRooms()`, `ImportVisitedRooms(...)`, `Clear()`

### `RoomExit`, `RoomNode` & `WorldGraph`

- **`RoomExit`**: `ExitId`, `DestinationRoomId`, `DestinationSpawnId`, `RequiredAbilities`, `RequiredFlags`, `IsUnlocked(Func<string, bool> hasAbility, Func<string, bool> hasFlag)`.
- **`RoomNode`**: `RoomId`, `DisplayName`, `AreaId`, `HasCheckpoint`, `Exits`, `GrantedAbilities`, `GrantedFlags`, `AddExit(RoomExit)`, `GrantAbility(string)`, `GrantFlag(string)`.
- **`WorldGraph`**: `Rooms`, `AddRoom(...)`, `AddExit(...)`, `AddBidirectionalConnection(...)`, `TryGetRoom(...)`, `TryFindExit(...)`, `GetAvailableExits(...)`.

### `WorldReachabilityValidator` (`static class`)

| Method | Return | Description |
| --- | --- | --- |
| `CanReach(graph, startRoomId, goalRoomId, initialAbilities = null, initialFlags = null)` | `bool` | Checks progression-ordered reachability from start to goal. |
| `FindUnreachableRooms(graph, startRoomId, initialAbilities = null, initialFlags = null)` | `List<string>` | Lists rooms unreachable from `startRoomId`. |
| `FindSoftLockRooms(graph, startRoomId, initialAbilities = null, initialFlags = null)` | `List<string>` | Lists reachable rooms unable to return to `startRoomId` or any `HasCheckpoint` room. |

### `CheckpointService` (`sealed class`)

- Properties: `ActiveCheckpoint` (`CheckpointData?`), `HasActiveCheckpoint`, `LastSafeGroundRoomId`, `LastSafeGroundPosition`, `HasSafeGround`.
- Events: `OnCheckpointActivated`, `OnSafeGroundUpdated`, `OnRespawnRequested`.
- Methods: `ActivateCheckpoint(CheckpointData)`, `RecordSafeGround(string roomId, Vector3 position)`, `ClearSafeGround()`, `RequestHazardRespawn(string currentRoomId)`, `RequestDeathRespawn()`, `Reset()`.

### `IResettable` & `RoomLifecycleController`

- **`IResettable`**: `void ResetState()`.
- **`RoomLifecycleController`**:
  - Properties: `Graph`, `Flags`, `Checkpoints`, `CurrentRoomId`, `CurrentSpawnPointId`.
  - Events: `OnRoomExited`, `OnRoomEntered`.
  - Methods: `RegisterResettable(roomId, target)`, `UnregisterResettable(roomId, target)`, `ResetRoom(roomId)`, `TransitionTo(roomId, spawnPointId = null, resetTargetRoom = true)`, `TryTraverseExit(exitId, hasAbility = null, resetTargetRoom = true)`, `RespawnFromHazard()`, `RespawnFromDeath()`.

**Struct Events (`WorldEvents.cs`)**: `RoomEnteredEvent`, `RoomExitedEvent`, `CheckpointActivatedEvent`, `SafeGroundUpdatedEvent`, `RespawnRequestedEvent`, `WorldFlagChangedEvent`.

---

## Namespace: `ContentContent.World.Unity`

- **`WorldGraphAsset`** (`ScriptableObject`): Author interconnected rooms, ability/flag gates, and exits in Inspector; `BuildGraph()` compiles into `WorldGraph`, and `ValidateReachability(initialAbilities, initialFlags)` returns `WorldValidationReport` (`HasStartRoom`, `DuplicateRoomIds`, `UnreachableRooms`, `SoftLockRooms`, `IsValid`).
- **`WorldManagerBehaviour`** (`MonoBehaviour`): Owns `RoomLifecycleController`, loads optional `WorldGraphAsset`, tracks `RoomSpawnPoint`s (`RegisterSpawnPoint`, `UnregisterSpawnPoint`, `TryGetSpawnPoint`), exposes `TransitionToRoom`, `ActivateCheckpoint`, `RecordSafeGround`, `TriggerHazardRespawn`, `TriggerDeathRespawn`, `ResetCurrentRoom`, `OnRespawnRequested` (`UnityEvent<RespawnRequestedEvent>`), `OnRoomEntered` (`UnityEvent<string>`), and `OnCheckpointActivated` (`UnityEvent<string>`).
- **`RoomSpawnPoint`** (`MonoBehaviour`): Named spawn anchor auto-registering with `WorldManagerBehaviour`.
- **`ResettableBehaviour`** (`abstract MonoBehaviour`, `IResettable`): Auto-registers with `WorldManagerBehaviour` for room resets.
- **`ResettableTransform`** (`ResettableBehaviour`): Code-free concrete resetter snapshotting initial local position/rotation/scale/active state (plus optional `Rigidbody2D` velocity reset) and restoring them cleanly on room reload or checkpoint respawn.
- **`RoomTransitionTrigger2D`** (`MonoBehaviour`, `#if CONTENTCONTENT_PHYSICS2D`): 2D room transition trigger.
- **`CheckpointBehaviour2D`** (`MonoBehaviour`, `#if CONTENTCONTENT_PHYSICS2D`): Checkpoint trigger defaulting `activateOnTouch` to `true`, with `Activate()` and `OnActivated` (`UnityEvent`).
- **`HazardZone2D`** (`MonoBehaviour`, `#if CONTENTCONTENT_PHYSICS2D`): Hazard trigger invoking damage callbacks and `TriggerHazardRespawn()`.

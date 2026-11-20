# Camera Scripting API

Complete reference for `com.contentcontent.camera`:
- **Pure C# Runtime**: `ContentContent.Camera`
- **Unity Adapters**: `ContentContent.Camera.Unity`
- **Cinemachine 3 Bridge**: `ContentContent.Camera.Unity.Cinemachine` (enabled when `com.unity.cinemachine` `3.0+` is installed)

Depends on `com.contentcontent.core`.

---

## Namespace: `ContentContent.Camera`

### `CameraFramingConfig` (`[Serializable] sealed class`)

| Field | Default | Description |
| --- | --- | --- |
| `DefaultOrthographicSize` | `6f` | Default camera orthographic half-height. |
| `LookAheadDistance` | `2f` | Horizontal lead offset ahead of facing direction. |
| `LookAheadSmoothSpeed` | `6f` | Exponential smoothing speed for horizontal look-ahead. |
| `LookAheadTurnDelay` | `0.08f` | Delay before flipping lead direction after turning. |
| `VerticalPeekUpDistance` | `3f` | Vertical offset when holding up while grounded and stationary. |
| `VerticalPeekDownDistance` | `3.5f` | Downward peek distance when holding down. |
| `VerticalPeekHoldDelay` | `0.45f` | Hold duration before vertical peek activates. |
| `VerticalPeekSmoothSpeed` | `8f` | Exponential smoothing speed for vertical peek. |
| `ZoomSmoothSpeed` | `5f` | Exponential smoothing speed for orthographic size changes. |

Methods: `Clone()`, `static CreateHollowKnightPreset()`.

### `CameraZoneConfig` (`[Serializable] sealed class`)

| Field | Default | Description |
| --- | --- | --- |
| `ZoneId` | `""` | Unique zone identifier. |
| `Priority` | `0` | Priority on `CameraZoneStack` (higher wins). |
| `LockPosition` | `false` | Locks `FollowAnchorPosition` to `LockedPosition + StaticOffset`. |
| `LockedPosition` | `Vector3.Zero` | World-space position used when locked. |
| `OrthographicSizeOverride` | `0f` | Overrides default orthographic size when `&gt; 0`. |
| `StaticOffset` | `Vector3.Zero` | Static offset added while active. |
| `AllowLookAhead` | `true` | Enables horizontal look-ahead inside the zone. |
| `AllowVerticalPeek` | `true` | Enables vertical peek inside the zone. |
| `BoundsId` | `""` | Bounds identifier mapped to `CinemachineConfiner2D` in Unity. |

### `CameraFrameInput` & `CameraFrameOutput` (`struct`)

- **`CameraFrameInput`**: `TargetPosition`, `FacingDirection`, `VerticalLookInput`, `IsTargetGrounded`, `IsTargetStationary`.
- **`CameraFrameOutput`**: `FollowAnchorPosition`, `FramingOffset`, `OrthographicSize`, `ActiveZoneId`, `ActiveBoundsId`, `IsPositionLocked`, `SnapRequested`.

### `CameraZoneStack` (`sealed class`)

- `ActiveZone` (`CameraZoneConfig`), `Count` (`int`), `OnActiveZoneChanged` (`event Action<CameraZoneChangedEvent>`).
- `EnterZone(CameraZoneConfig)`, `ExitZone(string zoneId)`, `Contains(string zoneId)`, `Clear()`.

### `CameraDirector` (`sealed class`)

```csharp
public CameraDirector(CameraFramingConfig config = null, IEventBus eventBus = null)
```

| Member | Return | Description |
| --- | --- | --- |
| `Config`, `Zones`, `ActiveZone`, `CurrentOutput` | — | Framing config, zone stack, active zone, and last frame output. |
| `CurrentLookAheadOffset`, `CurrentVerticalPeekOffset`, `CurrentOrthographicSize` | `float` | Current smoothed framing values. |
| `OnSnapRequested`, `OnImpulseRequested` | `event` | Direct callbacks (also published on `IEventBus`). |
| `Tick(float deltaTime, in CameraFrameInput input)` | `CameraFrameOutput` | Advances smoothing and computes framing output. |
| `RequestSnap(Vector3 worldPosition, string reason = null)` | `void` | Forces an instant cut on the next `Tick`. |
| `RequestImpulse(Vector3 velocity, float strength = 1f, string sourceId = null)` | `void` | Emits an impulse cue for Cinemachine Impulse Source. |
| `Reset()` | `void` | Clears zones and resets offsets/zoom. |

**Struct Events (`CameraEvents.cs`)**: `CameraZoneChangedEvent`, `CameraSnapRequestedEvent`, `CameraImpulseRequestedEvent`.

---

## Namespace: `ContentContent.Camera.Unity`

- **`CameraFramingConfigAsset`** (`ScriptableObject`): Reusable framing config preset with `CreateRuntimeCopy()`.
- **`CameraDirectorBehaviour`** (`MonoBehaviour`): Owns `CameraDirector`, updates `FollowAnchor` in `LateUpdate`, exposes `SetTargetState`, `EnterZone`, `ExitZone`, `RequestSnap`, `RequestImpulse`, and bounds registry (`RegisterBounds`, `UnregisterBounds`, `TryGetBounds<T>`).
- **`CameraZoneTrigger2D`** (`MonoBehaviour`, `#if CONTENTCONTENT_PHYSICS2D`): `Collider2D` trigger pushing/popping `CameraZoneConfig` and registering confiner bounds.

## Namespace: `ContentContent.Camera.Unity.Cinemachine`

- **`CinemachineCameraBridge`** (`MonoBehaviour`, `#if CONTENTCONTENT_CINEMACHINE`): Syncs `CameraDirectorBehaviour` outputs to `CinemachineCamera` (`Lens.OrthographicSize`, `ForceCameraPosition`), `CinemachinePositionComposer.TargetOffset`, `CinemachineConfiner2D.BoundingShape2D`, and `CinemachineImpulseSource.GenerateImpulseWithVelocity`.

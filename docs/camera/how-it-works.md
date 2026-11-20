# How Camera Works

`com.contentcontent.camera` provides a pure-C# (`noEngineReferences: true`) camera framing director for 2D/Metroidvania platformers — handling frame-rate-independent look-ahead, grounded vertical peek, priority camera zones, zoom overrides, instant snap cuts, and impulse cues — paired directly with **Cinemachine 3** (`Unity.Cinemachine`) in Unity.

## Package Architecture

```text
Packages/com.contentcontent.camera/
├── package.json                                  # Depends on com.contentcontent.core
├── Runtime/
│   ├── ContentContent.Camera.asmdef              # Pure C# (noEngineReferences: true)
│   ├── CameraZoneConfig.cs                       # Priority zone override config
│   ├── CameraFramingConfig.cs                    # Look-ahead, vertical peek, zoom & Hollow Knight preset
│   ├── CameraFrameInput.cs                       # CameraFrameInput & CameraFrameOutput structs
│   ├── CameraEvents.cs                           # Core EventBus struct events
│   ├── CameraZoneStack.cs                        # Priority + recency zone stack
│   ├── CameraDirector.cs                         # Exponentially smoothed pure-C# framing director
│   └── Unity/
│       ├── ContentContent.Camera.Unity.asmdef
│       ├── CameraFramingConfigAsset.cs           # ScriptableObject preset asset
│       ├── CameraDirectorBehaviour.cs            # MonoBehaviour adapter + FollowAnchor + bounds registry
│       ├── CameraZoneTrigger2D.cs                # Collider2D trigger zone + confiner registration
│       └── Cinemachine/
│           ├── ContentContent.Camera.Cinemachine.asmdef  # Enabled when com.unity.cinemachine 3.0+ is present
│           └── CinemachineCameraBridge.cs        # Bridges Director -> CinemachineCamera / PositionComposer / Confiner2D / ImpulseSource
└── Tests/EditMode/
    ├── ContentContent.Camera.Tests.asmdef
    └── CameraDirectorTests.cs
```

---

## Core Concepts

### 1. Exponential Framing & Delayed Look-Ahead (`CameraDirector`)

`CameraDirector` advances all smoothing using $1 - e^{-\text{rate} \cdot \Delta t}$ so look-ahead, peek, and zoom feel identical at 30 FPS, 60 FPS, and 144+ FPS:
- **Turn-Delayed Look-Ahead**: Reversing direction waits `LookAheadTurnDelay` seconds before flipping the lead offset, preventing camera whip during micro-adjustments.
- **Grounded Vertical Peek**: Holding up/down while grounded and stationary waits `VerticalPeekHoldDelay` seconds before shifting vertically by `VerticalPeekUpDistance` / `VerticalPeekDownDistance`, snapping smoothly back to `0` as soon as the character moves or leaves the ground.
- **Instant Snap Cuts**: Calling `RequestSnap(...)` bypasses exponential smoothing for one tick and emits `SnapRequested = true`, allowing `CinemachineCameraBridge` to call `CinemachineCamera.ForceCameraPosition(...)` after room transitions or respawns.

### 2. Priority Zone Stack (`CameraZoneStack`)

Overlapping room zones, tight corridors, and locked boss arenas push/pop `CameraZoneConfig` entries on `CameraZoneStack`:
- Higher `Priority` wins; equal priorities resolve to the most recently entered zone.
- Zones can lock the camera to a fixed `LockedPosition`, override `OrthographicSizeOverride`, add a `StaticOffset`, disable look-ahead/peek, and specify a `BoundsId` for `CinemachineConfiner2D`.

### 3. Cinemachine 3 Integration (`CinemachineCameraBridge`)

When `com.unity.cinemachine` (`3.0.0+`) is installed, `ContentContent.Camera.Cinemachine` activates automatically via `versionDefines` (`CONTENTCONTENT_CINEMACHINE`):
- Drives `CinemachineCamera.Lens.OrthographicSize`.
- Applies `FramingOffset` to `CinemachinePositionComposer.TargetOffset` (or follows `FollowAnchor` directly during locked boss arenas).
- Swaps `CinemachineConfiner2D.BoundingShape2D` and calls `InvalidateBoundingShapeCache()` whenever `ActiveBoundsId` changes.
- Forwards `RequestImpulse(...)` into `CinemachineImpulseSource.GenerateImpulseWithVelocity(...)`.

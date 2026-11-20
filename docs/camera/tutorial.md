# Camera Tutorial

This guide covers installing `com.contentcontent.camera`, wiring `CameraDirectorBehaviour` with Cinemachine 3 and Unity's New Input System (`com.unity.inputsystem`), configuring overlapping room/boss zones, and trying the built-in sample.

---

## Step 1: Install the Package

`com.contentcontent.camera` depends on `com.contentcontent.core` (and integrates directly with `com.unity.cinemachine` `3.0+`). Install via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.camera
```

### Try the Built-In Sample

1. Ensure `com.contentcontent.core` is installed, open **Window → Package Manager**, select **ContentContent - Camera**, and click **Download** on **Camera Playground** under **Downloadable Samples**.
2. Click **Open Scene** (or open `Assets/Samples/ContentContent/Camera/CameraPlayground/CameraPlayground.unity`) and press **Play**.

---

## 1. Wiring Cinemachine 3 + `CameraDirectorBehaviour`

1. Install **Cinemachine (`com.unity.cinemachine` 3.0+)** via Package Manager (`ContentContent.Camera.Cinemachine` enables automatically via `versionDefines`).
2. Create a `CinemachineCamera` with a **`CinemachinePositionComposer`** (Body), optional **`CinemachineConfiner2D`**, and optional **`CinemachineImpulseSource`**.
3. Attach **`CameraDirectorBehaviour`** and **`CinemachineCameraBridge`** to your camera rig GameObject, and assign your Player `Transform` to `Follow Target` (plus an empty `Follow Anchor` `Transform` for locked boss arenas).
4. Feed player state from your New Input System + character script:

```csharp
using ContentContent.Camera.Unity;
using ContentContent.Character.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTargetFeed2D : MonoBehaviour
{
    [SerializeField] private CameraDirectorBehaviour _cameraDirector;
    [SerializeField] private CharacterMotor2D _character;
    [SerializeField] private InputActionReference _lookAction;

    private void OnEnable() => _lookAction?.action.Enable();
    private void OnDisable() => _lookAction?.action.Disable();

    private void Update()
    {
        if (_cameraDirector == null || _character == null || _character.Motor == null)
            return;

        float verticalLook = _lookAction != null ? _lookAction.action.ReadValue<Vector2>().y : 0f;
        var facing = _character.Motor.FacingDirection;
        bool grounded = _character.Motor.IsGrounded;
        bool stationary = _character.Motor.PlanarSpeed < 0.05f;

        _cameraDirector.SetTargetState(
            new Vector3(facing.X, facing.Y, facing.Z),
            verticalLook,
            grounded,
            stationary);
    }
}
```

---

## 2. Setting Up Room Bounds & Boss Arena Zones

1. Add a trigger `Collider2D` to a room and attach **`CameraZoneTrigger2D`**.
2. Assign a `PolygonCollider2D` (or `CompositeCollider2D`) representing the room limits to **Confiner Bounds** and set **Zone Id** / **Priority** (e.g., `Priority = 1` for normal rooms, `Priority = 10` for a Boss Arena).
3. On a Boss Arena trigger, check **Lock Position**, assign a **Locked Anchor**, set **Orthographic Size Override** (e.g., `8.5`), and uncheck **Allow Look Ahead** / **Allow Vertical Peek**.
4. Whenever the player respawns or transitions rooms, call `_cameraDirector.RequestSnap(playerPos, "RoomTransition");` so `CinemachineCameraBridge` immediately calls `ForceCameraPosition`.

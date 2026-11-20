# Character Tutorial

This guide walks through installing `com.contentcontent.character`, trying the built-in sample, setting up a 2D or 3D character in Unity, tuning Hollow Knight–style traversal (wall slide/jump, pogo bounce, moving & one-way platforms), adding custom cooldown-gated actions, and subscribing to character events.

---

## Step 1: Install the Package

`com.contentcontent.character` depends on `com.contentcontent.core`. Install both via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
```

If you are working inside the `ContentContent` repository directly, both packages are already present under `Packages/`.

### Try the Built-In Sample

1. Ensure `com.contentcontent.core` is installed, open **Window → Package Manager**, select **ContentContent - Character**, and click **Download** on **Character Playground** under **Downloadable Samples**.
2. Attach `CharacterPlaygroundSample` to an empty `GameObject` and press **Play** to switch live between `SideScroller2D`, `TopDown2D`, and `ThirdPerson3D` modes and test jump, dash, knockback, and custom lock-flagged actions.

---

## 1. Quick Setup in Unity (2D Side-Scroller or Top-Down)

1. Create a GameObject with a `Rigidbody2D` (set **Freeze Rotation Z**) and a `Collider2D`.
2. Add the **`CharacterMotor2D`** component (`ContentContent.Character.Unity`).
3. Create a config preset via **Right-click → Create → ContentContent → Character → Character Config** and assign it to `_configAsset` (or tune `_inlineConfig` directly):
   - Set **Mode** to `SideScroller2D` or `TopDown2D`.
   - For `SideScroller2D`, configure **Ground Mask**, **One Way Platform Mask**, **Wall Mask**, and the ground (green), ceiling (yellow), and wall (cyan) sensor offsets/sizes visible as Gizmos when selected.
4. Feed input from Unity's New Input System (`com.unity.inputsystem`):

```csharp
using ContentContent.Character.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputBridge2D : MonoBehaviour
{
    [SerializeField] private CharacterMotor2D _motor;
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;
    [SerializeField] private InputActionReference _grabAction;
    [SerializeField] private InputActionReference _attackAction;

    private void OnEnable()
    {
        _moveAction?.action.Enable();
        _jumpAction?.action.Enable();
        _dashAction?.action.Enable();
        _grabAction?.action.Enable();
        _attackAction?.action.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.action.Disable();
        _jumpAction?.action.Disable();
        _dashAction?.action.Disable();
        _grabAction?.action.Disable();
        _attackAction?.action.Disable();
    }

    private void Update()
    {
        Vector2 move = _moveAction != null ? _moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        _motor.SetMoveInput(move);

        if (_jumpAction != null)
        {
            if (_jumpAction.action.WasPressedThisFrame())
            {
                if (move.y < -0.5f)
                    _motor.PressDropThrough();
                else
                    _motor.PressJump();
            }

            if (_jumpAction.action.WasReleasedThisFrame())
                _motor.ReleaseJump();
        }

        if (_dashAction != null && _dashAction.action.WasPressedThisFrame())
            _motor.PressDash();

        _motor.SetGrabHeld(_grabAction != null && _grabAction.action.IsPressed());

        if (_attackAction != null && _attackAction.action.WasPressedThisFrame())
            _motor.TriggerAction("Attack", bufferWindow: 0.15f);
    }
}
```

### Hollow Knight / Celeste Traversal Presets & Pogo Bounce

Use `CharacterConfig.CreateHollowKnightPreset()` or `CharacterConfig.CreateCelestePreset()` in code or tests, and call `ApplyBounce(...)` on a successful down-slash pogo hit:

```csharp
// Configure a Hollow Knight–style motor in pure C#
var hkConfig = CharacterConfig.CreateHollowKnightPreset();
var controller = new ContentContent.Character.CharacterController(hkConfig);

// On down-slash hit against an enemy or spike in Unity:
_motor.ApplyBounce(upwardSpeed: 13f, refillAirJumps: true, refillAirDashes: true);
```

To make a moving platform carry `CharacterMotor2D` cleanly and transfer momentum on jump, implement `ISurfaceVelocityProvider` (or move via a `Rigidbody2D` velocity):

```csharp
using ContentContent.Character.Unity;
using UnityEngine;

public class MovingPlatform2D : MonoBehaviour, ISurfaceVelocityProvider
{
    public Vector2 SurfaceVelocity { get; private set; }
}
```

---

## 2. Quick Setup in Unity (3D Third-Person)

1. Add Unity's **`CharacterController`** and **`CharacterMotor3D`** to your character GameObject.
2. Assign a `CharacterConfigAsset` with **Mode = `ThirdPerson3D`**.
3. Pass camera-relative XZ movement to `_motor3D.SetMoveInput(worldMove)` and call `PressJump()`, `ReleaseJump()`, `PressDash()`, and `TriggerAction(...)` from your input callbacks.

---

## 3. Adding Custom Actions with Cooldowns & Movement Locks

Extend `CharacterActionBase` (or use `DelegateCharacterAction`) to add attacks, rolls, or hitstun:

```csharp
using System.Numerics;
using ContentContent.Character;

public sealed class MeleeSlashAction : CharacterActionBase
{
    public MeleeSlashAction()
        : base(
            id: "Attack",
            duration: 0.25f,
            cooldown: 0.45f,
            priority: 1,
            lockFlags: ActionLockFlags.BlockMovement | ActionLockFlags.BlockTurn | ActionLockFlags.BlockJump)
    {
    }

    protected override void HandleStart(CharacterContext context)
    {
        // Lunge slightly forward in facing direction
        context.Motor.ApplyImpulse(context.Motor.FacingDirection * 6f);
    }
}
```

Register the action on `Awake()` / `Start()`:

```csharp
_motor.Controller.RegisterAction(new MeleeSlashAction());
```

High-priority actions (such as `"Hitstun"` with `priority: 10` and `ActionLockFlags.All`) automatically interrupt lower-priority actions and invoke `HandleEnd(context, interrupted: true)`.

---

## 4. Subscribing via C# Events, Blackboard, or Core EventBus

```csharp
using ContentContent.Character;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Events;

// 1. Direct C# events
controller.Motor.OnJumped += e => PlayJumpSfx(e.IsAirJump, e.IsWallJump);
controller.Motor.OnWallJumped += e => SpawnWallKickDust(e.WallNormal);
controller.Motor.OnLanded += e => SpawnLandingDust(e.ImpactSpeed);
controller.Actions.OnActionStarted += e => animator.SetTrigger(e.ActionId);

// 2. Core Blackboard (auto-synced every tick)
float speed = controller.Blackboard.Get(CharacterBlackboardKeys.PlanarSpeed);
bool grounded = controller.Blackboard.Get(CharacterBlackboardKeys.IsGrounded);
bool wallSliding = controller.Blackboard.Get(CharacterBlackboardKeys.IsWallSliding);

// 3. Optional Core EventBus (attach EventBusBehaviour on CharacterMotor2D/3D)
eventBus.Subscribe<CharacterDashedEvent>((in CharacterDashedEvent e) =>
{
    SpawnDashTrail(e.Direction, e.IsAirDash);
});
```

---

## 5. Pure C# Unit Testing (No Unity Required)

Because `ContentContent.Character` has `"noEngineReferences": true`, you can simulate entire jump/wall-jump/dash/combat sequences deterministically in NUnit:

```csharp
using System.Numerics;
using ContentContent.Character;

var controller = new CharacterController(CharacterConfig.CreateHollowKnightPreset());

var moveRight = new CharacterInput { Move = Vector3.UnitX };
controller.Tick(0.1f, in moveRight, MotorSensors.Grounded);
```

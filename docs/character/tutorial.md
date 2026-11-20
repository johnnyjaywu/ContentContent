# Character Tutorial

This guide walks through installing `com.contentcontent.character`, trying the built-in sample, setting up a 2D or 3D character in Unity, wiring input, adding custom cooldown-gated actions, and subscribing to character events.

---

## Step 1: Install the Package

`com.contentcontent.character` depends on `com.contentcontent.core`. Install both via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.character
```

If you are working inside the `GameSystems` repository directly, both packages are already present under `Packages/`.

### Try the Built-In Sample

1. Open **Window → Package Manager**, select **ContentContent - Character**, expand **Samples**, and click **Import** on **Character Playground**.
2. Attach `CharacterPlaygroundSample` to an empty `GameObject` and press **Play** to switch live between `SideScroller2D`, `TopDown2D`, and `ThirdPerson3D` modes and test jump, dash, knockback, and custom lock-flagged actions.

---

## 1. Quick Setup in Unity (2D Side-Scroller or Top-Down)

1. Create a GameObject with a `Rigidbody2D` (set **Freeze Rotation Z**) and a `Collider2D`.
2. Add the **`CharacterMotor2D`** component (`ContentContent.Character.Unity`).
3. Create a config preset via **Right-click → Create → ContentContent → Character → Character Config** and assign it to `_configAsset` (or tune `_inlineConfig` directly):
   - Set **Mode** to `SideScroller2D` or `TopDown2D`.
   - For `SideScroller2D`, configure **Ground Mask**, **Ground Check Offset/Size**, and **Ceiling Check Offset/Size** (visible as green/yellow Gizmos when selected).
4. Feed input from your input bridge (New Input System, legacy Input, or AI):

```csharp
using ContentContent.Character.Unity;
using UnityEngine;

public class PlayerInputBridge2D : MonoBehaviour
{
    [SerializeField] private CharacterMotor2D _motor;

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        _motor.SetMoveInput(new Vector2(x, y));

        if (Input.GetButtonDown("Jump")) _motor.PressJump();
        if (Input.GetButtonUp("Jump")) _motor.ReleaseJump();
        if (Input.GetKeyDown(KeyCode.LeftShift)) _motor.PressDash();
        if (Input.GetButtonDown("Fire1")) _motor.TriggerAction("Attack", bufferWindow: 0.15f);
    }
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
controller.Motor.OnJumped += e => PlayJumpSfx(e.IsAirJump);
controller.Motor.OnLanded += e => SpawnLandingDust(e.ImpactSpeed);
controller.Actions.OnActionStarted += e => animator.SetTrigger(e.ActionId);

// 2. Core Blackboard (auto-synced every tick)
float speed = controller.Blackboard.Get(CharacterBlackboardKeys.PlanarSpeed);
bool grounded = controller.Blackboard.Get(CharacterBlackboardKeys.IsGrounded);

// 3. Optional Core EventBus (attach EventBusBehaviour on CharacterMotor2D/3D)
eventBus.Subscribe<CharacterDashedEvent>((in CharacterDashedEvent e) =>
{
    SpawnDashTrail(e.Direction, e.IsAirDash);
});
```

---

## 5. Pure C# Unit Testing (No Unity Required)

Because `ContentContent.Character` has `"noEngineReferences": true`, you can simulate entire jump/dash/combat sequences deterministically in NUnit:

```csharp
using System.Numerics;
using ContentContent.Character;

var controller = new CharacterController(new CharacterConfig
{
    Mode = MovementMode.SideScroller2D,
    JumpVelocity = 12f,
    CoyoteTime = 0.12f
});

var moveRight = new CharacterInput { Move = Vector3.UnitX };
controller.Tick(0.1f, in moveRight, MotorSensors.Grounded);
```

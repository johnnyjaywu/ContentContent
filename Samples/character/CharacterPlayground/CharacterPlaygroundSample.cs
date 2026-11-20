using System.Collections.Generic;
using ContentContent.Character.Unity;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Samples
{
    /// <summary>
    /// Drop-in interactive character playground demonstrating <see cref="CoreCharacterController"/>,
    /// <see cref="CharacterMotor"/> (SideScroller2D, TopDown2D, ThirdPerson3D), HFSM states,
    /// coyote time, jump buffering, dash, knockback, and custom cooldown-gated actions.
    /// Attach to any empty GameObject and press Play.
    /// </summary>
    public class CharacterPlaygroundSample : MonoBehaviour
    {
        private CoreCharacterController _controller;
        private MovementMode _mode = MovementMode.SideScroller2D;
        private Transform _avatar;
        private Vector3 _position;

        private bool _uiJumpQueued;
        private bool _uiDashQueued;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "SampleCharacterAvatar";
            capsule.transform.SetParent(transform, false);
            Destroy(capsule.GetComponent<Collider>());
            _avatar = capsule.transform;

            BuildController(_mode);
        }

        private void BuildController(MovementMode mode)
        {
            _mode = mode;
            _position = Vector3.zero;

            var config = new CharacterConfig
            {
                Mode = mode,
                MaxSpeed = 8f,
                JumpVelocity = 12f,
                MaxAirJumps = 1,
                DashSpeed = 18f,
                DashDuration = 0.18f,
                DashCooldown = 0.8f
            };

            _controller = new CoreCharacterController(config);
            _controller.OnStateChanged += evt => AppendLog($"[HFSM] {evt.PreviousState} -> {evt.NextState}");
            _controller.Motor.OnJumped += evt => AppendLog($"[Motor] Jump ({(evt.IsAirJump ? "Air" : "Ground")})");
            _controller.Motor.OnLanded += evt => AppendLog($"[Motor] Landed (impact {evt.ImpactSpeed:0.0})");
            _controller.Motor.OnDashed += _ => AppendLog("[Motor] Dash!");

            // Register a custom cooldown-gated Attack action that locks movement while swinging
            var attackAction = new DelegateCharacterAction(
                id: "Attack",
                duration: 0.35f,
                cooldown: 1.0f,
                priority: 10,
                lockFlags: ActionLockFlags.BlockMovement | ActionLockFlags.BlockJump,
                onStart: _ => AppendLog("[Action] Attack started (movement locked)"),
                onEnd: (_, interrupted) => AppendLog(interrupted ? "[Action] Attack interrupted" : "[Action] Attack finished"));

            _controller.Actions.Register(attackAction);
            AppendLog($"Initialized in {mode} mode.");
        }

        private void Update()
        {
            if (_controller == null)
                return;

            CharacterInput input = ReadInput();
            MotorSensors sensors = SampleAnalyticSensors();

            float dt = Time.deltaTime;
            System.Numerics.Vector3 vel = _controller.Tick(dt, in input, in sensors);
            _position += vel.ToUnity() * dt;

            // Clamp to analytic ground plane y = 0 for gravity modes
            if (_mode != MovementMode.TopDown2D && _position.y < 0f)
            {
                _position.y = 0f;
            }

            // Keep avatar within a friendly sandbox radius
            _position.x = Mathf.Clamp(_position.x, -10f, 10f);
            if (_mode == MovementMode.TopDown2D)
            {
                _position.y = Mathf.Clamp(_position.y, -6f, 6f);
                _position.z = 0f;
            }
            else if (_mode == MovementMode.SideScroller2D)
            {
                _position.z = 0f;
            }
            else
            {
                _position.z = Mathf.Clamp(_position.z, -10f, 10f);
            }

            _avatar.position = _position;
        }

        private CharacterInput ReadInput()
        {
            float moveX = 0f;
            float moveY = 0f;
            bool jumpPressed = _uiJumpQueued;
            bool jumpHeld = _uiJumpQueued;
            bool jumpReleased = false;
            bool dashPressed = _uiDashQueued;
            bool attackPressed = false;

            _uiJumpQueued = false;
            _uiDashQueued = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveY -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveY += 1f;

                jumpPressed |= kb.spaceKey.wasPressedThisFrame;
                jumpHeld |= kb.spaceKey.isPressed;
                jumpReleased |= kb.spaceKey.wasReleasedThisFrame;
                dashPressed |= kb.leftShiftKey.wasPressedThisFrame;
                attackPressed |= kb.fKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            moveX = Input.GetAxisRaw("Horizontal");
            moveY = Input.GetAxisRaw("Vertical");
            jumpPressed |= Input.GetKeyDown(KeyCode.Space);
            jumpHeld |= Input.GetKey(KeyCode.Space);
            jumpReleased |= Input.GetKeyUp(KeyCode.Space);
            dashPressed |= Input.GetKeyDown(KeyCode.LeftShift);
            attackPressed |= Input.GetKeyDown(KeyCode.F);
#endif

            if (attackPressed)
            {
                _controller.Actions.TryStart("Attack", _controller.Context, bufferWindow: 0.15f);
            }

            Vector3 moveVec = _mode == MovementMode.ThirdPerson3D
                ? new Vector3(moveX, 0f, moveY)
                : new Vector3(moveX, moveY, 0f);

            return new CharacterInput
            {
                Move = moveVec.ToNumerics(),
                JumpPressed = jumpPressed,
                JumpHeld = jumpHeld,
                JumpReleased = jumpReleased,
                DashPressed = dashPressed
            };
        }

        private MotorSensors SampleAnalyticSensors()
        {
            if (_mode == MovementMode.TopDown2D)
            {
                return MotorSensors.Grounded;
            }

            bool grounded = _position.y <= 0.001f && _controller.Motor.VerticalSpeed <= 0.01f;
            return grounded ? MotorSensors.Grounded : MotorSensors.Airborne;
        }

        private void AppendLog(string entry)
        {
            _log.Add(entry);
            if (_log.Count > 8)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (_controller == null)
                return;

            var motor = _controller.Motor;
            GUILayout.BeginArea(new Rect(16, 16, 420, 460), "Character Playground Sample", GUI.skin.window);

            GUILayout.Label($"Mode: {_mode}  |  HFSM State: {_controller.CurrentState}");
            GUILayout.Label($"Pos: ({_position.x:0.0}, {_position.y:0.0}, {_position.z:0.0})  |  Grounded: {motor.IsGrounded}");
            GUILayout.Label($"Speed: Planar={motor.PlanarSpeed:0.0}  Vert={motor.VerticalSpeed:0.0}  |  AirJumps: {motor.AirJumpsRemaining}");
            GUILayout.Label($"Dash CD: {(motor.IsDashCooldownReady ? "READY" : $"{motor.DashCooldownRemaining:0.0}s")}  |  Action: {(_controller.Actions.ActiveAction?.Id ?? "None")}");
            GUILayout.Label("Controls: WASD/Arrows Move | Space Jump | Shift Dash | F Attack");

            GUILayout.Space(6);
            GUILayout.Label("Switch MovementMode:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SideScroller2D")) BuildController(MovementMode.SideScroller2D);
            if (GUILayout.Button("TopDown2D")) BuildController(MovementMode.TopDown2D);
            if (GUILayout.Button("ThirdPerson3D")) BuildController(MovementMode.ThirdPerson3D);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Trigger Actions:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump")) _uiJumpQueued = true;
            if (GUILayout.Button("Dash")) _uiDashQueued = true;
            if (GUILayout.Button("Attack (Lock)")) _controller.Actions.TryStart("Attack", _controller.Context, bufferWindow: 0.15f);
            if (GUILayout.Button("Knockback"))
            {
                motor.ApplyImpulse(new System.Numerics.Vector3(-10f, 6f, 0f));
                AppendLog("[Motor] Applied knockback impulse");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Event Log:");
            for (int i = 0; i < _log.Count; i++)
            {
                GUILayout.Label(_log[i]);
            }

            GUILayout.EndArea();
        }
    }
}

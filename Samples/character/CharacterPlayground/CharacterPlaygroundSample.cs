using System;
using System.Collections.Generic;
using ContentContent.Character.Unity;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ContentContent.Character.Samples
{
    /// <summary>
    /// Real Physics2D playable playground built with <see cref="CharacterMotor2D"/>,
    /// <see cref="MovingPlatform2D"/>, one-way platforms, walls, a pogo bounce pad,
    /// and a cooldown-gated <see cref="SimpleCharacterAction"/>.
    /// Attach to an empty GameObject and press Play, then select the spawned Player in the Hierarchy
    /// to inspect live PlayMode Telemetry and tweak feel presets in real time.
    /// </summary>
    public class CharacterPlaygroundSample : MonoBehaviour
    {
#if CONTENTCONTENT_PHYSICS2D
        private sealed class SampleBouncePad2D : MonoBehaviour
        {
            public Action<CharacterMotor2D> OnBounced;

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (other != null && other.TryGetComponent(out CharacterMotor2D motor))
                {
                    motor.ApplyBounce(14f, refillAirJumps: true, refillAirDashes: true);
                    OnBounced?.Invoke(motor);
                }
            }
        }

        private readonly List<string> _log = new List<string>();
        private CharacterMotor2D _motor;
        private Transform _playerTransform;
        private CharacterPresetType _activePreset = CharacterPresetType.ActionPlatformer;
        private bool _usingCharacterInputComponent;

        private void Start()
        {
            BuildArena();
            BuildPlayer();
            ApplyPreset(_activePreset);
        }

        private void BuildArena()
        {
            // Main floor
            CreateBlock("Floor", new Vector2(0f, -2.5f), new Vector2(26f, 1f), new Color(0.24f, 0.28f, 0.34f));

            // Elevated solid ledge (test jump height, coyote time, ceiling corner nudge)
            CreateBlock("RightLedge", new Vector2(5.2f, 0.1f), new Vector2(5f, 0.7f), new Color(0.3f, 0.35f, 0.42f));

            // Left & right walls (test wall slide, wall jump, and wall climb)
            CreateBlock("LeftWall", new Vector2(-10.5f, 2.5f), new Vector2(1f, 11f), new Color(0.24f, 0.28f, 0.34f));
            CreateBlock("RightWall", new Vector2(10.5f, 2.5f), new Vector2(1f, 11f), new Color(0.24f, 0.28f, 0.34f));
            CreateBlock("WallJumpPillar", new Vector2(8.2f, 2.4f), new Vector2(0.8f, 5.2f), new Color(0.36f, 0.43f, 0.52f));

            // One-way platform (jump through from below, press Down + Space to drop through)
            GameObject oneWay = CreateBlock("OneWayPlatform", new Vector2(-4.5f, -0.3f), new Vector2(4.2f, 0.35f), new Color(0.26f, 0.58f, 0.46f));
            BoxCollider2D oneWayCol = oneWay.GetComponent<BoxCollider2D>();
            oneWayCol.usedByEffector = true;
            PlatformEffector2D effector = oneWay.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;

            // Moving platform using MovingPlatform2D
            GameObject movingGo = CreateBlock("MovingPlatform", new Vector2(-2.2f, 1.8f), new Vector2(3.2f, 0.4f), new Color(0.25f, 0.52f, 0.78f));
            Rigidbody2D platRb = movingGo.AddComponent<Rigidbody2D>();
            platRb.bodyType = RigidbodyType2D.Kinematic;
            MovingPlatform2D movingPlatform = movingGo.AddComponent<MovingPlatform2D>();
            movingPlatform.ConfigurePatrol(new Vector2(4.4f, 0f), speed: 2.6f, pauseDuration: 0.35f);

            // Bounce pad (refills air jump & air dash)
            GameObject bounceGo = CreateBlock("BouncePad", new Vector2(-7.5f, -1.75f), new Vector2(1.8f, 0.4f), new Color(0.85f, 0.52f, 0.18f));
            BoxCollider2D bounceCol = bounceGo.GetComponent<BoxCollider2D>();
            bounceCol.isTrigger = true;
            SampleBouncePad2D pad = bounceGo.AddComponent<SampleBouncePad2D>();
            pad.OnBounced = _ => AppendLog("[BouncePad] Pogo bounce + air abilities refilled!");
        }

        private void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.SetParent(transform, false);
            player.transform.position = new Vector3(0f, -0.5f, 0f);
            _playerTransform = player.transform;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 0.8f, 0.6f);
            Destroy(visual.GetComponent<Collider>());
            SetTint(visual, new Color(0.92f, 0.94f, 0.98f));

            var col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.sharedMaterial = new PhysicsMaterial2D("CharacterNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _motor = player.AddComponent<CharacterMotor2D>();
            _motor.AutoFitSensorsFromCollider();

            // Register a cooldown-gated Attack action directly via SimpleCharacterAction
            var attack = new SimpleCharacterAction(
                id: "Attack",
                duration: 0.25f,
                cooldown: 0.6f,
                priority: 1,
                lockFlags: ActionLockFlags.BlockMovement | ActionLockFlags.BlockJump,
                onStart: _ => AppendLog("[Action] Attack started (movement locked)"),
                onEnd: (_, interrupted) => AppendLog(interrupted ? "[Action] Attack interrupted" : "[Action] Attack finished"));
            _motor.Controller.RegisterAction(attack);

            // Attach code-free CharacterInput2D when com.unity.inputsystem is present
            Type input2DType = Type.GetType("ContentContent.Character.Unity.Input.CharacterInput2D, ContentContent.Character.Input", throwOnError: false);
            if (input2DType != null)
            {
                player.AddComponent(input2DType);
                _usingCharacterInputComponent = true;
            }

            _motor.OnJumped.AddListener(() => AppendLog("[Motor] Jump"));
            _motor.OnWallJumped.AddListener(() => AppendLog("[Motor] Wall Jump!"));
            _motor.OnLanded.AddListener(speed => AppendLog($"[Motor] Landed (impact {speed:0.0})"));
            _motor.OnDashed.AddListener(() => AppendLog("[Motor] Dash!"));
            _motor.OnStateChanged.AddListener(state => AppendLog($"[HFSM] -> {state}"));
        }

        private GameObject CreateBlock(string objectName, Vector2 pos, Vector2 size, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            Destroy(go.GetComponent<Collider>());

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            SetTint(go, tint);
            return go;
        }

        private static void SetTint(GameObject go, Color tint)
        {
            if (go.TryGetComponent(out Renderer rend))
            {
                rend.material.color = tint;
            }
        }

        private void ApplyPreset(CharacterPresetType preset)
        {
            _activePreset = preset;
            if (_motor == null)
            {
                return;
            }

            _motor.ApplyPreset(preset);
            _motor.Controller.Reset();
            _playerTransform.position = new Vector3(0f, -0.5f, 0f);
            AppendLog($"Applied {preset} preset.");
        }

        private void Update()
        {
            if (_motor == null)
            {
                return;
            }

            bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                attackPressed = kb.fKey.wasPressedThisFrame;

                if (!_usingCharacterInputComponent)
                {
                    float mx = 0f;
                    float my = 0f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mx -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mx += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) my -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) my += 1f;
                    _motor.SetMoveInput(new Vector2(mx, my));

                    if (kb.spaceKey.wasPressedThisFrame)
                    {
                        if (my < -0.5f && _motor.Motor.Sensors.IsOnOneWayPlatform)
                            _motor.PressDropThrough();
                        else
                            _motor.PressJump();
                    }
                    if (kb.spaceKey.wasReleasedThisFrame) _motor.ReleaseJump();
                    if (kb.leftShiftKey.wasPressedThisFrame) _motor.PressDash();
                    _motor.SetGrabHeld(kb.eKey.isPressed || kb.leftCtrlKey.isPressed);
                }
            }
#else
            attackPressed = Input.GetKeyDown(KeyCode.F);
            if (!_usingCharacterInputComponent)
            {
                float mx = Input.GetAxisRaw("Horizontal");
                float my = Input.GetAxisRaw("Vertical");
                _motor.SetMoveInput(new Vector2(mx, my));
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (my < -0.5f && _motor.Motor.Sensors.IsOnOneWayPlatform)
                        _motor.PressDropThrough();
                    else
                        _motor.PressJump();
                }
                if (Input.GetKeyUp(KeyCode.Space)) _motor.ReleaseJump();
                if (Input.GetKeyDown(KeyCode.LeftShift)) _motor.PressDash();
                _motor.SetGrabHeld(Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.LeftControl));
            }
#endif

            if (attackPressed)
            {
                _motor.TriggerAction("Attack", 0.15f);
            }
        }

        private void AppendLog(string entry)
        {
            _log.Add(entry);
            if (_log.Count > 7)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (_motor == null || _motor.Controller == null)
            {
                return;
            }

            CharacterMotor motor = _motor.Motor;
            GUILayout.BeginArea(new Rect(16, 16, 450, 440), "Character Playground (Physics2D)", GUI.skin.window);

            GUILayout.Label($"Preset: {_activePreset}  |  HFSM State: {_motor.CurrentStateName}");
            GUILayout.Label($"Vel: ({motor.Velocity.X:0.0}, {motor.Velocity.Y:0.0})  |  Grounded: {motor.IsGrounded}  |  Wall: {motor.IsWallSliding || motor.IsWallClimbing}");
            GUILayout.Label($"AirJumps: {motor.AirJumpsRemaining}  |  AirDashes: {motor.AirDashesRemaining}  |  Action: {(_motor.Controller.Actions.ActiveAction?.Id ?? "None")}");
            GUILayout.Label("Move: WASD/Arrows | Jump: Space | Dash: Shift | Climb: E | Drop-Through: S+Space | Attack: F");

            GUILayout.Space(6);
            GUILayout.Label("Switch Feel Preset:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Side-Scroller")) ApplyPreset(CharacterPresetType.SideScroller);
            if (GUILayout.Button("Action")) ApplyPreset(CharacterPresetType.ActionPlatformer);
            if (GUILayout.Button("Climber")) ApplyPreset(CharacterPresetType.PrecisionClimber);
            if (GUILayout.Button("Top-Down")) ApplyPreset(CharacterPresetType.TopDown);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Quick Triggers:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Attack (Lock)")) _motor.TriggerAction("Attack", 0.15f);
            if (GUILayout.Button("Knockback"))
            {
                _motor.ApplyImpulse(new Vector2(-10f, 6f));
                AppendLog("[Motor] Applied knockback impulse");
            }
            if (GUILayout.Button("Reset Position"))
            {
                _motor.Controller.Reset();
                _playerTransform.position = new Vector3(0f, -0.5f, 0f);
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
#endif
    }
}

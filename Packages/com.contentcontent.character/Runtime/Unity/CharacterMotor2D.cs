#if CONTENTCONTENT_PHYSICS2D
using System.Collections.Generic;
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using ContentContent.Core.Fsm;
using UnityEngine;
using UnityEngine.Events;
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Code-free <see cref="Rigidbody2D"/> adapter bridging Unity 2D physics to the pure-C#
    /// <see cref="CoreCharacterController"/> for both <see cref="MovementMode.SideScroller2D"/>
    /// and <see cref="MovementMode.TopDown2D"/> games.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Character Motor 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMotor2D : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional shared CharacterConfigAsset preset. Overrides Inline Config when assigned.")]
        [SerializeField] private CharacterConfigAsset _configAsset;

        [Tooltip("Per-character movement and feel settings used when Config Asset is unassigned.")]
        [SerializeField] private CharacterConfig _inlineConfig = new CharacterConfig();

        [Tooltip("While playing in the Unity Editor, automatically reflects Inspector slider tweaks in real time.")]
        [SerializeField] private bool _liveSyncConfigInEditor = true;

        [Header("Optional Core Integrations")]
        [Tooltip("Optional BlackboardBehaviour synchronized every tick. Auto-detected on this GameObject if left empty.")]
        [SerializeField] private BlackboardBehaviour _blackboardBehaviour;

        [Tooltip("Optional EventBusBehaviour used to publish zero-copy character struct events. Auto-detected if left empty.")]
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Header("2D Sensors (SideScroller2D)")]
        [Tooltip("Physics2D layers treated as solid ground and ceilings.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Tooltip("Physics2D layers treated as drop-through one-way platforms (PlatformEffector2D is also auto-detected).")]
        [SerializeField] private LayerMask _oneWayPlatformMask = 0;

        [Tooltip("Local offset of the ground-check overlap box from the character's position.")]
        [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0f, -0.5f);

        [Tooltip("Size of the ground-check overlap box.")]
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.6f, 0.12f);

        [Tooltip("Local offset of the ceiling-check overlap box from the character's position.")]
        [SerializeField] private Vector2 _ceilingCheckOffset = new Vector2(0f, 0.5f);

        [Tooltip("Size of the ceiling-check overlap box.")]
        [SerializeField] private Vector2 _ceilingCheckSize = new Vector2(0.6f, 0.12f);

        [Header("Wall Sensors")]
        [Tooltip("Physics2D layers treated as climbable/slidable walls.")]
        [SerializeField] private LayerMask _wallMask = ~0;

        [Tooltip("Horizontal distance from center and vertical offset for left/right wall checks.")]
        [SerializeField] private Vector2 _wallCheckOffset = new Vector2(0.45f, 0f);

        [Tooltip("Size of each wall-check overlap box.")]
        [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.12f, 0.7f);

        [Header("Ceiling Corner Correction")]
        [Tooltip("Nudges the character sideways around thin ceiling corners during upward jumps.")]
        [SerializeField] private bool _enableCornerCorrection = true;

        [Tooltip("Maximum horizontal nudge distance applied when clipping a single edge of the ceiling sensor.")]
        [SerializeField] private float _cornerCorrectionDistance = 0.25f;

        [Tooltip("Height of the corner edge probes.")]
        [SerializeField] private float _cornerCorrectionProbeHeight = 0.1f;

        [Header("Actions (Code-Free)")]
        [Tooltip("Reusable CharacterActionAssets automatically registered with UnityEvent callbacks.")]
        [SerializeField] private List<CharacterActionBinding> _actions = new List<CharacterActionBinding>();

        [Header("Events")]
        [SerializeField] private UnityEvent _onJumped;
        [SerializeField] private UnityEvent _onWallJumped;
        [SerializeField] private UnityEvent<float> _onLanded;
        [SerializeField] private UnityEvent _onDashed;
        [SerializeField] private UnityEvent<Vector2> _onFacingChanged;
        [SerializeField] private UnityEvent<string> _onStateChanged;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[8];
        private readonly RaycastHit2D[] _castBuffer = new RaycastHit2D[8];

        private Rigidbody2D _body;
        private Collider2D _selfCollider;
        private Collider2D _groundCollider;
        private Collider2D _ignoredPlatformCollider;
        private CoreCharacterController _controller;

        private Vector2 _moveInput;
        private Vector2 _lastFacing = Vector2.right;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _jumpReleased;
        private bool _dashPressed;
        private bool _grabHeld;
        private bool _dropThroughPressed;

        /// <summary>Underlying pure-C# character controller.</summary>
        public CoreCharacterController Controller => _controller;

        /// <summary>Underlying kinematic motor.</summary>
        public CharacterMotor Motor => _controller?.Motor;

        /// <summary>Assigned shared config asset, if any.</summary>
        public CharacterConfigAsset ConfigAsset
        {
            get => _configAsset;
            set => _configAsset = value;
        }

        /// <summary>Inline configuration used when <see cref="ConfigAsset"/> is null.</summary>
        public CharacterConfig InlineConfig => _inlineConfig;

        /// <summary>Active configuration source (either <see cref="ConfigAsset"/> or <see cref="InlineConfig"/>).</summary>
        public CharacterConfig ActiveConfigSource => _configAsset != null ? _configAsset.Config : _inlineConfig;

        /// <summary>Current world-space velocity applied to the <see cref="Rigidbody2D"/>.</summary>
        public Vector2 Velocity => _controller != null ? _controller.Velocity.ToUnity2D() : Vector2.zero;

        /// <summary>Current normalized facing direction.</summary>
        public Vector2 FacingDirection => _controller != null ? _controller.Motor.FacingDirection.ToUnity2D() : _lastFacing;

        /// <summary>True when currently grounded.</summary>
        public bool IsGrounded => _controller != null && _controller.Motor.IsGrounded;

        /// <summary>Current HFSM state name for debugging/Inspector inspection.</summary>
        public string CurrentStateName => _controller?.CurrentState?.ToString() ?? "None";

        public UnityEvent OnJumped => _onJumped;
        public UnityEvent OnWallJumped => _onWallJumped;
        public UnityEvent<float> OnLanded => _onLanded;
        public UnityEvent OnDashed => _onDashed;
        public UnityEvent<Vector2> OnFacingChanged => _onFacingChanged;
        public UnityEvent<string> OnStateChanged => _onStateChanged;

        private void Reset()
        {
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (_blackboardBehaviour == null)
            {
                _blackboardBehaviour = GetComponent<BlackboardBehaviour>();
            }

            if (_eventBusBehaviour == null)
            {
                _eventBusBehaviour = GetComponent<EventBusBehaviour>();
            }

            if (_inlineConfig == null)
            {
                _inlineConfig = new CharacterConfig();
            }
            else if (_inlineConfig.Mode == MovementMode.ThirdPerson3D)
            {
                _inlineConfig.Mode = MovementMode.SideScroller2D;
            }

            AutoFitSensorsFromCollider();
        }

        private void OnValidate()
        {
            if (_inlineConfig == null)
            {
                _inlineConfig = new CharacterConfig();
            }

            _inlineConfig.Sanitize();
            _groundCheckSize.x = Mathf.Max(0.02f, _groundCheckSize.x);
            _groundCheckSize.y = Mathf.Max(0.02f, _groundCheckSize.y);
            _ceilingCheckSize.x = Mathf.Max(0.02f, _ceilingCheckSize.x);
            _ceilingCheckSize.y = Mathf.Max(0.02f, _ceilingCheckSize.y);
            _wallCheckSize.x = Mathf.Max(0.02f, _wallCheckSize.x);
            _wallCheckSize.y = Mathf.Max(0.02f, _wallCheckSize.y);
            _cornerCorrectionDistance = Mathf.Max(0f, _cornerCorrectionDistance);
            _cornerCorrectionProbeHeight = Mathf.Max(0.02f, _cornerCorrectionProbeHeight);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _selfCollider = GetComponent<Collider2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;

            if (_blackboardBehaviour == null)
            {
                _blackboardBehaviour = GetComponent<BlackboardBehaviour>();
            }

            if (_eventBusBehaviour == null)
            {
                _eventBusBehaviour = GetComponent<EventBusBehaviour>();
            }

            CharacterConfig config = _configAsset != null
                ? _configAsset.CreateRuntimeCopy()
                : _inlineConfig.Clone();
            config.Sanitize();

            _controller = new CoreCharacterController(
                config,
                _blackboardBehaviour != null ? _blackboardBehaviour.Blackboard : null,
                _eventBusBehaviour != null ? _eventBusBehaviour.Bus : null);

            _lastFacing = _controller.Motor.FacingDirection.ToUnity2D();

            CharacterMotorUtility.RegisterActionBindings(_controller, _actions, this);

            _controller.Motor.OnJumped += HandleJumped;
            _controller.Motor.OnWallJumped += HandleWallJumped;
            _controller.Motor.OnLanded += HandleLanded;
            _controller.Motor.OnDashed += HandleDashed;
            _controller.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            RestoreIgnoredPlatformCollision();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Motor.OnJumped -= HandleJumped;
                _controller.Motor.OnWallJumped -= HandleWallJumped;
                _controller.Motor.OnLanded -= HandleLanded;
                _controller.Motor.OnDashed -= HandleDashed;
                _controller.OnStateChanged -= HandleStateChanged;
            }
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (_liveSyncConfigInEditor)
            {
                CharacterMotorUtility.SyncLiveConfig(_controller, _configAsset, _inlineConfig);
            }
#endif

            var input = new CharacterInput
            {
                Move = _moveInput.ToNumerics(),
                JumpPressed = _jumpPressed,
                JumpHeld = _jumpHeld,
                JumpReleased = _jumpReleased,
                DashPressed = _dashPressed,
                GrabHeld = _grabHeld,
                DropThroughPressed = _dropThroughPressed
            };

            _jumpPressed = false;
            _jumpReleased = false;
            _dashPressed = false;
            _dropThroughPressed = false;

            MotorSensors sensors = SampleSensors();
            System.Numerics.Vector3 velocity = _controller.Tick(Time.fixedDeltaTime, in input, in sensors);
            _controller.FixedTick(Time.fixedDeltaTime);

            Vector2 currentFacing = _controller.Motor.FacingDirection.ToUnity2D();
            if ((currentFacing - _lastFacing).sqrMagnitude > 0.0001f)
            {
                _lastFacing = currentFacing;
                _onFacingChanged?.Invoke(currentFacing);
            }

            UpdateOneWayDropThroughCollision();

            Vector2 unityVelocity = velocity.ToUnity2D();
#if UNITY_6000_0_OR_NEWER
            _body.linearVelocity = unityVelocity;
#else
            _body.velocity = unityVelocity;
#endif
        }

        /// <summary>
        /// Automatically sizes ground, ceiling, and wall sensor boxes from the attached <see cref="Collider2D"/>.
        /// Returns false if no <see cref="Collider2D"/> is present.
        /// </summary>
        public bool AutoFitSensorsFromCollider()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                return false;
            }

            float sx = Mathf.Abs(transform.lossyScale.x);
            float sy = Mathf.Abs(transform.lossyScale.y);
            Vector2 localCenter;
            Vector2 size;

            if (col is BoxCollider2D box)
            {
                localCenter = new Vector2(box.offset.x * sx, box.offset.y * sy);
                size = new Vector2(box.size.x * sx, box.size.y * sy);
            }
            else if (col is CapsuleCollider2D capsule)
            {
                localCenter = new Vector2(capsule.offset.x * sx, capsule.offset.y * sy);
                size = new Vector2(capsule.size.x * sx, capsule.size.y * sy);
            }
            else if (col is CircleCollider2D circle)
            {
                float d = circle.radius * 2f * Mathf.Max(sx, sy);
                localCenter = new Vector2(circle.offset.x * sx, circle.offset.y * sy);
                size = new Vector2(d, d);
            }
            else
            {
                Bounds bounds = col.bounds;
                localCenter = (Vector2)(bounds.center - transform.position);
                size = bounds.size;
            }

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            _groundCheckOffset = new Vector2(localCenter.x, localCenter.y - halfH);
            _groundCheckSize = new Vector2(Mathf.Max(0.1f, size.x * 0.8f), 0.12f);

            _ceilingCheckOffset = new Vector2(localCenter.x, localCenter.y + halfH);
            _ceilingCheckSize = new Vector2(Mathf.Max(0.1f, size.x * 0.8f), 0.12f);

            _wallCheckOffset = new Vector2(Mathf.Abs(localCenter.x) + halfW + 0.03f, localCenter.y);
            _wallCheckSize = new Vector2(0.12f, Mathf.Max(0.1f, size.y * 0.7f));

            return true;
        }

        /// <summary>Applies a built-in preset to the active config and refreshes the motor if initialized.</summary>
        public void ApplyPreset(CharacterPresetType preset)
        {
            CharacterMotorUtility.ApplyPreset(_controller, _configAsset, _inlineConfig, preset);
        }

        /// <summary>Sets the current 2D movement input vector (e.g. from Unity Input System or AI).</summary>
        public void SetMoveInput(Vector2 move) => _moveInput = move;

        /// <summary>Signals that the jump button was pressed.</summary>
        public void PressJump()
        {
            _jumpPressed = true;
            _jumpHeld = true;
        }

        /// <summary>Signals that the jump button was released (enables variable jump height).</summary>
        public void ReleaseJump()
        {
            _jumpHeld = false;
            _jumpReleased = true;
        }

        /// <summary>Signals that the dash button was pressed.</summary>
        public void PressDash() => _dashPressed = true;

        /// <summary>Sets whether the wall-grab / climb input is currently held.</summary>
        public void SetGrabHeld(bool held) => _grabHeld = held;

        /// <summary>Signals a drop-through press while standing on a one-way platform.</summary>
        public void PressDropThrough() => _dropThroughPressed = true;

        /// <summary>Attempts to start or buffer a registered <see cref="ICharacterAction"/> by ID.</summary>
        public bool TriggerAction(string actionId, float bufferWindow = 0.12f)
        {
            return _controller != null && _controller.TryStartAction(actionId, bufferWindow);
        }

        /// <summary>Applies an instantaneous 2D impulse (e.g. knockback or bounce pad).</summary>
        public void ApplyImpulse(Vector2 impulse)
        {
            _controller?.Motor.ApplyImpulse(impulse.ToNumerics());
        }

        /// <summary>Applies an upward bounce (e.g. down-slash pogo) and optionally refills air charges.</summary>
        public void ApplyBounce(float upwardSpeed, bool refillAirJumps = true, bool refillAirDashes = true)
        {
            _controller?.Motor.ApplyBounce(upwardSpeed, refillAirJumps, refillAirDashes);
        }

        /// <summary>Refills mid-air jump charges.</summary>
        public void RefillAirJumps(int? count = null)
        {
            _controller?.Motor.RefillAirJumps(count);
        }

        /// <summary>Refills mid-air dash charges and optionally resets dash cooldown.</summary>
        public void RefillAirDashes(int? count = null, bool resetCooldown = false)
        {
            _controller?.Motor.RefillAirDashes(count, resetCooldown);
        }

        private void HandleJumped(CharacterJumpedEvent _) => _onJumped?.Invoke();
        private void HandleWallJumped(CharacterWallJumpedEvent _) => _onWallJumped?.Invoke();
        private void HandleLanded(CharacterLandedEvent evt) => _onLanded?.Invoke(evt.ImpactSpeed);
        private void HandleDashed(CharacterDashedEvent _) => _onDashed?.Invoke();
        private void HandleStateChanged(CharacterStateChangedEvent evt) => _onStateChanged?.Invoke(evt.NextState?.ToString() ?? "None");

        private MotorSensors SampleSensors()
        {
            _groundCollider = null;

            if (_controller.Config.Mode == MovementMode.TopDown2D)
            {
                return MotorSensors.Grounded;
            }

            Vector2 pos = _body.position;
            Collider2D groundCol = OverlapBoxNonSelf(pos + _groundCheckOffset, _groundCheckSize, _groundMask);
            bool isOneWay = groundCol != null && IsOneWayCollider(groundCol);

            // Avoid treating upward passage through a one-way platform as grounded
            if (isOneWay && _controller.Motor.LocomotionVelocity.Y > 0.05f)
            {
                groundCol = null;
                isOneWay = false;
            }

            _groundCollider = groundCol;
            bool grounded = groundCol != null;

            System.Numerics.Vector3 groundNormal = System.Numerics.Vector3.UnitY;
            Vector2 platformVelocity = Vector2.zero;

            if (grounded)
            {
                groundNormal = SampleGroundNormal(pos);
                platformVelocity = ReadSurfaceVelocity(groundCol);
            }

            Collider2D ceilingCol = OverlapBoxNonSelf(pos + _ceilingCheckOffset, _ceilingCheckSize, _groundMask);
            bool ceiling = ceilingCol != null;

            if (ceiling &&
                _enableCornerCorrection &&
                _controller.Motor.LocomotionVelocity.Y > 0.01f &&
                _cornerCorrectionDistance > 0f &&
                TryApplyCeilingCornerCorrection(ref pos))
            {
                ceiling = false;
            }

            bool touchingWall = false;
            System.Numerics.Vector3 wallNormal = System.Numerics.Vector3.Zero;

            Vector2 leftOffset = new Vector2(-Mathf.Abs(_wallCheckOffset.x), _wallCheckOffset.y);
            Vector2 rightOffset = new Vector2(Mathf.Abs(_wallCheckOffset.x), _wallCheckOffset.y);

            if (OverlapBoxNonSelf(pos + leftOffset, _wallCheckSize, _wallMask) != null)
            {
                touchingWall = true;
                wallNormal = System.Numerics.Vector3.UnitX;
            }
            else if (OverlapBoxNonSelf(pos + rightOffset, _wallCheckSize, _wallMask) != null)
            {
                touchingWall = true;
                wallNormal = -System.Numerics.Vector3.UnitX;
            }

            return new MotorSensors
            {
                IsGrounded = grounded,
                GroundNormal = groundNormal,
                IsTouchingCeiling = ceiling,
                IsTouchingWall = touchingWall,
                WallNormal = wallNormal,
                PlatformVelocity = platformVelocity.ToNumerics(),
                IsOnOneWayPlatform = isOneWay
            };
        }

        private bool TryApplyCeilingCornerCorrection(ref Vector2 pos)
        {
            float probeWidth = Mathf.Min(_cornerCorrectionDistance, _ceilingCheckSize.x * 0.45f);
            float probeHeight = Mathf.Max(0.02f, _cornerCorrectionProbeHeight);
            Vector2 probeSize = new Vector2(probeWidth, probeHeight);

            float edgeX = _ceilingCheckSize.x * 0.5f - probeWidth * 0.5f;
            bool leftHit = OverlapBoxNonSelf(pos + _ceilingCheckOffset + new Vector2(-edgeX, 0f), probeSize, _groundMask) != null;
            bool rightHit = OverlapBoxNonSelf(pos + _ceilingCheckOffset + new Vector2(edgeX, 0f), probeSize, _groundMask) != null;

            if (leftHit == rightHit)
            {
                return false;
            }

            float nudgeX = leftHit ? _cornerCorrectionDistance : -_cornerCorrectionDistance;
            Vector2 candidatePos = pos + new Vector2(nudgeX, 0f);

            if (OverlapBoxNonSelf(candidatePos + _ceilingCheckOffset, _ceilingCheckSize, _groundMask) != null)
            {
                return false;
            }

            pos = candidatePos;
            _body.position = candidatePos;
            return true;
        }

        private void UpdateOneWayDropThroughCollision()
        {
            if (_controller.Motor.IsDroppingThroughPlatform)
            {
                if (_ignoredPlatformCollider == null && _groundCollider != null && IsOneWayCollider(_groundCollider))
                {
                    _ignoredPlatformCollider = _groundCollider;
                    if (_selfCollider != null)
                    {
                        Physics2D.IgnoreCollision(_selfCollider, _ignoredPlatformCollider, true);
                    }
                }
            }
            else
            {
                RestoreIgnoredPlatformCollision();
            }
        }

        private void RestoreIgnoredPlatformCollision()
        {
            if (_ignoredPlatformCollider != null)
            {
                if (_selfCollider != null)
                {
                    Physics2D.IgnoreCollision(_selfCollider, _ignoredPlatformCollider, false);
                }

                _ignoredPlatformCollider = null;
            }
        }

        private bool IsOneWayCollider(Collider2D col)
        {
            if (col.GetComponent<PlatformEffector2D>() != null)
            {
                return true;
            }

            return (_oneWayPlatformMask.value & (1 << col.gameObject.layer)) != 0;
        }

        private Vector2 ReadSurfaceVelocity(Collider2D groundCol)
        {
            var provider = groundCol.GetComponent<ISurfaceVelocityProvider>();
            if (provider != null)
            {
                return provider.SurfaceVelocity;
            }

            if (groundCol.attachedRigidbody != null && groundCol.attachedRigidbody != _body)
            {
#if UNITY_6000_0_OR_NEWER
                return groundCol.attachedRigidbody.linearVelocity;
#else
                return groundCol.attachedRigidbody.velocity;
#endif
            }

            return Vector2.zero;
        }

        private System.Numerics.Vector3 SampleGroundNormal(Vector2 pos)
        {
            var filter = CreateLayerFilter(_groundMask);
            int count = Physics2D.BoxCast(
                pos + _groundCheckOffset + new Vector2(0f, 0.04f),
                _groundCheckSize,
                0f,
                Vector2.down,
                filter,
                _castBuffer,
                0.08f);

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _castBuffer[i];
                if (hit.collider != null &&
                    hit.collider != _selfCollider &&
                    hit.collider != _ignoredPlatformCollider &&
                    hit.normal.sqrMagnitude > 0.0001f)
                {
                    return hit.normal.ToNumerics();
                }
            }

            return System.Numerics.Vector3.UnitY;
        }

        private Collider2D OverlapBoxNonSelf(Vector2 center, Vector2 size, LayerMask mask)
        {
            var filter = CreateLayerFilter(mask);
            int count = Physics2D.OverlapBox(center, size, 0f, filter, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapBuffer[i];
                if (col != null && col != _selfCollider && col != _ignoredPlatformCollider)
                {
                    return col;
                }
            }

            return null;
        }

        private static ContactFilter2D CreateLayerFilter(LayerMask mask)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = false;
            return filter;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos + (Vector3)_groundCheckOffset, new Vector3(_groundCheckSize.x, _groundCheckSize.y, 0.01f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(pos + (Vector3)_ceilingCheckOffset, new Vector3(_ceilingCheckSize.x, _ceilingCheckSize.y, 0.01f));

            Gizmos.color = Color.cyan;
            Vector3 wallSize = new Vector3(_wallCheckSize.x, _wallCheckSize.y, 0.01f);
            Vector3 leftOffset = new Vector3(-Mathf.Abs(_wallCheckOffset.x), _wallCheckOffset.y, 0f);
            Vector3 rightOffset = new Vector3(Mathf.Abs(_wallCheckOffset.x), _wallCheckOffset.y, 0f);
            Gizmos.DrawWireCube(pos + leftOffset, wallSize);
            Gizmos.DrawWireCube(pos + rightOffset, wallSize);
        }
    }
}
#endif

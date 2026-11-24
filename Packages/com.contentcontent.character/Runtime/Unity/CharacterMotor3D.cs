#if CONTENTCONTENT_PHYSICS
using System.Collections.Generic;
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using UnityEngine;
using UnityEngine.Events;
using CoreCharacterController = ContentContent.Character.CharacterController;
using UnityCharacterController = UnityEngine.CharacterController;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Code-free <see cref="UnityCharacterController"/> adapter bridging Unity 3D collision &amp; movement
    /// to the pure-C# <see cref="CoreCharacterController"/> in <see cref="MovementMode.ThirdPerson3D"/>.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Character Motor 3D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    [RequireComponent(typeof(UnityCharacterController))]
    public class CharacterMotor3D : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional shared CharacterConfigAsset preset. Overrides Inline Config when assigned.")]
        [SerializeField] private CharacterConfigAsset _configAsset;

        [Tooltip("Per-character movement and feel settings used when Config Asset is unassigned.")]
        [SerializeField] private CharacterConfig _inlineConfig = CharacterConfig.CreateThirdPersonPreset();

        [Tooltip("While playing in the Unity Editor, automatically reflects Inspector slider tweaks in real time.")]
        [SerializeField] private bool _liveSyncConfigInEditor = true;

        [Header("Optional Core Integrations")]
        [Tooltip("Optional BlackboardBehaviour synchronized every tick. Auto-detected on this GameObject if left empty.")]
        [SerializeField] private BlackboardBehaviour _blackboardBehaviour;

        [Tooltip("Optional EventBusBehaviour used to publish zero-copy character struct events. Auto-detected if left empty.")]
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Header("3D Sensors")]
        [Tooltip("Physics layers treated as solid ground and ceilings.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Tooltip("Local offset of the ground-check sphere from the character's position.")]
        [SerializeField] private Vector3 _groundCheckOffset = new Vector3(0f, -0.9f, 0f);

        [Tooltip("Radius of the ground-check sphere.")]
        [SerializeField] private float _groundCheckRadius = 0.25f;

        [Tooltip("Local offset of the ceiling-check sphere from the character's position.")]
        [SerializeField] private Vector3 _ceilingCheckOffset = new Vector3(0f, 0.9f, 0f);

        [Tooltip("Radius of the ceiling-check sphere.")]
        [SerializeField] private float _ceilingCheckRadius = 0.25f;

        [Header("Actions (Code-Free)")]
        [Tooltip("Reusable CharacterActionAssets automatically registered with UnityEvent callbacks.")]
        [SerializeField] private List<CharacterActionBinding> _actions = new List<CharacterActionBinding>();

        [Header("Events")]
        [SerializeField] private UnityEvent _onJumped;
        [SerializeField] private UnityEvent<float> _onLanded;
        [SerializeField] private UnityEvent _onDashed;
        [SerializeField] private UnityEvent<Vector3> _onFacingChanged;
        [SerializeField] private UnityEvent<string> _onStateChanged;

        private UnityCharacterController _unityController;
        private CoreCharacterController _controller;

        private Vector3 _moveInput;
        private Vector3 _lastFacing = Vector3.forward;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _jumpReleased;
        private bool _dashPressed;

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

        /// <summary>Current world-space velocity.</summary>
        public Vector3 Velocity => _controller != null ? _controller.Velocity.ToUnity() : Vector3.zero;

        /// <summary>Current normalized facing direction.</summary>
        public Vector3 FacingDirection => _controller != null ? _controller.Motor.FacingDirection.ToUnity() : _lastFacing;

        /// <summary>True when currently grounded.</summary>
        public bool IsGrounded => _controller != null && _controller.Motor.IsGrounded;

        /// <summary>Current HFSM state name for debugging/Inspector inspection.</summary>
        public string CurrentStateName => _controller?.CurrentState?.ToString() ?? "None";

        public UnityEvent OnJumped => _onJumped;
        public UnityEvent<float> OnLanded => _onLanded;
        public UnityEvent OnDashed => _onDashed;
        public UnityEvent<Vector3> OnFacingChanged => _onFacingChanged;
        public UnityEvent<string> OnStateChanged => _onStateChanged;

        private void Reset()
        {
            if (_blackboardBehaviour == null)
            {
                _blackboardBehaviour = GetComponent<BlackboardBehaviour>();
            }

            if (_eventBusBehaviour == null)
            {
                _eventBusBehaviour = GetComponent<EventBusBehaviour>();
            }

            _inlineConfig = CharacterConfig.CreateThirdPersonPreset();
            AutoFitSensorsFromController();
        }

        private void OnValidate()
        {
            if (_inlineConfig == null)
            {
                _inlineConfig = CharacterConfig.CreateThirdPersonPreset();
            }

            _inlineConfig.Sanitize();
            _groundCheckRadius = Mathf.Max(0.02f, _groundCheckRadius);
            _ceilingCheckRadius = Mathf.Max(0.02f, _ceilingCheckRadius);
        }

        private void Awake()
        {
            _unityController = GetComponent<UnityCharacterController>();

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

            _lastFacing = _controller.Motor.FacingDirection.ToUnity();

            CharacterMotorUtility.RegisterActionBindings(_controller, _actions, this);

            _controller.Motor.OnJumped += HandleJumped;
            _controller.Motor.OnLanded += HandleLanded;
            _controller.Motor.OnDashed += HandleDashed;
            _controller.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Motor.OnJumped -= HandleJumped;
                _controller.Motor.OnLanded -= HandleLanded;
                _controller.Motor.OnDashed -= HandleDashed;
                _controller.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
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
                DashPressed = _dashPressed
            };

            _jumpPressed = false;
            _jumpReleased = false;
            _dashPressed = false;

            MotorSensors sensors = SampleSensors();
            float dt = Time.deltaTime;
            System.Numerics.Vector3 velocity = _controller.Tick(dt, in input, in sensors);

            Vector3 currentFacing = _controller.Motor.FacingDirection.ToUnity();
            if ((currentFacing - _lastFacing).sqrMagnitude > 0.0001f)
            {
                _lastFacing = currentFacing;
                _onFacingChanged?.Invoke(currentFacing);
            }

            _unityController.Move(velocity.ToUnity() * dt);
        }

        private void FixedUpdate()
        {
            _controller?.FixedTick(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Automatically sizes ground and ceiling sensor spheres from the attached <see cref="UnityCharacterController"/>.
        /// Returns false if no <see cref="UnityCharacterController"/> is present.
        /// </summary>
        public bool AutoFitSensorsFromController()
        {
            UnityCharacterController uc = GetComponent<UnityCharacterController>();
            if (uc == null)
            {
                return false;
            }

            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float scaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

            Vector3 center = new Vector3(
                uc.center.x * transform.lossyScale.x,
                uc.center.y * scaleY,
                uc.center.z * transform.lossyScale.z);

            float halfHeight = Mathf.Max(uc.radius * scaleXZ, uc.height * 0.5f * scaleY);
            float radius = Mathf.Max(0.05f, uc.radius * scaleXZ * 0.85f);

            _groundCheckRadius = radius;
            _groundCheckOffset = new Vector3(center.x, center.y - halfHeight + radius * 0.4f, center.z);

            _ceilingCheckRadius = radius;
            _ceilingCheckOffset = new Vector3(center.x, center.y + halfHeight - radius * 0.4f, center.z);

            return true;
        }

        /// <summary>Applies a built-in preset to the active config and refreshes the motor if initialized.</summary>
        public void ApplyPreset(CharacterPresetType preset)
        {
            CharacterMotorUtility.ApplyPreset(_controller, _configAsset, _inlineConfig, preset);
        }

        /// <summary>Sets the current world-space XZ movement direction (Y is ignored).</summary>
        public void SetMoveInput(Vector3 worldPlanarMove) => _moveInput = worldPlanarMove;

        /// <summary>Sets planar movement from a 2D stick input mapped onto the XZ plane.</summary>
        public void SetMoveInput(Vector2 stickInput) => _moveInput = new Vector3(stickInput.x, 0f, stickInput.y);

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

        /// <summary>Attempts to start or buffer a registered <see cref="ICharacterAction"/> by ID.</summary>
        public bool TriggerAction(string actionId, float bufferWindow = 0.12f)
        {
            return _controller != null && _controller.TryStartAction(actionId, bufferWindow);
        }

        /// <summary>Applies an instantaneous 3D impulse (e.g. knockback or launch pad).</summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            _controller?.Motor.ApplyImpulse(impulse.ToNumerics());
        }

        private void HandleJumped(CharacterJumpedEvent _) => _onJumped?.Invoke();
        private void HandleLanded(CharacterLandedEvent evt) => _onLanded?.Invoke(evt.ImpactSpeed);
        private void HandleDashed(CharacterDashedEvent _) => _onDashed?.Invoke();
        private void HandleStateChanged(CharacterStateChangedEvent evt) => _onStateChanged?.Invoke(evt.NextState?.ToString() ?? "None");

        private MotorSensors SampleSensors()
        {
            Vector3 pos = transform.position;
            bool grounded = _unityController.isGrounded
                || Physics.CheckSphere(pos + _groundCheckOffset, _groundCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);
            bool ceiling = Physics.CheckSphere(pos + _ceilingCheckOffset, _ceilingCheckRadius, _groundMask, QueryTriggerInteraction.Ignore);

            return new MotorSensors
            {
                IsGrounded = grounded,
                GroundNormal = System.Numerics.Vector3.UnitY,
                IsTouchingCeiling = ceiling
            };
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos + _groundCheckOffset, _groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos + _ceilingCheckOffset, _ceilingCheckRadius);
        }
    }
}
#endif

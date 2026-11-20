#if CONTENTCONTENT_PHYSICS
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using UnityEngine;
using CoreCharacterController = ContentContent.Character.CharacterController;
using UnityCharacterController = UnityEngine.CharacterController;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Thin <see cref="UnityCharacterController"/> adapter bridging Unity 3D collision &amp; movement
    /// to the pure-C# <see cref="CoreCharacterController"/> in <see cref="MovementMode.ThirdPerson3D"/>.
    /// </summary>
    [RequireComponent(typeof(UnityCharacterController))]
    public class CharacterMotor3D : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CharacterConfigAsset _configAsset;
        [SerializeField] private CharacterConfig _inlineConfig = new CharacterConfig
        {
            Mode = MovementMode.ThirdPerson3D
        };

        [Header("Optional Core Integrations")]
        [SerializeField] private BlackboardBehaviour _blackboardBehaviour;
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Header("3D Sensors")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private Vector3 _groundCheckOffset = new Vector3(0f, -0.9f, 0f);
        [SerializeField] private float _groundCheckRadius = 0.25f;
        [SerializeField] private Vector3 _ceilingCheckOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float _ceilingCheckRadius = 0.25f;

        private UnityCharacterController _unityController;
        private CoreCharacterController _controller;

        private Vector3 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _jumpReleased;
        private bool _dashPressed;

        /// <summary>Underlying pure-C# character controller.</summary>
        public CoreCharacterController Controller => _controller;

        /// <summary>Underlying kinematic motor.</summary>
        public CharacterMotor Motor => _controller?.Motor;

        /// <summary>Current world-space velocity.</summary>
        public Vector3 Velocity => _controller != null ? _controller.Velocity.ToUnity() : Vector3.zero;

        /// <summary>Current HFSM state name for debugging/Inspector inspection.</summary>
        public string CurrentStateName => _controller?.CurrentState?.ToString() ?? "None";

        private void Awake()
        {
            _unityController = GetComponent<UnityCharacterController>();

            CharacterConfig config = _configAsset != null
                ? _configAsset.CreateRuntimeCopy()
                : _inlineConfig.Clone();

            _controller = new CoreCharacterController(
                config,
                _blackboardBehaviour != null ? _blackboardBehaviour.Blackboard : null,
                _eventBusBehaviour != null ? _eventBusBehaviour.Bus : null);
        }

        private void Update()
        {
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

            _unityController.Move(velocity.ToUnity() * dt);
        }

        private void FixedUpdate()
        {
            _controller?.FixedTick(Time.fixedDeltaTime);
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

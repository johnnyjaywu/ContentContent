#if CONTENTCONTENT_PHYSICS2D
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using UnityEngine;
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Thin <see cref="Rigidbody2D"/> adapter bridging Unity 2D physics to the pure-C#
    /// <see cref="CoreCharacterController"/> for both <see cref="MovementMode.SideScroller2D"/>
    /// and <see cref="MovementMode.TopDown2D"/> games.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMotor2D : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CharacterConfigAsset _configAsset;
        [SerializeField] private CharacterConfig _inlineConfig = new CharacterConfig();

        [Header("Optional Core Integrations")]
        [SerializeField] private BlackboardBehaviour _blackboardBehaviour;
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Header("2D Sensors (SideScroller2D)")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0f, -0.5f);
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.6f, 0.12f);
        [SerializeField] private Vector2 _ceilingCheckOffset = new Vector2(0f, 0.5f);
        [SerializeField] private Vector2 _ceilingCheckSize = new Vector2(0.6f, 0.12f);

        private Rigidbody2D _body;
        private CoreCharacterController _controller;

        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _jumpReleased;
        private bool _dashPressed;

        /// <summary>Underlying pure-C# character controller.</summary>
        public CoreCharacterController Controller => _controller;

        /// <summary>Underlying kinematic motor.</summary>
        public CharacterMotor Motor => _controller?.Motor;

        /// <summary>Current world-space velocity applied to the <see cref="Rigidbody2D"/>.</summary>
        public Vector2 Velocity => _controller != null ? _controller.Velocity.ToUnity2D() : Vector2.zero;

        /// <summary>Current HFSM state name for debugging/Inspector inspection.</summary>
        public string CurrentStateName => _controller?.CurrentState?.ToString() ?? "None";

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;

            CharacterConfig config = _configAsset != null
                ? _configAsset.CreateRuntimeCopy()
                : _inlineConfig.Clone();

            _controller = new CoreCharacterController(
                config,
                _blackboardBehaviour != null ? _blackboardBehaviour.Blackboard : null,
                _eventBusBehaviour != null ? _eventBusBehaviour.Bus : null);
        }

        private void FixedUpdate()
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
            System.Numerics.Vector3 velocity = _controller.Tick(Time.fixedDeltaTime, in input, in sensors);
            _controller.FixedTick(Time.fixedDeltaTime);

            Vector2 unityVelocity = velocity.ToUnity2D();
#if UNITY_6000_0_OR_NEWER
            _body.linearVelocity = unityVelocity;
#else
            _body.velocity = unityVelocity;
#endif
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

        private MotorSensors SampleSensors()
        {
            if (_controller.Config.Mode == MovementMode.TopDown2D)
            {
                return MotorSensors.Grounded;
            }

            Vector2 pos = _body.position;
            bool grounded = Physics2D.OverlapBox(pos + _groundCheckOffset, _groundCheckSize, 0f, _groundMask) != null;
            bool ceiling = Physics2D.OverlapBox(pos + _ceilingCheckOffset, _ceilingCheckSize, 0f, _groundMask) != null;

            return new MotorSensors
            {
                IsGrounded = grounded,
                GroundNormal = System.Numerics.Vector3.UnitY,
                IsTouchingCeiling = ceiling
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(pos + (Vector3)_groundCheckOffset, new Vector3(_groundCheckSize.x, _groundCheckSize.y, 0.01f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(pos + (Vector3)_ceilingCheckOffset, new Vector3(_ceilingCheckSize.x, _ceilingCheckSize.y, 0.01f));
        }
    }
}
#endif

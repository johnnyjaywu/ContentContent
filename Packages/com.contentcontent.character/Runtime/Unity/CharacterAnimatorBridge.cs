using UnityEngine;
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Code-free bridge synchronizing a <see cref="CharacterMotor2D"/> or <see cref="CharacterMotor3D"/>
    /// with an <see cref="Animator"/> and optional 2D visual facing flip, safely ignoring missing Animator parameters.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Character Animator Bridge")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    public class CharacterAnimatorBridge : MonoBehaviour
    {
        [Header("Targets (Auto-Detected When Empty)")]
#if CONTENTCONTENT_PHYSICS2D
        [SerializeField] private CharacterMotor2D _motor2D;
#endif
#if CONTENTCONTENT_PHYSICS
        [SerializeField] private CharacterMotor3D _motor3D;
#endif
#if CONTENTCONTENT_ANIMATION
        [SerializeField] private Animator _animator;
#endif

        [Header("Visual Facing (2D Side-Scroller)")]
        [Tooltip("Flips the SpriteRenderer horizontally (or scales X) when the character turns left or right.")]
        [SerializeField] private bool _flipVisualOnTurn = true;

        [Tooltip("Optional SpriteRenderer flipped along X when turning. If empty, Transform scale X is flipped when Flip Target is assigned.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("Optional child transform scaled along X (-1 / +1) when turning (used if SpriteRenderer is null).")]
        [SerializeField] private Transform _flipTarget;

#if CONTENTCONTENT_ANIMATION
        [Header("Animator Parameter Names (Optional)")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _verticalSpeedParam = "VerticalSpeed";
        [SerializeField] private string _isGroundedParam = "IsGrounded";
        [SerializeField] private string _isDashingParam = "IsDashing";
        [SerializeField] private string _isWallSlidingParam = "IsWallSliding";
        [SerializeField] private string _isWallClimbingParam = "IsWallClimbing";
        [SerializeField] private string _jumpTriggerParam = "Jump";
        [SerializeField] private string _dashTriggerParam = "Dash";

        private int _speedHash;
        private int _verticalSpeedHash;
        private int _isGroundedHash;
        private int _isDashingHash;
        private int _isWallSlidingHash;
        private int _isWallClimbingHash;
        private int _jumpTriggerHash;
        private int _dashTriggerHash;
#endif

        private CoreCharacterController _subscribedController;

        private void Reset()
        {
#if CONTENTCONTENT_PHYSICS2D
            if (_motor2D == null) _motor2D = GetComponent<CharacterMotor2D>();
#endif
#if CONTENTCONTENT_PHYSICS
            if (_motor3D == null) _motor3D = GetComponent<CharacterMotor3D>();
#endif
#if CONTENTCONTENT_ANIMATION
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
#endif
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
#if CONTENTCONTENT_PHYSICS2D
            if (_motor2D == null) _motor2D = GetComponent<CharacterMotor2D>();
#endif
#if CONTENTCONTENT_PHYSICS
            if (_motor3D == null) _motor3D = GetComponent<CharacterMotor3D>();
#endif
#if CONTENTCONTENT_ANIMATION
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            CacheAnimatorParameters();
#endif
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            TrySubscribeMotor();
        }

        private void OnDisable()
        {
            UnsubscribeMotor();
        }

        private void LateUpdate()
        {
            CoreCharacterController controller = ResolveController();
            if (controller == null)
            {
                return;
            }

            if (_subscribedController != controller)
            {
                TrySubscribeMotor();
            }

            CharacterMotor motor = controller.Motor;

#if CONTENTCONTENT_ANIMATION
            if (_animator != null)
            {
                if (_speedHash != 0) _animator.SetFloat(_speedHash, motor.PlanarSpeed);
                if (_verticalSpeedHash != 0) _animator.SetFloat(_verticalSpeedHash, motor.VerticalSpeed);
                if (_isGroundedHash != 0) _animator.SetBool(_isGroundedHash, motor.IsGrounded);
                if (_isDashingHash != 0) _animator.SetBool(_isDashingHash, motor.IsDashing);
                if (_isWallSlidingHash != 0) _animator.SetBool(_isWallSlidingHash, motor.IsWallSliding);
                if (_isWallClimbingHash != 0) _animator.SetBool(_isWallClimbingHash, motor.IsWallClimbing);
            }
#endif

            if (_flipVisualOnTurn && controller.Config.Mode == MovementMode.SideScroller2D)
            {
                float fx = motor.FacingDirection.X;
                if (Mathf.Abs(fx) > 0.01f)
                {
                    bool facingLeft = fx < 0f;
                    if (_spriteRenderer != null)
                    {
                        _spriteRenderer.flipX = facingLeft;
                    }
                    else if (_flipTarget != null)
                    {
                        Vector3 scale = _flipTarget.localScale;
                        float absX = Mathf.Abs(scale.x);
                        scale.x = facingLeft ? -absX : absX;
                        _flipTarget.localScale = scale;
                    }
                }
            }
        }

        private CoreCharacterController ResolveController()
        {
#if CONTENTCONTENT_PHYSICS2D
            if (_motor2D != null && _motor2D.Controller != null)
            {
                return _motor2D.Controller;
            }
#endif
#if CONTENTCONTENT_PHYSICS
            if (_motor3D != null && _motor3D.Controller != null)
            {
                return _motor3D.Controller;
            }
#endif
            return null;
        }

        private void TrySubscribeMotor()
        {
            CoreCharacterController controller = ResolveController();
            if (controller == null || controller == _subscribedController)
            {
                return;
            }

            UnsubscribeMotor();
            _subscribedController = controller;
            _subscribedController.Motor.OnJumped += HandleJumped;
            _subscribedController.Motor.OnDashed += HandleDashed;
        }

        private void UnsubscribeMotor()
        {
            if (_subscribedController != null)
            {
                _subscribedController.Motor.OnJumped -= HandleJumped;
                _subscribedController.Motor.OnDashed -= HandleDashed;
                _subscribedController = null;
            }
        }

        private void HandleJumped(CharacterJumpedEvent _)
        {
#if CONTENTCONTENT_ANIMATION
            if (_animator != null && _jumpTriggerHash != 0)
            {
                _animator.SetTrigger(_jumpTriggerHash);
            }
#endif
        }

        private void HandleDashed(CharacterDashedEvent _)
        {
#if CONTENTCONTENT_ANIMATION
            if (_animator != null && _dashTriggerHash != 0)
            {
                _animator.SetTrigger(_dashTriggerHash);
            }
#endif
        }

#if CONTENTCONTENT_ANIMATION
        private void CacheAnimatorParameters()
        {
            _speedHash = 0;
            _verticalSpeedHash = 0;
            _isGroundedHash = 0;
            _isDashingHash = 0;
            _isWallSlidingHash = 0;
            _isWallClimbingHash = 0;
            _jumpTriggerHash = 0;
            _dashTriggerHash = 0;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = parameters[i].name;
                if (paramName == _speedParam) _speedHash = Animator.StringToHash(_speedParam);
                else if (paramName == _verticalSpeedParam) _verticalSpeedHash = Animator.StringToHash(_verticalSpeedParam);
                else if (paramName == _isGroundedParam) _isGroundedHash = Animator.StringToHash(_isGroundedParam);
                else if (paramName == _isDashingParam) _isDashingHash = Animator.StringToHash(_isDashingParam);
                else if (paramName == _isWallSlidingParam) _isWallSlidingHash = Animator.StringToHash(_isWallSlidingParam);
                else if (paramName == _isWallClimbingParam) _isWallClimbingHash = Animator.StringToHash(_isWallClimbingParam);
                else if (paramName == _jumpTriggerParam) _jumpTriggerHash = Animator.StringToHash(_jumpTriggerParam);
                else if (paramName == _dashTriggerParam) _dashTriggerHash = Animator.StringToHash(_dashTriggerParam);
            }
        }
#endif
    }
}

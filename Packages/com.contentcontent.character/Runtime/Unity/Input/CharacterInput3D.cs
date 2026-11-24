#if CONTENTCONTENT_PHYSICS
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.Character.Unity.Input
{
    /// <summary>
    /// Code-free Unity Input System adapter for <see cref="CharacterMotor3D"/> with automatic
    /// camera-relative XZ movement. Works out of the box with default Keyboard/Gamepad bindings
    /// or custom <see cref="InputActionReference"/> slots.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Character Input 3D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    [RequireComponent(typeof(CharacterMotor3D))]
    public class CharacterInput3D : MonoBehaviour
    {
        [Serializable]
        public class ActionInputBinding
        {
            [Tooltip("Action ID matching a CharacterActionAsset registered on the motor.")]
            public string actionId = "Attack";

            [Tooltip("Optional InputActionReference from your .inputactions asset.")]
            public InputActionReference inputAction;

            [Tooltip("Fallback keyboard key used when Input Action is unassigned and default bindings are enabled.")]
            public Key fallbackKey = Key.F;

            [Tooltip("Input buffer window in seconds if the action is pressed slightly before cooldown/locks clear.")]
            public float bufferWindow = 0.15f;
        }

        [Header("Targets (Auto-Detected When Empty)")]
        [SerializeField] private CharacterMotor3D _motor;

        [Tooltip("Optional reference camera for camera-relative XZ movement. Defaults to Camera.main.")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Input Action References (Optional)")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _dashAction;

        [Header("Instant-Play Defaults")]
        [Tooltip("When enabled, any unassigned InputActionReference automatically uses built-in Keyboard and Gamepad controls.")]
        [SerializeField] private bool _useDefaultBindingsWhenUnassigned = true;

        [Header("Custom Action Bindings")]
        [SerializeField] private List<ActionInputBinding> _actionBindings = new List<ActionInputBinding>();

        private void Reset()
        {
            _motor = GetComponent<CharacterMotor3D>();
            if (UnityEngine.Camera.main != null)
            {
                _cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<CharacterMotor3D>();
            }
        }

        private void OnEnable()
        {
            _moveAction?.action?.Enable();
            _jumpAction?.action?.Enable();
            _dashAction?.action?.Enable();

            for (int i = 0; i < _actionBindings.Count; i++)
            {
                _actionBindings[i]?.inputAction?.action?.Enable();
            }
        }

        private void OnDisable()
        {
            _moveAction?.action?.Disable();
            _jumpAction?.action?.Disable();
            _dashAction?.action?.Disable();

            for (int i = 0; i < _actionBindings.Count; i++)
            {
                _actionBindings[i]?.inputAction?.action?.Disable();
            }
        }

        private void Update()
        {
            if (_motor == null)
            {
                return;
            }

            Vector2 stick = ReadMove();
            Vector3 worldMove = ToCameraRelativeXZ(stick);
            _motor.SetMoveInput(worldMove);

            if (ReadJumpPressed())
            {
                _motor.PressJump();
            }

            if (ReadJumpReleased())
            {
                _motor.ReleaseJump();
            }

            if (ReadDashPressed())
            {
                _motor.PressDash();
            }

            for (int i = 0; i < _actionBindings.Count; i++)
            {
                ActionInputBinding binding = _actionBindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.actionId))
                {
                    continue;
                }

                if (ReadActionBindingPressed(binding))
                {
                    _motor.TriggerAction(binding.actionId.Trim(), Mathf.Max(0f, binding.bufferWindow));
                }
            }
        }

        private Vector3 ToCameraRelativeXZ(Vector2 stick)
        {
            if (stick.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Transform cam = _cameraTransform != null
                ? _cameraTransform
                : (UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null);

            if (cam == null)
            {
                return new Vector3(stick.x, 0f, stick.y);
            }

            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }
            else
            {
                forward = Vector3.forward;
            }

            Vector3 right = cam.right;
            right.y = 0f;
            if (right.sqrMagnitude > 0.0001f)
            {
                right.Normalize();
            }
            else
            {
                right = Vector3.right;
            }

            return right * stick.x + forward * stick.y;
        }

        private Vector2 ReadMove()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                return _moveAction.action.ReadValue<Vector2>();
            }

            if (!_useDefaultBindingsWhenUnassigned)
            {
                return Vector2.zero;
            }

            Vector2 move = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null && move.sqrMagnitude < 0.01f)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                Vector2 dpad = pad.dpad.ReadValue();
                move = stick.sqrMagnitude >= dpad.sqrMagnitude ? stick : dpad;
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        private bool ReadJumpPressed()
        {
            if (_jumpAction != null && _jumpAction.action != null)
            {
                return _jumpAction.action.WasPressedThisFrame();
            }

            if (!_useDefaultBindingsWhenUnassigned)
            {
                return false;
            }

            bool pressed = false;
            if (Keyboard.current != null) pressed |= Keyboard.current.spaceKey.wasPressedThisFrame;
            if (Gamepad.current != null) pressed |= Gamepad.current.buttonSouth.wasPressedThisFrame;
            return pressed;
        }

        private bool ReadJumpReleased()
        {
            if (_jumpAction != null && _jumpAction.action != null)
            {
                return _jumpAction.action.WasReleasedThisFrame();
            }

            if (!_useDefaultBindingsWhenUnassigned)
            {
                return false;
            }

            bool released = false;
            if (Keyboard.current != null) released |= Keyboard.current.spaceKey.wasReleasedThisFrame;
            if (Gamepad.current != null) released |= Gamepad.current.buttonSouth.wasReleasedThisFrame;
            return released;
        }

        private bool ReadDashPressed()
        {
            if (_dashAction != null && _dashAction.action != null)
            {
                return _dashAction.action.WasPressedThisFrame();
            }

            if (!_useDefaultBindingsWhenUnassigned)
            {
                return false;
            }

            bool pressed = false;
            if (Keyboard.current != null) pressed |= Keyboard.current.leftShiftKey.wasPressedThisFrame;
            if (Gamepad.current != null) pressed |= Gamepad.current.buttonEast.wasPressedThisFrame;
            return pressed;
        }

        private bool ReadActionBindingPressed(ActionInputBinding binding)
        {
            if (binding.inputAction != null && binding.inputAction.action != null)
            {
                return binding.inputAction.action.WasPressedThisFrame();
            }

            if (!_useDefaultBindingsWhenUnassigned || binding.fallbackKey == Key.None)
            {
                return false;
            }

            Keyboard kb = Keyboard.current;
            return kb != null && kb[binding.fallbackKey] != null && kb[binding.fallbackKey].wasPressedThisFrame;
        }
    }
}
#endif

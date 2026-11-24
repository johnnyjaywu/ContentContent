#if CONTENTCONTENT_PHYSICS2D
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.Character.Unity.Input
{
    /// <summary>
    /// Code-free Unity Input System adapter for <see cref="CharacterMotor2D"/>.
    /// Assign your own <see cref="InputActionReference"/>s or leave them empty to play immediately
    /// using built-in keyboard (WASD/Arrows, Space, Shift, E) and gamepad defaults.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Character Input 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    [RequireComponent(typeof(CharacterMotor2D))]
    public class CharacterInput2D : MonoBehaviour
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

        [Header("Target Motor (Auto-Detected)")]
        [SerializeField] private CharacterMotor2D _motor;

        [Header("Input Action References (Optional)")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _dashAction;
        [SerializeField] private InputActionReference _grabAction;
        [SerializeField] private InputActionReference _dropThroughAction;

        [Header("Behavior & Instant-Play Defaults")]
        [Tooltip("When enabled, any unassigned InputActionReference automatically uses built-in Keyboard and Gamepad controls.")]
        [SerializeField] private bool _useDefaultBindingsWhenUnassigned = true;

        [Tooltip("Pressing Jump while holding Down on a one-way platform drops through instead of jumping.")]
        [SerializeField] private bool _allowDownJumpDropThrough = true;

        [Header("Custom Action Bindings")]
        [SerializeField] private List<ActionInputBinding> _actionBindings = new List<ActionInputBinding>();

        private void Reset()
        {
            _motor = GetComponent<CharacterMotor2D>();
        }

        private void Awake()
        {
            if (_motor == null)
            {
                _motor = GetComponent<CharacterMotor2D>();
            }
        }

        private void OnEnable()
        {
            _moveAction?.action?.Enable();
            _jumpAction?.action?.Enable();
            _dashAction?.action?.Enable();
            _grabAction?.action?.Enable();
            _dropThroughAction?.action?.Enable();

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
            _grabAction?.action?.Disable();
            _dropThroughAction?.action?.Disable();

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

            Vector2 move = ReadMove();
            _motor.SetMoveInput(move);

            bool dropPressed = ReadDropThroughPressed();
            if (dropPressed)
            {
                _motor.PressDropThrough();
            }

            if (ReadJumpPressed())
            {
                bool onOneWay = _motor.Motor != null &&
                                _motor.Motor.Sensors.IsGrounded &&
                                _motor.Motor.Sensors.IsOnOneWayPlatform;

                if (_allowDownJumpDropThrough && onOneWay && move.y < -0.5f)
                {
                    _motor.PressDropThrough();
                }
                else
                {
                    _motor.PressJump();
                }
            }

            if (ReadJumpReleased())
            {
                _motor.ReleaseJump();
            }

            if (ReadDashPressed())
            {
                _motor.PressDash();
            }

            _motor.SetGrabHeld(ReadGrabHeld());

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

        private bool ReadGrabHeld()
        {
            if (_grabAction != null && _grabAction.action != null)
            {
                return _grabAction.action.IsPressed();
            }

            if (!_useDefaultBindingsWhenUnassigned)
            {
                return false;
            }

            bool held = false;
            if (Keyboard.current != null)
            {
                held |= Keyboard.current.eKey.isPressed || Keyboard.current.leftCtrlKey.isPressed;
            }

            if (Gamepad.current != null)
            {
                held |= Gamepad.current.leftTrigger.isPressed || Gamepad.current.leftShoulder.isPressed;
            }

            return held;
        }

        private bool ReadDropThroughPressed()
        {
            if (_dropThroughAction != null && _dropThroughAction.action != null)
            {
                return _dropThroughAction.action.WasPressedThisFrame();
            }

            return false;
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

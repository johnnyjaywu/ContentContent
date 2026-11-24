using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.Interaction.Unity.Input
{
    /// <summary>
    /// Code-free Input System adapter driving <see cref="InteractorBehaviour"/>.
    /// Assign an <see cref="InputActionReference"/> or leave empty for default E / Gamepad South bindings.
    /// </summary>
    [AddComponentMenu("ContentContent/Interaction/Interaction Input")]
    [RequireComponent(typeof(InteractorBehaviour))]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/interaction/quickstart")]
    public class InteractionInput : MonoBehaviour
    {
        [Tooltip("Optional Input System action reference. When omitted, creates a default '<Keyboard>/e' and '<Gamepad>/buttonSouth' action.")]
        [SerializeField] private InputActionReference _interactAction;

        private InteractorBehaviour _interactor;
        private InputAction _runtimeFallbackAction;
        private InputAction _boundAction;

        private void Reset()
        {
            _interactor = GetComponent<InteractorBehaviour>();
        }

        private void Awake()
        {
            _interactor = GetComponent<InteractorBehaviour>();
        }

        private void OnEnable()
        {
            if (_interactor == null)
                _interactor = GetComponent<InteractorBehaviour>();

            _boundAction = _interactAction != null ? _interactAction.action : GetOrCreateFallbackAction();
            if (_boundAction != null)
            {
                _boundAction.started += HandleStarted;
                _boundAction.canceled += HandleCanceled;
                _boundAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_boundAction != null)
            {
                _boundAction.started -= HandleStarted;
                _boundAction.canceled -= HandleCanceled;
                if (_boundAction == _runtimeFallbackAction)
                    _boundAction.Disable();
                _boundAction = null;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeFallbackAction != null)
            {
                _runtimeFallbackAction.Dispose();
                _runtimeFallbackAction = null;
            }
        }

        private InputAction GetOrCreateFallbackAction()
        {
            if (_runtimeFallbackAction == null)
            {
                _runtimeFallbackAction = new InputAction("Interact", InputActionType.Button);
                _runtimeFallbackAction.AddBinding("<Keyboard>/e");
                _runtimeFallbackAction.AddBinding("<Gamepad>/buttonSouth");
            }
            return _runtimeFallbackAction;
        }

        private void HandleStarted(InputAction.CallbackContext _)
        {
            if (_interactor != null)
                _interactor.BeginInteract();
        }

        private void HandleCanceled(InputAction.CallbackContext _)
        {
            if (_interactor != null)
                _interactor.CancelInteract();
        }
    }
}

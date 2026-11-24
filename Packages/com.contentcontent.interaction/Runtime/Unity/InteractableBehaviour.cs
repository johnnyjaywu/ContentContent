using UnityEngine;
using UnityEngine.Events;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Interaction.Unity
{
    /// <summary>
    /// Code-free <see cref="IInteractable"/> component supporting action verbs, hold durations,
    /// priority scoring, single-use latches, cooldowns, lock states, and Inspector UnityEvents.
    /// </summary>
    [AddComponentMenu("ContentContent/Interaction/Interactable")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/interaction/quickstart")]
    public class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [Header("Prompt & Targeting")]
        [Tooltip("Optional identifier passed in OnInteracted events. Defaults to GameObject name when empty.")]
        [SerializeField] private string _interactableId = string.Empty;

        [Tooltip("Action verb shown in UI prompts (e.g., Open, Talk, Pick Up, Pull).")]
        [SerializeField] private string _actionVerb = "Interact";

        [Tooltip("Display name of the object shown after the action verb.")]
        [SerializeField] private string _displayName = "Object";

        [Tooltip("Hold duration in seconds (0 = instant tap activation).")]
        [SerializeField, Min(0f)] private float _holdDuration;

        [Tooltip("Higher priority wins when multiple interactables overlap nearby.")]
        [SerializeField] private int _priority;

        [Tooltip("Optional custom max range (0 uses the Interactor's default range).")]
        [SerializeField, Min(0f)] private float _maxDistanceOverride;

        [Tooltip("Local world-space offset for interaction distance and gizmo point.")]
        [SerializeField] private Vector3 _interactionOffset;

        [Header("Usage & Locks")]
        [Tooltip("Minimum seconds between repeated interactions.")]
        [SerializeField, Min(0f)] private float _cooldownSeconds;

        [Tooltip("Allows interacting only once until ResetInteractable() is called.")]
        [SerializeField] private bool _singleUse;

        [Tooltip("Blocks interaction and shows Locked Reason on the prompt.")]
        [SerializeField] private bool _locked;

        [Tooltip("Reason displayed when Locked is enabled.")]
        [SerializeField] private string _lockedReason = "Locked";

        [Header("Events")]
        [SerializeField] private UnityEvent _onFocused = new UnityEvent();
        [SerializeField] private UnityEvent _onUnfocused = new UnityEvent();
        [SerializeField] private UnityEvent _onInteracted = new UnityEvent();
        [SerializeField] private UnityEvent<string> _onInteractionBlocked = new UnityEvent<string>();

        private bool _hasUsed;
        private float _cooldownRemaining;

        public string InteractableId => string.IsNullOrWhiteSpace(_interactableId) ? name : _interactableId.Trim();
        public NumVector3 WorldPosition
        {
            get
            {
                Vector3 p = transform.TransformPoint(_interactionOffset);
                return new NumVector3(p.x, p.y, p.z);
            }
        }
        public int Priority => _priority;
        public float MaxDistanceOverride => _maxDistanceOverride;
        public float HoldDuration => Mathf.Max(0f, _holdDuration);
        public bool IsLocked => _locked;
        public bool HasUsed => _hasUsed;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);

        public UnityEvent OnFocused => _onFocused ?? (_onFocused = new UnityEvent());
        public UnityEvent OnUnfocused => _onUnfocused ?? (_onUnfocused = new UnityEvent());
        public UnityEvent OnInteracted => _onInteracted ?? (_onInteracted = new UnityEvent());
        public UnityEvent<string> OnInteractionBlocked => _onInteractionBlocked ?? (_onInteractionBlocked = new UnityEvent<string>());

        private void OnValidate()
        {
            _holdDuration = Mathf.Max(0f, _holdDuration);
            _maxDistanceOverride = Mathf.Max(0f, _maxDistanceOverride);
            _cooldownSeconds = Mathf.Max(0f, _cooldownSeconds);
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);
            }
        }

        public InteractionPrompt GetPrompt(in InteractionContext context)
        {
            bool canInteract = true;
            string reason = string.Empty;

            if (_singleUse && _hasUsed)
            {
                canInteract = false;
                reason = "Already used";
            }
            else if (_locked)
            {
                canInteract = false;
                reason = string.IsNullOrEmpty(_lockedReason) ? "Locked" : _lockedReason;
            }
            else if (_cooldownRemaining > 0f)
            {
                canInteract = false;
                reason = $"Wait {_cooldownRemaining:0.0}s";
            }

            return new InteractionPrompt(
                InteractableId,
                _actionVerb,
                _displayName,
                canInteract,
                reason,
                HoldDuration);
        }

        public InteractionResult TryInteract(in InteractionContext context)
        {
            InteractionPrompt prompt = GetPrompt(in context);
            if (!prompt.CanInteract)
            {
                _onInteractionBlocked?.Invoke(prompt.BlockedReason);
                return InteractionResult.Blocked(prompt.BlockedReason);
            }

            if (_singleUse)
                _hasUsed = true;

            if (_cooldownSeconds > 0f)
                _cooldownRemaining = _cooldownSeconds;

            _onInteracted?.Invoke();
            return InteractionResult.Success(InteractableId);
        }

        internal void NotifyFocusChanged(bool focused)
        {
            if (focused)
                _onFocused?.Invoke();
            else
                _onUnfocused?.Invoke();
        }

        public void SetLocked(bool locked, string reason = null)
        {
            _locked = locked;
            if (reason != null)
                _lockedReason = reason;
        }

        public void Lock() => _locked = true;
        public void Unlock() => _locked = false;

        public void ResetInteractable()
        {
            _hasUsed = false;
            _cooldownRemaining = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _locked ? new Color(1f, 0.35f, 0.35f, 0.8f) : new Color(0.25f, 0.95f, 0.55f, 0.8f);
            Gizmos.DrawWireSphere(transform.TransformPoint(_interactionOffset), 0.25f);
        }
    }
}

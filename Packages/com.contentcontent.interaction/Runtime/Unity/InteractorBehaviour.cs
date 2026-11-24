using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Interaction.Unity
{
    public enum InteractionDetectionMode
    {
        Physics2D = 0,
        Physics3D = 1
    }

    /// <summary>
    /// Scans nearby Physics2D or Physics3D interactables, selects the best focus target
    /// via <see cref="InteractionSelector"/>, and drives prompt and hold UnityEvents.
    /// </summary>
    [AddComponentMenu("ContentContent/Interaction/Interactor")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/interaction/quickstart")]
    public class InteractorBehaviour : MonoBehaviour
    {
        private const int MaxCandidates = 32;

        [Header("Detection")]
        [Tooltip("Physics2D uses OverlapCircle; Physics3D uses OverlapSphereNonAlloc.")]
#if CONTENTCONTENT_PHYSICS2D && !CONTENTCONTENT_PHYSICS
        [SerializeField] private InteractionDetectionMode _detectionMode = InteractionDetectionMode.Physics2D;
#else
        [SerializeField] private InteractionDetectionMode _detectionMode = InteractionDetectionMode.Physics3D;
#endif

        [Tooltip("Layers scanned for IInteractable colliders.")]
        [SerializeField] private LayerMask _interactableMask = ~0;

        [Tooltip("Optional ray/sensor origin override (defaults to this Transform).")]
        [SerializeField] private Transform _origin;

        [Tooltip("Maximum detection radius in world units.")]
        [SerializeField, Min(0.1f)] private float _maxDistance = 2.5f;

        [Header("Focus Scoring")]
        [SerializeField, Min(0f)] private float _distanceWeight = 1f;
        [SerializeField, Min(0f)] private float _facingWeight = 0.75f;
        [SerializeField, Min(0f)] private float _priorityWeight = 0.5f;

        [Tooltip("When enabled, obstacles on the same mask block interaction if no direct line of sight exists.")]
        [SerializeField] private bool _requireLineOfSight;

        [Tooltip("Include locked or on-cooldown interactables in focus so players see why they cannot interact.")]
        [SerializeField] private bool _includeBlockedInFocus = true;

        [Tooltip("Layer mask used when Require Line Of Sight is enabled (walls/ground).")]
        [SerializeField] private LayerMask _occlusionMask;

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onPromptChanged = new UnityEvent<string>();
        [SerializeField] private UnityEvent _onPromptCleared = new UnityEvent();
        [SerializeField] private UnityEvent<float> _onHoldProgress = new UnityEvent<float>();
        [SerializeField] private UnityEvent<string> _onInteracted = new UnityEvent<string>();

        private readonly List<IInteractable> _candidates = new List<IInteractable>(MaxCandidates);
#if CONTENTCONTENT_PHYSICS2D
        private readonly List<Collider2D> _hits2D = new List<Collider2D>(MaxCandidates);
#endif
#if CONTENTCONTENT_PHYSICS
        private readonly Collider[] _hits3D = new Collider[MaxCandidates];
#endif

        private InteractionSelector _selector;
        private InteractionConfig _config;
        private InteractableBehaviour _previousFocusedBehaviour;

        public InteractionSelector Selector
        {
            get
            {
                EnsureInitialized();
                return _selector;
            }
        }

        public LayerMask InteractableMask => _interactableMask;
        public InteractionDetectionMode DetectionMode
        {
            get => _detectionMode;
            set => _detectionMode = value;
        }
        public float MaxDistance => Mathf.Max(0.1f, _maxDistance);

        public UnityEvent<string> OnPromptChanged => _onPromptChanged ?? (_onPromptChanged = new UnityEvent<string>());
        public UnityEvent OnPromptCleared => _onPromptCleared ?? (_onPromptCleared = new UnityEvent());
        public UnityEvent<float> OnHoldProgress => _onHoldProgress ?? (_onHoldProgress = new UnityEvent<float>());
        public UnityEvent<string> OnInteracted => _onInteracted ?? (_onInteracted = new UnityEvent<string>());

        private void Reset()
        {
            _interactableMask = ~0;
#if CONTENTCONTENT_PHYSICS2D && !CONTENTCONTENT_PHYSICS
            _detectionMode = InteractionDetectionMode.Physics2D;
#endif
        }

        private void OnValidate()
        {
            _maxDistance = Mathf.Max(0.1f, _maxDistance);
            _distanceWeight = Mathf.Max(0f, _distanceWeight);
            _facingWeight = Mathf.Max(0f, _facingWeight);
            _priorityWeight = Mathf.Max(0f, _priorityWeight);
            SyncConfig();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            RefreshFocusNow();
            if (_selector.IsHolding)
            {
                InteractionContext ctx = BuildContext();
                _selector.TickHold(Time.deltaTime, in ctx);
            }
        }

        private void OnDisable()
        {
            if (_selector != null)
            {
                _selector.ClearFocus();
            }
        }

        private void OnDestroy()
        {
            if (_selector == null)
                return;

            _selector.OnFocusChanged -= HandleFocusChanged;
            _selector.OnHoldProgressChanged -= HandleHoldProgressChanged;
            _selector.OnInteractionCompleted -= HandleInteractionCompleted;
        }

        public void RefreshFocusNow()
        {
            EnsureInitialized();
            SyncConfig();
            CollectCandidates();
            InteractionContext ctx = BuildContext();
            _selector.UpdateFocus(in ctx, _candidates, _requireLineOfSight ? CheckLineOfSight : (System.Func<NumVector3, NumVector3, bool>)null);
        }

        public bool BeginInteract()
        {
            RefreshFocusNow();
            InteractionContext ctx = BuildContext();
            return _selector.BeginInteract(in ctx);
        }

        public void CancelInteract()
        {
            EnsureInitialized();
            _selector.CancelHold();
        }

        public InteractionResult TryInteractNow()
        {
            RefreshFocusNow();
            InteractionContext ctx = BuildContext();
            return _selector.TryInteractImmediate(in ctx);
        }

        private void EnsureInitialized()
        {
            if (_selector != null)
                return;

            _config = new InteractionConfig();
            SyncConfig();
            _selector = new InteractionSelector(_config);
            _selector.OnFocusChanged += HandleFocusChanged;
            _selector.OnHoldProgressChanged += HandleHoldProgressChanged;
            _selector.OnInteractionCompleted += HandleInteractionCompleted;
        }

        private void SyncConfig()
        {
            if (_config == null)
                return;

            _config.MaxDistance = Mathf.Max(0.1f, _maxDistance);
            _config.DistanceWeight = Mathf.Max(0f, _distanceWeight);
            _config.FacingWeight = Mathf.Max(0f, _facingWeight);
            _config.PriorityWeight = Mathf.Max(0f, _priorityWeight);
            _config.RequireLineOfSight = _requireLineOfSight;
            _config.IncludeBlockedInFocus = _includeBlockedInFocus;
            _config.Sanitize();
        }

        private InteractionContext BuildContext()
        {
            Transform t = _origin != null ? _origin : transform;
            Vector3 p = t.position;
            Vector3 f = _detectionMode == InteractionDetectionMode.Physics2D ? t.right : t.forward;
            return new InteractionContext(
                new NumVector3(p.x, p.y, p.z),
                new NumVector3(f.x, f.y, f.z),
                this);
        }

        private void CollectCandidates()
        {
            _candidates.Clear();
            Vector3 origin = _origin != null ? _origin.position : transform.position;
            float radius = Mathf.Max(0.1f, _maxDistance);

            if (_detectionMode == InteractionDetectionMode.Physics2D)
            {
#if CONTENTCONTENT_PHYSICS2D
                var filter = new ContactFilter2D();
                filter.SetLayerMask(_interactableMask);
                filter.useTriggers = true;
                int count = Physics2D.OverlapCircle(new Vector2(origin.x, origin.y), radius, filter, _hits2D);
                for (int i = 0; i < count; i++)
                {
                    Collider2D col = _hits2D[i];
                    if (col == null)
                        continue;

                    IInteractable interactable = col.GetComponentInParent<IInteractable>();
                    if (interactable != null && !_candidates.Contains(interactable))
                        _candidates.Add(interactable);
                }
#endif
                return;
            }

#if CONTENTCONTENT_PHYSICS
            int count3D = Physics.OverlapSphereNonAlloc(origin, radius, _hits3D, _interactableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count3D; i++)
            {
                Collider col = _hits3D[i];
                if (col == null)
                    continue;

                IInteractable interactable = col.GetComponentInParent<IInteractable>();
                if (interactable != null && !_candidates.Contains(interactable))
                    _candidates.Add(interactable);
            }
#endif
        }

        private bool CheckLineOfSight(NumVector3 from, NumVector3 to)
        {
            Vector3 a = new Vector3(from.X, from.Y, from.Z);
            Vector3 b = new Vector3(to.X, to.Y, to.Z);
            Vector3 delta = b - a;
            float dist = delta.magnitude;
            if (dist <= 0.01f)
                return true;

            if (_detectionMode == InteractionDetectionMode.Physics2D)
            {
#if CONTENTCONTENT_PHYSICS2D
                RaycastHit2D hit = Physics2D.Raycast(a, delta / dist, dist, _occlusionMask);
                return hit.collider == null;
#else
                return true;
#endif
            }

#if CONTENTCONTENT_PHYSICS
            return !Physics.Raycast(a, delta / dist, dist, _occlusionMask, QueryTriggerInteraction.Ignore);
#else
            return true;
#endif
        }

        private void HandleFocusChanged(IInteractable prev, IInteractable next, InteractionPrompt prompt)
        {
            var nextBehaviour = next as InteractableBehaviour;
            if (_previousFocusedBehaviour != null && _previousFocusedBehaviour != nextBehaviour)
            {
                _previousFocusedBehaviour.NotifyFocusChanged(false);
            }
            if (nextBehaviour != null && nextBehaviour != _previousFocusedBehaviour)
            {
                nextBehaviour.NotifyFocusChanged(true);
            }
            _previousFocusedBehaviour = nextBehaviour;

            if (next == null)
                _onPromptCleared?.Invoke();
            else
                _onPromptChanged?.Invoke(prompt.FormatDisplayText());
        }

        private void HandleHoldProgressChanged(float progress) => _onHoldProgress?.Invoke(progress);

        private void HandleInteractionCompleted(IInteractable target, InteractionResult result)
        {
            _onInteracted?.Invoke(target.InteractableId);
            RefreshFocusNow();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = _origin != null ? _origin.position : transform.position;
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.4f);
            Gizmos.DrawWireSphere(origin, MaxDistance);

            if (_selector != null && _selector.FocusedInteractable != null)
            {
                NumVector3 p = _selector.FocusedInteractable.WorldPosition;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, new Vector3(p.X, p.Y, p.Z));
            }
        }
    }
}

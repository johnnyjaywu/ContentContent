using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Concrete <see cref="ResettableBehaviour"/> that snapshots initial local Transform
    /// and GameObject active state, restoring them automatically on room re-entry or checkpoint respawn.
    /// </summary>
    [AddComponentMenu("ContentContent/World/Resettable Transform")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class ResettableTransform : ResettableBehaviour
    {
        [Tooltip("Restores local position, rotation, and scale on room reset.")]
        [SerializeField] private bool _resetTransform = true;

        [Tooltip("Restores initial GameObject active state on room reset.")]
        [SerializeField] private bool _resetActiveState = true;

        [Tooltip("Zeroes Rigidbody2D linear/angular velocity on room reset when present.")]
        [SerializeField] private bool _resetRigidbody2DVelocity = true;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private bool _initialActiveSelf;
        private bool _hasSnapshot;

#if CONTENTCONTENT_PHYSICS2D
        private Rigidbody2D _rigidbody2D;
#endif

        protected override void Awake()
        {
            base.Awake();
            CaptureSnapshot();
        }

        /// <summary>
        /// Captures the current transform and active state as the reset baseline.
        /// </summary>
        public void CaptureSnapshot()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialActiveSelf = gameObject.activeSelf;
            _hasSnapshot = true;

#if CONTENTCONTENT_PHYSICS2D
            if (_rigidbody2D == null)
                TryGetComponent(out _rigidbody2D);
#endif
        }

        public override void ResetState()
        {
            if (!_hasSnapshot)
                CaptureSnapshot();

            if (_resetTransform)
            {
                transform.localPosition = _initialLocalPosition;
                transform.localRotation = _initialLocalRotation;
                transform.localScale = _initialLocalScale;
            }

            if (_resetActiveState && gameObject.activeSelf != _initialActiveSelf)
            {
                gameObject.SetActive(_initialActiveSelf);
            }

#if CONTENTCONTENT_PHYSICS2D
            if (_resetRigidbody2DVelocity && _rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }
#endif
        }
    }
}

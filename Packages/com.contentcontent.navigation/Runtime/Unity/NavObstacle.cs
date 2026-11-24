using UnityEngine;

namespace ContentContent.Navigation.Unity
{
    /// <summary>
    /// Drop-in dynamic obstacle component. Automatically updates nearby <see cref="NavSurface"/>
    /// cells when enabled, disabled, or moved in the scene.
    /// </summary>
    [AddComponentMenu("ContentContent/Navigation/Nav Obstacle")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/navigation/quickstart")]
    public class NavObstacle : MonoBehaviour
    {
        private const float MoveThresholdSqr = 0.0025f;

        [Tooltip("Extra expansion margin applied around the obstacle bounds when refreshing NavSurface cells.")]
        [SerializeField, Min(0f)] private float _padding = 0f;

        [Tooltip("Bounds size used if this GameObject has neither a 3D Collider nor a Collider2D.")]
        [SerializeField] private Vector3 _fallbackSize = Vector3.one;

        private Bounds _lastBounds;
        private bool _hasLastBounds;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;

#if CONTENTCONTENT_PHYSICS
        private Collider _collider;
#endif
#if CONTENTCONTENT_PHYSICS2D
        private Collider2D _collider2D;
#endif

        private void OnValidate()
        {
            _padding = Mathf.Max(0f, _padding);
            _fallbackSize = new Vector3(
                Mathf.Max(0.05f, _fallbackSize.x),
                Mathf.Max(0.05f, _fallbackSize.y),
                Mathf.Max(0.05f, _fallbackSize.z));
        }

        private void Awake()
        {
#if CONTENTCONTENT_PHYSICS
            TryGetComponent(out _collider);
#endif
#if CONTENTCONTENT_PHYSICS2D
            TryGetComponent(out _collider2D);
#endif
        }

        private void OnEnable()
        {
            CacheTransform();
            _lastBounds = ComputeBounds();
            _hasLastBounds = true;
            RefreshSurfaces(_lastBounds);
        }

        private void OnDisable()
        {
            if (_hasLastBounds)
            {
                RefreshSurfaces(_lastBounds);
                _hasLastBounds = false;
            }
        }

        private void Update()
        {
            if (!HasTransformChanged())
                return;

            Bounds previous = _lastBounds;
            CacheTransform();
            _lastBounds = ComputeBounds();

            RefreshSurfaces(previous);
            RefreshSurfaces(_lastBounds);
        }

        private bool HasTransformChanged()
        {
            Transform t = transform;
            if ((t.position - _lastPosition).sqrMagnitude > MoveThresholdSqr)
                return true;
            if (Quaternion.Angle(t.rotation, _lastRotation) > 0.5f)
                return true;
            if ((t.lossyScale - _lastScale).sqrMagnitude > MoveThresholdSqr)
                return true;
            return false;
        }

        private void CacheTransform()
        {
            Transform t = transform;
            _lastPosition = t.position;
            _lastRotation = t.rotation;
            _lastScale = t.lossyScale;
        }

        private Bounds ComputeBounds()
        {
            Bounds b;
#if CONTENTCONTENT_PHYSICS
            if (_collider != null)
            {
                b = _collider.bounds;
            }
            else
#endif
#if CONTENTCONTENT_PHYSICS2D
            if (_collider2D != null)
            {
                b = _collider2D.bounds;
            }
            else
#endif
            {
                b = new Bounds(transform.position, Vector3.Scale(_fallbackSize, transform.lossyScale));
            }

            if (_padding > 0f)
                b.Expand(_padding * 2f);

            return b;
        }

        private static void RefreshSurfaces(Bounds worldBounds)
        {
            var surfaces = NavSurface.ActiveSurfaces;
            for (int i = 0; i < surfaces.Count; i++)
            {
                NavSurface surface = surfaces[i];
                if (surface != null && surface.WorldBounds.Intersects(worldBounds))
                {
                    surface.RefreshBounds(worldBounds);
                }
            }
        }
    }
}

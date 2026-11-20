#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Camera.Unity
{
    /// <summary>
    /// 2D trigger volume that enters/exits a <see cref="CameraZoneConfig"/> on a
    /// <see cref="CameraDirectorBehaviour"/> and optionally registers a <see cref="Collider2D"/>
    /// confinement boundary for Cinemachine 3.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CameraZoneTrigger2D : MonoBehaviour
    {
        [Header("Target Director & Filtering")]
        [SerializeField] private CameraDirectorBehaviour _director;
        [SerializeField] private string _targetTag = "Player";

        [Header("Zone Override")]
        [SerializeField] private string _zoneId = "RoomZone";
        [SerializeField] private int _priority = 0;
        [SerializeField] private bool _lockPosition = false;
        [SerializeField] private Transform _lockedAnchor;
        [SerializeField] private Vector3 _lockedPosition;
        [SerializeField] private float _orthographicSizeOverride = 0f;
        [SerializeField] private Vector3 _staticOffset;
        [SerializeField] private bool _allowLookAhead = true;
        [SerializeField] private bool _allowVerticalPeek = true;

        [Header("Confiner Bounds (Optional)")]
        [SerializeField] private string _boundsId = string.Empty;
        [SerializeField] private Collider2D _confinerBounds;

        private bool _isOccupied;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            if (_director == null)
            {
                _director = FindFirstObjectByType<CameraDirectorBehaviour>();
            }

            if (_director != null && _confinerBounds != null)
            {
                _director.RegisterBounds(ResolveBoundsId(), _confinerBounds);
            }
        }

        private void OnDisable()
        {
            if (_isOccupied && _director != null)
            {
                _director.ExitZone(ResolveZoneId());
                _isOccupied = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!MatchesTarget(other) || _director == null)
                return;

            if (_confinerBounds != null)
            {
                _director.RegisterBounds(ResolveBoundsId(), _confinerBounds);
            }

            _isOccupied = true;
            _director.EnterZone(BuildConfig());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!MatchesTarget(other) || _director == null)
                return;

            _isOccupied = false;
            _director.ExitZone(ResolveZoneId());
        }

        private bool MatchesTarget(Collider2D other)
        {
            if (other == null)
                return false;

            return string.IsNullOrEmpty(_targetTag) || other.CompareTag(_targetTag);
        }

        private string ResolveZoneId()
        {
            return string.IsNullOrEmpty(_zoneId) ? gameObject.name : _zoneId;
        }

        private string ResolveBoundsId()
        {
            return !string.IsNullOrEmpty(_boundsId)
                ? _boundsId
                : (_confinerBounds != null ? ResolveZoneId() : string.Empty);
        }

        private CameraZoneConfig BuildConfig()
        {
            Vector3 lockPos = _lockedAnchor != null ? _lockedAnchor.position : _lockedPosition;
            return new CameraZoneConfig
            {
                ZoneId = ResolveZoneId(),
                Priority = _priority,
                LockPosition = _lockPosition,
                LockedPosition = new NumVector3(lockPos.x, lockPos.y, lockPos.z),
                OrthographicSizeOverride = _orthographicSizeOverride,
                StaticOffset = new NumVector3(_staticOffset.x, _staticOffset.y, _staticOffset.z),
                AllowLookAhead = _allowLookAhead,
                AllowVerticalPeek = _allowVerticalPeek,
                BoundsId = ResolveBoundsId()
            };
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = _lockPosition ? Color.magenta : Color.yellow;
                Bounds b = col.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }

            if (_lockPosition)
            {
                Vector3 lockPos = _lockedAnchor != null ? _lockedAnchor.position : _lockedPosition;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lockPos, 0.35f);
            }
        }
    }
}
#endif

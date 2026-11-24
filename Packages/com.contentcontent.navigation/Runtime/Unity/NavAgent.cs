using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Navigation.Unity
{
    /// <summary>
    /// Drop-in navigation agent for 2D (<see cref="NavMode.PlanarXY"/>), ground 2D (<see cref="NavMode.PlanarXZ"/>),
    /// and volumetric 3D (<see cref="NavMode.Volumetric3D"/>). Automatically discovers the scene's
    /// <see cref="NavSurface"/> and moves via Rigidbody2D, Rigidbody, or Transform.
    /// </summary>
    [AddComponentMenu("ContentContent/Navigation/Nav Agent")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/navigation/quickstart")]
    public class NavAgent : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Maximum movement speed in world units per second.")]
        [SerializeField, Min(0.01f)] private float _speed = 5f;

        [Tooltip("Agent stops cleanly once within this distance of its destination.")]
        [SerializeField, Min(0.01f)] private float _stoppingDistance = 0.15f;

        [Tooltip("Automatically rotates Transform toward movement direction.")]
        [SerializeField] private bool _rotateTowardsMovement = true;

        [Tooltip("Minimum seconds between automatic repaths when obstacles update the NavGrid.")]
        [SerializeField, Min(0.05f)] private float _repathInterval = 0.25f;

        [Header("Surface Override (Optional)")]
        [Tooltip("Explicit NavSurface override. Leave empty to auto-discover the nearest active NavSurface.")]
        [SerializeField] private NavSurface _surface;

        [Header("Events")]
        [SerializeField] private UnityEvent _onDestinationReached = new UnityEvent();
        [SerializeField] private UnityEvent _onPathFailed = new UnityEvent();

        private readonly List<Vector3> _waypoints = new List<Vector3>(32);
        private int _waypointIndex;
        private int _lastGridVersion = -1;
        private float _repathCooldown;
        private bool _hasDestination;
        private bool _isStopped;

#if CONTENTCONTENT_PHYSICS
        private Rigidbody _rigidbody;
#endif
#if CONTENTCONTENT_PHYSICS2D
        private Rigidbody2D _rigidbody2D;
#endif

        public NavSurface Surface => _surface;
        public float Speed { get => _speed; set => _speed = Mathf.Max(0.01f, value); }
        public float StoppingDistance { get => _stoppingDistance; set => _stoppingDistance = Mathf.Max(0.01f, value); }
        public Vector3 Destination { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool IsStopped => _isStopped;
        public bool HasPath => _waypoints.Count > 0 && _waypointIndex < _waypoints.Count;

        public float RemainingDistance
        {
            get
            {
                if (!HasPath)
                    return 0f;

                float dist = Vector3.Distance(transform.position, _waypoints[_waypointIndex]);
                for (int i = _waypointIndex; i < _waypoints.Count - 1; i++)
                {
                    dist += Vector3.Distance(_waypoints[i], _waypoints[i + 1]);
                }
                return dist;
            }
        }

        public event Action OnDestinationReached;
        public event Action OnPathFailed;

        public UnityEvent OnDestinationReachedUnityEvent => _onDestinationReached ?? (_onDestinationReached = new UnityEvent());
        public UnityEvent OnPathFailedUnityEvent => _onPathFailed ?? (_onPathFailed = new UnityEvent());

        private void OnValidate()
        {
            _speed = Mathf.Max(0.01f, _speed);
            _stoppingDistance = Mathf.Max(0.01f, _stoppingDistance);
            _repathInterval = Mathf.Max(0.05f, _repathInterval);
        }

        private void Awake()
        {
#if CONTENTCONTENT_PHYSICS
            TryGetComponent(out _rigidbody);
#endif
#if CONTENTCONTENT_PHYSICS2D
            TryGetComponent(out _rigidbody2D);
#endif
        }

        private void OnEnable()
        {
            if (_surface == null)
                _surface = NavSurface.FindSurfaceForPoint(transform.position);
        }

        private void OnDisable()
        {
            if (_surface != null)
                _surface.CancelRequest(this);

            Velocity = Vector3.zero;
        }

        /// <summary>
        /// Explicitly binds this agent to a specific <see cref="NavSurface"/>.
        /// Normally unnecessary because agents auto-discover the containing surface.
        /// </summary>
        public void SetSurface(NavSurface surface)
        {
            if (_surface != null && _surface != surface)
                _surface.CancelRequest(this);

            _surface = surface;
            _lastGridVersion = -1;
        }

        /// <summary>
        /// Requests a path to <paramref name="target"/> and begins moving toward it.
        /// </summary>
        public void SetDestination(Vector3 target)
        {
            if (_surface == null)
                _surface = NavSurface.FindSurfaceForPoint(transform.position);

            if (_surface == null)
            {
                OnPathFailed?.Invoke();
                return;
            }

            bool significantMove = !_hasDestination ||
                                   (target - Destination).sqrMagnitude > (_surface.CellSize * 0.5f) * (_surface.CellSize * 0.5f);

            Destination = target;
            _hasDestination = true;
            _isStopped = false;

            if (significantMove || (!HasPath && _repathCooldown <= 0f))
            {
                QueueRepath();
            }
        }

        /// <summary>
        /// Halts movement and clears the current path.
        /// </summary>
        public void Stop()
        {
            _isStopped = true;
            _hasDestination = false;
            _waypoints.Clear();
            _waypointIndex = 0;
            Velocity = Vector3.zero;

            if (_surface != null)
                _surface.CancelRequest(this);
        }

        private void Update()
        {
            if (_repathCooldown > 0f)
                _repathCooldown -= Time.deltaTime;

            if (_hasDestination && !_isStopped && _surface != null && _surface.Grid != null)
            {
                if (_surface.Grid.Version != _lastGridVersion && _repathCooldown <= 0f)
                {
                    QueueRepath();
                }
            }

            if (!UsesPhysicsDriver())
            {
                StepMovement(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (UsesPhysicsDriver())
            {
                StepMovement(Time.fixedDeltaTime);
            }
        }

        private bool UsesPhysicsDriver()
        {
#if CONTENTCONTENT_PHYSICS2D
            if (_rigidbody2D != null && (_surface == null || _surface.Mode == NavMode.PlanarXY))
                return true;
#endif
#if CONTENTCONTENT_PHYSICS
            if (_rigidbody != null)
                return true;
#endif
            return false;
        }

        private void QueueRepath()
        {
            if (_surface == null)
                return;

            _repathCooldown = _repathInterval;
            _lastGridVersion = _surface.Grid != null ? _surface.Grid.Version : -1;

            _surface.RequestPath(
                this,
                transform.position,
                Destination,
                OnPathComputed);
        }

        private void OnPathComputed(bool found, List<Vector3> worldPath)
        {
            if (_isStopped || !_hasDestination)
                return;

            _waypoints.Clear();
            _waypointIndex = 0;

            if (!found || worldPath.Count == 0)
            {
                Velocity = Vector3.zero;
                OnPathFailed?.Invoke();
                _onPathFailed?.Invoke();
                return;
            }

            for (int i = 0; i < worldPath.Count; i++)
                _waypoints.Add(worldPath[i]);

            // Skip initial waypoint if it is already at our current cell center.
            if (_waypoints.Count > 1 && Vector3.Distance(transform.position, _waypoints[0]) <= _stoppingDistance)
                _waypointIndex = 1;
        }

        private void StepMovement(float dt)
        {
            if (_isStopped || !HasPath || dt <= 0f)
            {
                Velocity = Vector3.zero;
                return;
            }

            Vector3 currentPos = transform.position;
            float maxStep = _speed * dt;
            float arriveRadius = Mathf.Max(_stoppingDistance, maxStep);

            while (_waypointIndex < _waypoints.Count)
            {
                Vector3 target = _waypoints[_waypointIndex];
                Vector3 toTarget = target - currentPos;

                if (_surface != null && _surface.Mode == NavMode.PlanarXY)
                    toTarget.z = 0f;
                else if (_surface != null && _surface.Mode == NavMode.PlanarXZ)
                    toTarget.y = 0f;

                float dist = toTarget.magnitude;
                bool isFinal = _waypointIndex == _waypoints.Count - 1;
                float threshold = isFinal ? _stoppingDistance : arriveRadius;

                if (dist <= threshold)
                {
                    _waypointIndex++;
                    if (_waypointIndex >= _waypoints.Count)
                    {
                        ApplyPosition(currentPos + toTarget);
                        Velocity = Vector3.zero;
                        _waypoints.Clear();
                        _hasDestination = false;
                        OnDestinationReached?.Invoke();
                        _onDestinationReached?.Invoke();
                        return;
                    }
                    continue;
                }

                Vector3 direction = toTarget / dist;
                Vector3 step = direction * Mathf.Min(maxStep, dist);
                Velocity = step / dt;

                ApplyPosition(currentPos + step);
                if (_rotateTowardsMovement)
                    ApplyRotation(direction);
                return;
            }
        }

        private void ApplyPosition(Vector3 nextPosition)
        {
#if CONTENTCONTENT_PHYSICS2D
            if (_rigidbody2D != null && (_surface == null || _surface.Mode == NavMode.PlanarXY))
            {
                _rigidbody2D.MovePosition(new Vector2(nextPosition.x, nextPosition.y));
                return;
            }
#endif
#if CONTENTCONTENT_PHYSICS
            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(nextPosition);
                return;
            }
#endif
            transform.position = nextPosition;
        }

        private void ApplyRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return;

            if (_surface != null && _surface.Mode == NavMode.PlanarXY)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasPath)
                return;

            Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
            Vector3 prev = transform.position;
            for (int i = _waypointIndex; i < _waypoints.Count; i++)
            {
                Gizmos.DrawLine(prev, _waypoints[i]);
                Gizmos.DrawSphere(_waypoints[i], 0.12f);
                prev = _waypoints[i];
            }
        }
    }
}

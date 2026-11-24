using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Navigation.Unity
{
    public enum PatrolMode
    {
        Loop = 0,
        PingPong = 1
    }

    /// <summary>
    /// Moves a sibling <see cref="NavAgent"/> through an ordered list of waypoint Transforms
    /// in either <see cref="PatrolMode.Loop"/> or <see cref="PatrolMode.PingPong"/> order.
    /// </summary>
    [AddComponentMenu("ContentContent/Navigation/Nav Patrol")]
    [RequireComponent(typeof(NavAgent))]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/navigation/quickstart")]
    public class NavPatrol : MonoBehaviour
    {
        [Header("Patrol Route")]
        [Tooltip("Ordered list of Transforms the agent patrols between.")]
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();

        [Tooltip("Loop wraps from last back to first; PingPong reverses direction at each end.")]
        [SerializeField] private PatrolMode _mode = PatrolMode.Loop;

        [Tooltip("Optional pause duration (seconds) after reaching each waypoint.")]
        [SerializeField] private float _waitDuration = 0.5f;

        [Tooltip("Automatically starts moving to the first waypoint when enabled.")]
        [SerializeField] private bool _startOnEnable = true;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> _onWaypointReached = new UnityEvent<int>();

        private NavAgent _agent;
        private int _currentIndex;
        private int _pingPongDirection = 1;
        private float _waitTimer;
        private bool _waiting;
        private bool _subscribed;

        public IList<Transform> Waypoints => _waypoints;

        public PatrolMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public float WaitDuration
        {
            get => _waitDuration;
            set => _waitDuration = Mathf.Max(0f, value);
        }

        public int CurrentWaypointIndex => _currentIndex;
        public UnityEvent<int> OnWaypointReached => _onWaypointReached ?? (_onWaypointReached = new UnityEvent<int>());

        public NavAgent Agent
        {
            get
            {
                if (_agent == null)
                    _agent = GetComponent<NavAgent>();
                return _agent;
            }
        }

        private void Reset()
        {
            _agent = GetComponent<NavAgent>();
        }

        private void OnValidate()
        {
            _waitDuration = Mathf.Max(0f, _waitDuration);
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (Application.isPlaying && _startOnEnable)
            {
                StartPatrol();
            }
        }

        private void OnDisable()
        {
            if (_agent != null && _subscribed)
            {
                _agent.OnDestinationReached -= HandleDestinationReached;
                _subscribed = false;
            }
            _waiting = false;
        }

        private void Update()
        {
            if (!_waiting)
                return;

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceToNextWaypoint();
            }
        }

        /// <summary>Begins patrolling from waypoint index 0.</summary>
        public void StartPatrol()
        {
            EnsureSubscribed();
            _waiting = false;
            _pingPongDirection = 1;
            _currentIndex = FindNextValidIndex(-1, 1);

            if (_currentIndex >= 0)
            {
                Agent.SetDestination(_waypoints[_currentIndex].position);
            }
        }

        /// <summary>Advances immediately to the next valid waypoint according to <see cref="Mode"/>.</summary>
        public void AdvanceToNextWaypoint()
        {
            if (_waypoints == null || _waypoints.Count == 0)
                return;

            int count = _waypoints.Count;
            if (count == 1)
            {
                if (_waypoints[0] != null)
                {
                    _currentIndex = 0;
                    Agent.SetDestination(_waypoints[0].position);
                }
                return;
            }

            int next = ComputeNextIndex(count);
            if (next >= 0 && _waypoints[next] != null)
            {
                _currentIndex = next;
                Agent.SetDestination(_waypoints[next].position);
            }
        }

        private int ComputeNextIndex(int count)
        {
            if (_mode == PatrolMode.Loop)
            {
                return FindNextValidIndex(_currentIndex, 1);
            }

            if (_pingPongDirection >= 0)
            {
                if (_currentIndex >= count - 1)
                {
                    _pingPongDirection = -1;
                    return FindStepValidIndex(_currentIndex, -1);
                }
                int forward = FindStepValidIndex(_currentIndex, 1);
                if (forward >= 0)
                    return forward;

                _pingPongDirection = -1;
                return FindStepValidIndex(_currentIndex, -1);
            }

            if (_currentIndex <= 0)
            {
                _pingPongDirection = 1;
                return FindStepValidIndex(_currentIndex, 1);
            }
            int backward = FindStepValidIndex(_currentIndex, -1);
            if (backward >= 0)
                return backward;

            _pingPongDirection = 1;
            return FindStepValidIndex(_currentIndex, 1);
        }

        private int FindNextValidIndex(int startAfter, int step)
        {
            int count = _waypoints.Count;
            for (int offset = 1; offset <= count; offset++)
            {
                int idx = ((startAfter + offset * step) % count + count) % count;
                if (_waypoints[idx] != null)
                    return idx;
            }
            return -1;
        }

        private int FindStepValidIndex(int startFrom, int step)
        {
            for (int idx = startFrom + step; idx >= 0 && idx < _waypoints.Count; idx += step)
            {
                if (_waypoints[idx] != null)
                    return idx;
            }
            return -1;
        }

        private void HandleDestinationReached()
        {
            _onWaypointReached?.Invoke(_currentIndex);
            if (_waitDuration > 0f)
            {
                _waitTimer = _waitDuration;
                _waiting = true;
            }
            else
            {
                AdvanceToNextWaypoint();
            }
        }

        private void EnsureSubscribed()
        {
            if (_subscribed || Agent == null)
                return;

            Agent.OnDestinationReached += HandleDestinationReached;
            _subscribed = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Count == 0)
                return;

            Gizmos.color = new Color(1f, 0.78f, 0.2f, 0.9f);
            Transform first = null;
            Transform prev = null;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                Transform wp = _waypoints[i];
                if (wp == null)
                    continue;

                Gizmos.DrawSphere(wp.position, 0.16f);
                if (first == null)
                    first = wp;
                if (prev != null)
                    Gizmos.DrawLine(prev.position, wp.position);
                prev = wp;
            }

            if (_mode == PatrolMode.Loop && first != null && prev != null && first != prev)
            {
                Gizmos.DrawLine(prev.position, first.position);
            }
        }
    }
}

using UnityEngine;

namespace ContentContent.Navigation.Unity
{
    /// <summary>
    /// Continuously updates a sibling <see cref="NavAgent"/> destination to pursue a target <see cref="Transform"/>,
    /// repathing only when the target moves past a distance threshold.
    /// </summary>
    [AddComponentMenu("ContentContent/Navigation/Nav Follow Target")]
    [RequireComponent(typeof(NavAgent))]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/navigation/quickstart")]
    public class NavFollowTarget : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Transform to pursue. Assign a player or point of interest.")]
        [SerializeField] private Transform _target;

        [Tooltip("Seconds between distance checks for updating the agent's path.")]
        [SerializeField] private float _updateInterval = 0.2f;

        [Tooltip("Minimum target movement distance before requesting a new path.")]
        [SerializeField] private float _repathDistanceThreshold = 0.35f;

        [Tooltip("Automatically stops the NavAgent when Target becomes null.")]
        [SerializeField] private bool _stopWhenTargetLost = true;

        private NavAgent _agent;
        private float _timer;
        private bool _hasLastTargetPosition;
        private Vector3 _lastTargetPosition;

        public Transform Target
        {
            get => _target;
            set
            {
                if (_target == value)
                    return;

                _target = value;
                _hasLastTargetPosition = false;
                _timer = 0f;
            }
        }

        public float UpdateInterval
        {
            get => _updateInterval;
            set => _updateInterval = Mathf.Max(0.02f, value);
        }

        public float RepathDistanceThreshold
        {
            get => _repathDistanceThreshold;
            set => _repathDistanceThreshold = Mathf.Max(0.05f, value);
        }

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
            _updateInterval = Mathf.Max(0.02f, _updateInterval);
            _repathDistanceThreshold = Mathf.Max(0.05f, _repathDistanceThreshold);
        }

        private void Awake()
        {
            _agent = GetComponent<NavAgent>();
        }

        private void OnDisable()
        {
            _hasLastTargetPosition = false;
            _timer = 0f;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = _updateInterval;
            EvaluateFollow();
        }

        /// <summary>
        /// Immediately checks the current <see cref="Target"/> and updates <see cref="NavAgent"/> if needed.
        /// </summary>
        public void ForceRepath()
        {
            _hasLastTargetPosition = false;
            EvaluateFollow();
        }

        private void EvaluateFollow()
        {
            if (Agent == null)
                return;

            if (_target == null)
            {
                if (_stopWhenTargetLost && _hasLastTargetPosition)
                {
                    _hasLastTargetPosition = false;
                    _agent.Stop();
                }
                return;
            }

            Vector3 targetPos = _target.position;
            float thresholdSqr = _repathDistanceThreshold * _repathDistanceThreshold;
            bool movedEnough = !_hasLastTargetPosition || (targetPos - _lastTargetPosition).sqrMagnitude >= thresholdSqr;

            if (movedEnough || !_agent.HasPath)
            {
                _lastTargetPosition = targetPos;
                _hasLastTargetPosition = true;
                _agent.SetDestination(targetPos);
            }
        }
    }
}

#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;

namespace ContentContent.Character.Unity
{
    /// <summary>
    /// Code-free moving platform component implementing <see cref="ISurfaceVelocityProvider"/>
    /// so <see cref="CharacterMotor2D"/> carries the rider smoothly and inherits jump launch momentum.
    /// Supports either built-in two-point patrol or passive velocity tracking when moved externally.
    /// </summary>
    [AddComponentMenu("ContentContent/Character/Moving Platform 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/character/quickstart")]
    public class MovingPlatform2D : MonoBehaviour, ISurfaceVelocityProvider
    {
        [Header("Built-In Patrol (Optional)")]
        [Tooltip("When enabled, automatically patrols back and forth by Travel Offset. Disable if driven by an Animation or custom script.")]
        [SerializeField] private bool _enablePatrol = true;

        [Tooltip("Local offset from the starting position to the patrol turnaround point.")]
        [SerializeField] private Vector2 _travelOffset = new Vector2(4f, 0f);

        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float _speed = 2.5f;

        [Tooltip("Optional pause duration at each patrol endpoint in seconds.")]
        [SerializeField] private float _pauseDuration = 0.25f;

        private Rigidbody2D _body;
        private Vector2 _startPos;
        private Vector2 _endPos;
        private Vector2 _previousPos;
        private bool _movingForward = true;
        private float _pauseRemaining;

        /// <summary>Current world-space surface velocity read by <see cref="CharacterMotor2D"/>.</summary>
        public Vector2 SurfaceVelocity { get; private set; }

        /// <summary>Configures or updates the built-in patrol endpoints and speed at runtime.</summary>
        public void ConfigurePatrol(Vector2 travelOffset, float speed = 2.5f, float pauseDuration = 0.25f)
        {
            _enablePatrol = true;
            _travelOffset = travelOffset;
            _speed = Mathf.Max(0f, speed);
            _pauseDuration = Mathf.Max(0f, pauseDuration);
            _startPos = _body != null ? _body.position : (Vector2)transform.position;
            _endPos = _startPos + _travelOffset;
            _movingForward = true;
            _pauseRemaining = 0f;
        }

        private void Reset()
        {
            _enablePatrol = true;
            _travelOffset = new Vector2(4f, 0f);
            _speed = 2.5f;
            _pauseDuration = 0.25f;
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0f, _speed);
            _pauseDuration = Mathf.Max(0f, _pauseDuration);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _startPos = _body != null ? _body.position : (Vector2)transform.position;
            _endPos = _startPos + _travelOffset;
            _previousPos = _startPos;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
            {
                return;
            }

            Vector2 currentPos = _body != null ? _body.position : (Vector2)transform.position;

            if (!_enablePatrol || _travelOffset.sqrMagnitude < 0.0001f || _speed <= 0f)
            {
                SurfaceVelocity = (currentPos - _previousPos) / dt;
                _previousPos = currentPos;
                return;
            }

            if (_pauseRemaining > 0f)
            {
                _pauseRemaining = Mathf.Max(0f, _pauseRemaining - dt);
                SurfaceVelocity = Vector2.zero;
                _previousPos = currentPos;
                return;
            }

            Vector2 target = _movingForward ? _endPos : _startPos;
            Vector2 nextPos = Vector2.MoveTowards(currentPos, target, _speed * dt);
            SurfaceVelocity = (nextPos - currentPos) / dt;

            if (_body != null)
            {
                _body.MovePosition(nextPos);
            }
            else
            {
                transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            }

            _previousPos = nextPos;

            if ((nextPos - target).sqrMagnitude <= 0.00001f)
            {
                _movingForward = !_movingForward;
                _pauseRemaining = _pauseDuration;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying
                ? new Vector3(_startPos.x, _startPos.y, transform.position.z)
                : transform.position;
            Vector3 end = origin + (Vector3)_travelOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(origin, 0.15f);
            Gizmos.DrawWireSphere(end, 0.15f);
        }
    }
}
#endif

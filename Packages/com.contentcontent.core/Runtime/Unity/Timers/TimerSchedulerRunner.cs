using UnityEngine;

namespace ContentContent.Core.Timers.Unity
{
    /// <summary>
    /// MonoBehaviour adapter owning and ticking a <see cref="TimerScheduler"/> in Unity's <see cref="Update"/> loop.
    /// </summary>
    [AddComponentMenu("ContentContent/Core/Timer Scheduler Runner")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/core/quickstart")]
    public class TimerSchedulerRunner : MonoBehaviour
    {
        [Tooltip("Uses Time.unscaledDeltaTime so UI or pause timers continue when Time.timeScale is 0.")]
        [SerializeField] private bool _useUnscaledTime;

        [Tooltip("Local speed multiplier applied to all timers on this scheduler.")]
        [SerializeField] private float _timeScale = 1f;

        [Tooltip("Maximum times a repeating timer catches up in a single frame to avoid spiral-of-death hitches.")]
        [SerializeField] private int _maxFiresPerTick = 16;

        private TimerScheduler _scheduler;

        /// <summary>Local time scale applied to this scheduler.</summary>
        public float TimeScale
        {
            get => _timeScale;
            set
            {
                _timeScale = Mathf.Max(0f, value);
                if (_scheduler != null)
                {
                    _scheduler.TimeScale = _timeScale;
                }
            }
        }

        /// <summary>The underlying <see cref="TimerScheduler"/> instance (lazily initialized).</summary>
        public TimerScheduler Scheduler
        {
            get
            {
                if (_scheduler == null)
                {
                    _scheduler = new TimerScheduler(maxFiresPerTick: Mathf.Max(1, _maxFiresPerTick));
                    _scheduler.TimeScale = Mathf.Max(0f, _timeScale);
                }

                return _scheduler;
            }
        }

        private void OnValidate()
        {
            _timeScale = Mathf.Max(0f, _timeScale);
            _maxFiresPerTick = Mathf.Max(1, _maxFiresPerTick);
            if (_scheduler != null)
            {
                _scheduler.TimeScale = _timeScale;
                _scheduler.MaxFiresPerTick = _maxFiresPerTick;
            }
        }

        private void Update()
        {
            if (_scheduler == null)
                return;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _scheduler.Tick(dt);
        }

        private void OnDestroy()
        {
            _scheduler?.CancelAll();
        }
    }
}

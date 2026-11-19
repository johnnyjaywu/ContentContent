using UnityEngine;

namespace ContentContent.Core.Timers.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter that owns and ticks a <see cref="TimerScheduler"/> in Unity's <see cref="Update"/> loop.
    /// </summary>
    public class TimerSchedulerRunner : MonoBehaviour
    {
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField] private int _maxFiresPerTick = 16;

        private TimerScheduler _scheduler;

        /// <summary>The underlying <see cref="TimerScheduler"/> instance (lazily initialized).</summary>
        public TimerScheduler Scheduler
        {
            get
            {
                if (_scheduler == null)
                {
                    _scheduler = new TimerScheduler(maxFiresPerTick: Mathf.Max(1, _maxFiresPerTick));
                }

                return _scheduler;
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

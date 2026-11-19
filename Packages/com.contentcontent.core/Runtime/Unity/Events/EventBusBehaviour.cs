using UnityEngine;

namespace ContentContent.Core.Events.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter that owns an <see cref="EventBus"/> instance scoped to a
    /// GameObject or scene and clears all listeners on destroy.
    /// </summary>
    public class EventBusBehaviour : MonoBehaviour
    {
        [SerializeField] private bool _logExceptions = true;

        private EventBus _bus;

        /// <summary>The underlying <see cref="EventBus"/> instance (lazily initialized).</summary>
        public EventBus Bus
        {
            get
            {
                if (_bus == null)
                {
                    _bus = _logExceptions
                        ? new EventBus(ex => Debug.LogException(ex, this))
                        : new EventBus();
                }

                return _bus;
            }
        }

        private void OnDestroy()
        {
            _bus?.Clear();
        }
    }
}

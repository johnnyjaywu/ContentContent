using UnityEngine;

namespace ContentContent.Core.Events.Unity
{
    /// <summary>
    /// MonoBehaviour adapter owning an instance-based <see cref="EventBus"/> scoped to a
    /// GameObject or scene and clearing all listeners on destroy.
    /// </summary>
    [AddComponentMenu("ContentContent/Core/Event Bus")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/core/quickstart")]
    public class EventBusBehaviour : MonoBehaviour
    {
        [Tooltip("When enabled, exceptions thrown inside event subscribers are logged without stopping other subscribers.")]
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

using UnityEngine;

namespace ContentContent.Core.Blackboards.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter exposing a <see cref="Blackboard"/> instance with optional
    /// parent <see cref="BlackboardBehaviour"/> fallback for shared squad or world data.
    /// </summary>
    public class BlackboardBehaviour : MonoBehaviour
    {
        [SerializeField] private BlackboardBehaviour _parentBlackboard;

        private Blackboard _blackboard;

        /// <summary>The underlying <see cref="Blackboard"/> instance (lazily initialized).</summary>
        public Blackboard Blackboard
        {
            get
            {
                if (_blackboard == null)
                {
                    _blackboard = _parentBlackboard != null
                        ? new Blackboard(_parentBlackboard.Blackboard)
                        : new Blackboard();
                }

                return _blackboard;
            }
        }

        private void OnDestroy()
        {
            _blackboard?.ClearAll();
        }
    }
}

#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D trigger portal that transitions to a target room and spawn point when touched.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomTransitionTrigger2D : MonoBehaviour
    {
        [SerializeField] private WorldManagerBehaviour _worldManager;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private string _destinationRoomId = string.Empty;
        [SerializeField] private string _destinationSpawnPointId = "Default";
        [SerializeField] private bool _resetTargetRoom = true;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnEnable()
        {
            if (_worldManager == null)
                _worldManager = FindFirstObjectByType<WorldManagerBehaviour>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_worldManager == null || other == null)
                return;

            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
                return;

            if (!string.IsNullOrEmpty(_destinationRoomId))
            {
                _worldManager.TransitionToRoom(_destinationRoomId, _destinationSpawnPointId, _resetTargetRoom);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
#endif

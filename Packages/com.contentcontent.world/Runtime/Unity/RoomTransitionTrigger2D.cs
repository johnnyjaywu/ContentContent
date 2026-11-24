#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D trigger portal that transitions to a target room and spawn point when touched.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("ContentContent/World/Room Transition Trigger 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class RoomTransitionTrigger2D : MonoBehaviour
    {
        [Tooltip("Optional WorldManagerBehaviour. Auto-discovered in the scene if omitted.")]
        [SerializeField] private WorldManagerBehaviour _worldManager;

        [Tooltip("Trigger tag filter (empty allows any Collider2D).")]
        [SerializeField] private string _targetTag = "Player";

        [Tooltip("Target room ID entered when the trigger activates.")]
        [SerializeField] private string _destinationRoomId = string.Empty;

        [Tooltip("Target SpawnPoint ID inside Destination Room Id.")]
        [SerializeField] private string _destinationSpawnPointId = "Default";

        [Tooltip("Resets IResettable entities registered to Destination Room Id upon arrival.")]
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
                _worldManager = FindAnyObjectByType<WorldManagerBehaviour>();
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

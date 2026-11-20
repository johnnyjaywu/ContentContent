#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D bench / checkpoint trigger that registers a hard respawn location on touch or explicit <see cref="Activate"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CheckpointBehaviour2D : MonoBehaviour
    {
        [SerializeField] private WorldManagerBehaviour _worldManager;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private string _checkpointId = "Bench_01";
        [SerializeField] private string _roomId = string.Empty;
        [SerializeField] private string _spawnPointId = string.Empty;
        [SerializeField] private bool _activateOnTouch = false;

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
            if (!_activateOnTouch || other == null)
                return;

            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
                return;

            Activate();
        }

        /// <summary>Activates this bench/checkpoint on <see cref="WorldManagerBehaviour"/>.</summary>
        public void Activate()
        {
            if (_worldManager == null)
                return;

            string room = !string.IsNullOrEmpty(_roomId) ? _roomId : _worldManager.Controller.CurrentRoomId;
            if (string.IsNullOrEmpty(room))
                return;

            _worldManager.ActivateCheckpoint(_checkpointId, room, _spawnPointId, transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.45f);
        }
    }
}
#endif

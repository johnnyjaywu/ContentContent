#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D checkpoint trigger that registers a hard respawn location on touch or explicit <see cref="Activate"/>.
    /// </summary>
    [AddComponentMenu("ContentContent/World/Checkpoint 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    [RequireComponent(typeof(Collider2D))]
    public class CheckpointBehaviour2D : MonoBehaviour
    {
        [Tooltip("Optional WorldManagerBehaviour. Auto-discovered in the scene if omitted.")]
        [SerializeField] private WorldManagerBehaviour _worldManager;

        [Tooltip("Trigger tag filter (empty allows any Collider2D).")]
        [SerializeField] private string _targetTag = "Player";

        [Tooltip("Unique checkpoint identifier recorded in CheckpointActivatedEvent.")]
        [SerializeField] private string _checkpointId = "Checkpoint_01";

        [Tooltip("Room/zone ID containing this checkpoint.")]
        [SerializeField] private string _roomId = string.Empty;

        [Tooltip("SpawnPoint ID within Room Id to place the player on death respawn.")]
        [SerializeField] private string _spawnPointId = string.Empty;

        [Tooltip("Automatically activates when touched by a matching Collider2D.")]
        [SerializeField] private bool _activateOnTouch = true;

        [Header("Events")]
        [SerializeField] private UnityEvent _onActivated = new UnityEvent();

        public UnityEvent OnActivated => _onActivated ?? (_onActivated = new UnityEvent());

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
            if (!_activateOnTouch || other == null)
                return;

            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
                return;

            Activate();
        }

        /// <summary>Activates this hard checkpoint on <see cref="WorldManagerBehaviour"/>.</summary>
        public void Activate()
        {
            if (_worldManager == null)
                return;

            string room = !string.IsNullOrEmpty(_roomId) ? _roomId : _worldManager.Controller.CurrentRoomId;
            if (string.IsNullOrEmpty(room))
                return;

            _worldManager.ActivateCheckpoint(_checkpointId, room, _spawnPointId, transform.position);
            _onActivated?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.45f);
        }
    }
}
#endif

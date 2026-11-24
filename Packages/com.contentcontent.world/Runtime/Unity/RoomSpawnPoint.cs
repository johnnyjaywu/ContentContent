using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Named spawn anchor inside a world room, auto-registering with <see cref="WorldManagerBehaviour"/>.
    /// </summary>
    [AddComponentMenu("ContentContent/World/Room Spawn Point")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class RoomSpawnPoint : MonoBehaviour
    {
        [Tooltip("Optional WorldManagerBehaviour. Auto-discovered in the scene if omitted.")]
        [SerializeField] private WorldManagerBehaviour _worldManager;

        [Tooltip("Room/zone ID owning this spawn anchor.")]
        [SerializeField] private string _roomId = string.Empty;

        [Tooltip("Spawn anchor ID matched by room transitions and checkpoints.")]
        [SerializeField] private string _spawnPointId = "Default";

        public string RoomId => _roomId;
        public string SpawnPointId => _spawnPointId;

        private void OnEnable()
        {
            if (_worldManager == null)
                _worldManager = FindAnyObjectByType<WorldManagerBehaviour>();

            _worldManager?.RegisterSpawnPoint(this);
        }

        private void OnDisable()
        {
            _worldManager?.UnregisterSpawnPoint(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.7f);
        }
    }
}

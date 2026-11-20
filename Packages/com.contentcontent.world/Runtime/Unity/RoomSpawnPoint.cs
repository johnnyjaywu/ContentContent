using UnityEngine;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Named spawn anchor inside a world room, auto-registering with <see cref="WorldManagerBehaviour"/>.
    /// </summary>
    public class RoomSpawnPoint : MonoBehaviour
    {
        [SerializeField] private WorldManagerBehaviour _worldManager;
        [SerializeField] private string _roomId = string.Empty;
        [SerializeField] private string _spawnPointId = "Default";

        public string RoomId => _roomId;
        public string SpawnPointId => _spawnPointId;

        private void OnEnable()
        {
            if (_worldManager == null)
                _worldManager = FindFirstObjectByType<WorldManagerBehaviour>();

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

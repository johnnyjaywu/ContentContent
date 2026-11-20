#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D spike/pit hazard trigger that invokes optional damage callbacks and requests safe-ground respawn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardZone2D : MonoBehaviour
    {
        [SerializeField] private WorldManagerBehaviour _worldManager;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private bool _respawnToSafeGround = true;
        [SerializeField] private UnityEvent<Collider2D> _onHazardTouched;

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
            if (other == null)
                return;

            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
                return;

            _onHazardTouched?.Invoke(other);

            if (_respawnToSafeGround && _worldManager != null)
            {
                _worldManager.TriggerHazardRespawn();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
#endif

#if CONTENTCONTENT_PHYSICS2D
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// 2D spike/pit hazard trigger that invokes optional damage callbacks and requests safe-ground respawn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("ContentContent/World/Hazard Zone 2D")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class HazardZone2D : MonoBehaviour
    {
        [Tooltip("Optional WorldManagerBehaviour. Auto-discovered in the scene if omitted.")]
        [SerializeField] private WorldManagerBehaviour _worldManager;

        [Tooltip("Trigger tag filter (empty allows any Collider2D).")]
        [SerializeField] private string _targetTag = "Player";

        [Tooltip("Returns player to last safe ground on non-fatal contact instead of only firing damage callbacks.")]
        [SerializeField] private bool _respawnToSafeGround = true;

        [Header("Events")]
        [SerializeField] private UnityEvent<Collider2D> _onHazardTouched = new UnityEvent<Collider2D>();

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

using UnityEngine;

namespace ContentContent.Core.Pooling.Unity
{
    /// <summary>
    /// Drop-in MonoBehaviour wrapper for pooling a single GameObject prefab.
    /// </summary>
    public class PrefabPoolBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Transform _container;
        [SerializeField] private int _prewarmCount = 8;
        [SerializeField] private int _maxSize = 128;
        [SerializeField] private bool _collectionCheck = true;

        private ObjectPool<GameObject> _pool;

        public int CountActive => _pool?.CountActive ?? 0;
        public int CountInactive => _pool?.CountInactive ?? 0;

        private void Awake()
        {
            if (_prefab == null)
            {
                Debug.LogError($"[{nameof(PrefabPoolBehaviour)}] Prefab is not assigned on '{name}'.", this);
                return;
            }

            Transform parent = _container != null ? _container : transform;
            _pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject go = Instantiate(_prefab, parent);
                    go.SetActive(false);
                    if (go.TryGetComponent<PooledObject>(out var pooledObj))
                    {
                        pooledObj.Bind(Despawn);
                    }
                    return go;
                },
                onGet: go => go.SetActive(true),
                onRelease: go =>
                {
                    if (go == null)
                        return;
                    go.SetActive(false);
                    go.transform.SetParent(parent, worldPositionStays: false);
                },
                onDestroy: go =>
                {
                    if (go != null)
                        Destroy(go);
                },
                collectionCheck: _collectionCheck,
                defaultCapacity: Mathf.Max(1, _prewarmCount),
                maxSize: Mathf.Max(1, _maxSize));

            if (_prewarmCount > 0)
            {
                _pool.Prewarm(_prewarmCount);
            }
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_pool == null)
                return null;

            GameObject go = _pool.Get();
            Transform t = go.transform;
            if (parent != null)
            {
                t.SetParent(parent, worldPositionStays: false);
            }
            t.SetPositionAndRotation(position, rotation);
            return go;
        }

        public void Despawn(GameObject instance)
        {
            if (_pool == null || instance == null)
                return;

            _pool.Release(instance);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }
    }
}

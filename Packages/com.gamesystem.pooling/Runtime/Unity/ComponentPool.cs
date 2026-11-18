using System;
using UnityEngine;

namespace GameSystem.Pooling.Unity
{
    /// <summary>
    /// Unity adapter around <see cref="ObjectPool{T}"/> for pooling prefab <see cref="Component"/> instances.
    /// </summary>
    public class ComponentPool<T> : IObjectPool<T>, IDisposable where T : Component
    {
        private readonly ObjectPool<T> _innerPool;
        private readonly T _prefab;
        private readonly Transform _defaultParent;

        public int CountActive => _innerPool.CountActive;
        public int CountInactive => _innerPool.CountInactive;
        public int CountAll => _innerPool.CountAll;

        public ComponentPool(
            T prefab,
            Transform defaultParent = null,
            int defaultCapacity = 16,
            int maxSize = 256,
            bool collectionCheck = true)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _defaultParent = defaultParent;

            _innerPool = new ObjectPool<T>(
                createFunc: CreateInstance,
                onGet: OnGetInstance,
                onRelease: OnReleaseInstance,
                onDestroy: OnDestroyInstance,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public T Get() => _innerPool.Get();

        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T instance = _innerPool.Get();
            Transform t = instance.transform;
            if (parent != null)
            {
                t.SetParent(parent, worldPositionStays: false);
            }
            t.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public PooledHandle<T> Get(out T instance) => _innerPool.Get(out instance);

        public void Release(T instance) => _innerPool.Release(instance);

        public void Prewarm(int count) => _innerPool.Prewarm(count);

        public void Clear() => _innerPool.Clear();

        public void Dispose() => _innerPool.Dispose();

        private T CreateInstance()
        {
            T instance = UnityEngine.Object.Instantiate(_prefab, _defaultParent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGetInstance(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        private void OnReleaseInstance(T instance)
        {
            if (instance == null)
                return;

            instance.gameObject.SetActive(false);
            if (_defaultParent != null)
            {
                instance.transform.SetParent(_defaultParent, worldPositionStays: false);
            }
        }

        private static void OnDestroyInstance(T instance)
        {
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }
    }
}

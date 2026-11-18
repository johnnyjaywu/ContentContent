using System;
using System.Collections.Generic;

namespace ContentContent.Core.Pooling
{
    /// <summary>
    /// Keyed container for managing multiple <see cref="IObjectPool{T}"/> instances without static singletons.
    /// </summary>
    public class PoolRegistry<TKey> : IDisposable
    {
        private readonly Dictionary<TKey, object> _pools = new Dictionary<TKey, object>();
        private readonly List<Action> _clearActions = new List<Action>();

        public void Register<T>(TKey key, IObjectPool<T> pool) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (_pools.ContainsKey(key))
                throw new InvalidOperationException($"A pool with key '{key}' is already registered.");

            _pools[key] = pool;
            _clearActions.Add(pool.Clear);
        }

        public bool TryGetPool<T>(TKey key, out IObjectPool<T> pool) where T : class
        {
            if (_pools.TryGetValue(key, out object raw) && raw is IObjectPool<T> typedPool)
            {
                pool = typedPool;
                return true;
            }

            pool = null;
            return false;
        }

        public IObjectPool<T> GetPool<T>(TKey key) where T : class
        {
            if (!TryGetPool<T>(key, out var pool))
            {
                throw new KeyNotFoundException($"No pool of type {typeof(T).Name} registered for key '{key}'.");
            }

            return pool;
        }

        public bool Contains(TKey key) => _pools.ContainsKey(key);

        public void ClearAll()
        {
            for (int i = 0; i < _clearActions.Count; i++)
            {
                _clearActions[i]();
            }
        }

        public void Dispose()
        {
            ClearAll();
            _pools.Clear();
            _clearActions.Clear();
        }
    }
}

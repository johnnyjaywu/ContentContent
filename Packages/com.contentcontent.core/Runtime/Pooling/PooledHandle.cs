using System;

namespace ContentContent.Core.Pooling
{
    /// <summary>
    /// Scope guard that automatically returns a leased instance to its <see cref="IObjectPool{T}"/> on disposal.
    /// </summary>
    public readonly struct PooledHandle<T> : IDisposable where T : class
    {
        private readonly T _instance;
        private readonly IObjectPool<T> _pool;

        public PooledHandle(T instance, IObjectPool<T> pool)
        {
            _instance = instance;
            _pool = pool;
        }

        public T Value => _instance;

        public void Dispose()
        {
            if (_instance != null && _pool != null)
            {
                _pool.Release(_instance);
            }
        }
    }
}

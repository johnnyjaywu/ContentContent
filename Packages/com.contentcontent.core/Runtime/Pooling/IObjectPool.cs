namespace ContentContent.Core.Pooling
{
    /// <summary>
    /// Generic contract for an object pool managing reusable instances of <typeparamref name="T"/>.
    /// </summary>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>Number of instances currently leased out from the pool.</summary>
        int CountActive { get; }

        /// <summary>Number of idle instances currently stored in the pool.</summary>
        int CountInactive { get; }

        /// <summary>Total number of active and inactive instances tracked by the pool.</summary>
        int CountAll { get; }

        /// <summary>Retrieves an instance from the pool or creates a new one if empty.</summary>
        T Get();

        /// <summary>
        /// Retrieves an instance wrapped in a <see cref="PooledHandle{T}"/> that automatically
        /// returns the instance to the pool when disposed.
        /// </summary>
        PooledHandle<T> Get(out T instance);

        /// <summary>Returns an instance back to the pool.</summary>
        void Release(T instance);

        /// <summary>Pre-allocates <paramref name="count"/> inactive instances up to the pool's maximum size.</summary>
        void Prewarm(int count);

        /// <summary>Destroys and removes all currently inactive instances from the pool.</summary>
        void Clear();
    }
}

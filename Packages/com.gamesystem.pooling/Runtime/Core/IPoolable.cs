namespace GameSystem.Pooling
{
    /// <summary>
    /// Optional lifecycle interface for pooled objects.
    /// When implemented, <see cref="ObjectPool{T}"/> automatically invokes these hooks on get and release.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from a pool.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// Called when the object is returned to a pool.
        /// </summary>
        void OnDespawned();
    }
}

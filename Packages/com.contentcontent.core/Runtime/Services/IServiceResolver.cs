namespace ContentContent.Core.Services
{
    /// <summary>
    /// Read-only service resolution contract for zero-reflection dependency lookup.
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Resolves a service of type <typeparamref name="T"/>, throwing if not registered.
        /// </summary>
        T Get<T>();

        /// <summary>
        /// Attempts to resolve a service of type <typeparamref name="T"/>.
        /// </summary>
        bool TryGet<T>(out T service);

        /// <summary>
        /// Returns true if type <typeparamref name="T"/> is registered in this scope
        /// (or any parent scope when <paramref name="includeParent"/> is true).
        /// </summary>
        bool IsRegistered<T>(bool includeParent = true);
    }
}

namespace ContentContent.Core.Services
{
    /// <summary>
    /// Lifetime strategy for a service registered in a <see cref="ServiceContainer"/>.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>A pre-created instance registered directly.</summary>
        Instance,

        /// <summary>Lazily created once per declaring container scope and cached.</summary>
        Singleton,

        /// <summary>Created anew on every resolution call.</summary>
        Transient
    }
}

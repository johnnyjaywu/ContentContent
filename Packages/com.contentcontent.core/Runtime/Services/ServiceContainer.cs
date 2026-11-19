using System;
using System.Collections.Generic;

namespace ContentContent.Core.Services
{
    /// <summary>
    /// Zero-reflection, AOT/IL2CPP-safe hierarchical service container supporting instances,
    /// lazy singletons, transients, child scopes, circular dependency detection, and
    /// reverse-order <see cref="IDisposable"/> teardown.
    /// </summary>
    public class ServiceContainer : IServiceResolver, IDisposable
    {
        private sealed class Registration
        {
            public ServiceLifetime Lifetime;
            public Func<IServiceResolver, object> Factory;
            public object Instance;
            public bool HasInstance;
            public bool OwnsInstance;
        }

        private readonly ServiceContainer _parent;
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isDisposed;

        /// <summary>Optional parent scope for fallback resolution.</summary>
        public ServiceContainer Parent => _parent;

        /// <summary>True after <see cref="Dispose"/> has been called.</summary>
        public bool IsDisposed => _isDisposed;

        public ServiceContainer(ServiceContainer parent = null)
        {
            _parent = parent;
        }

        /// <summary>Creates a child scope that falls back to this container for unresolved services.</summary>
        public ServiceContainer CreateScope()
        {
            ThrowIfDisposed();
            return new ServiceContainer(this);
        }

        /// <summary>
        /// Registers a pre-created instance for type <typeparamref name="T"/>.
        /// Throws if <typeparamref name="T"/> is already registered in this scope.
        /// </summary>
        public void RegisterInstance<T>(T instance, bool ownsInstance = false)
        {
            RegisterInstanceInternal(instance, ownsInstance, allowOverwrite: false);
        }

        /// <summary>
        /// Registers or replaces a pre-created instance for type <typeparamref name="T"/>.
        /// </summary>
        public void ReplaceInstance<T>(T instance, bool ownsInstance = false)
        {
            RegisterInstanceInternal(instance, ownsInstance, allowOverwrite: true);
        }

        /// <summary>
        /// Registers a lazy singleton factory for type <typeparamref name="T"/>.
        /// The created instance is owned and disposed (if <see cref="IDisposable"/>) with this scope.
        /// </summary>
        public void RegisterSingleton<T>(Func<IServiceResolver, T> factory)
        {
            RegisterFactoryInternal(factory, ServiceLifetime.Singleton, allowOverwrite: false);
        }

        /// <summary>
        /// Registers or replaces a lazy singleton factory for type <typeparamref name="T"/>.
        /// </summary>
        public void ReplaceSingleton<T>(Func<IServiceResolver, T> factory)
        {
            RegisterFactoryInternal(factory, ServiceLifetime.Singleton, allowOverwrite: true);
        }

        /// <summary>
        /// Registers a transient factory that creates a new instance of <typeparamref name="T"/> on each resolution.
        /// </summary>
        public void RegisterTransient<T>(Func<IServiceResolver, T> factory)
        {
            RegisterFactoryInternal(factory, ServiceLifetime.Transient, allowOverwrite: false);
        }

        /// <summary>
        /// Registers or replaces a transient factory for type <typeparamref name="T"/>.
        /// </summary>
        public void ReplaceTransient<T>(Func<IServiceResolver, T> factory)
        {
            RegisterFactoryInternal(factory, ServiceLifetime.Transient, allowOverwrite: true);
        }

        /// <summary>Removes a local registration for type <typeparamref name="T"/> from this scope.</summary>
        public bool Unregister<T>()
        {
            ThrowIfDisposed();
            return _registrations.Remove(typeof(T));
        }

        public T Get<T>()
        {
            if (TryGet<T>(out var service))
                return service;

            throw new InvalidOperationException($"Service of type '{typeof(T).FullName}' is not registered.");
        }

        public bool TryGet<T>(out T service)
        {
            ThrowIfDisposed();

            if (TryResolve(typeof(T), this, null, out object raw))
            {
                service = (T)raw;
                return true;
            }

            service = default;
            return false;
        }

        public bool IsRegistered<T>(bool includeParent = true)
        {
            ThrowIfDisposed();
            var type = typeof(T);

            if (_registrations.ContainsKey(type))
                return true;

            if (includeParent && _parent != null && !_parent._isDisposed)
                return _parent.IsRegistered<T>(true);

            return false;
        }

        /// <summary>
        /// Disposes all owned <see cref="IDisposable"/> services in reverse creation/registration order
        /// and clears this scope's registrations.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _registrations.Clear();

            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                _disposables[i].Dispose();
            }

            _disposables.Clear();
        }

        private void RegisterInstanceInternal<T>(T instance, bool ownsInstance, bool allowOverwrite)
        {
            ThrowIfDisposed();
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = typeof(T);
            if (!allowOverwrite && _registrations.ContainsKey(type))
                throw new InvalidOperationException($"Service of type '{type.FullName}' is already registered in this scope.");

            _registrations[type] = new Registration
            {
                Lifetime = ServiceLifetime.Instance,
                Instance = instance,
                HasInstance = true,
                OwnsInstance = ownsInstance
            };

            if (ownsInstance && instance is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
        }

        private void RegisterFactoryInternal<T>(Func<IServiceResolver, T> factory, ServiceLifetime lifetime, bool allowOverwrite)
        {
            ThrowIfDisposed();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);
            if (!allowOverwrite && _registrations.ContainsKey(type))
                throw new InvalidOperationException($"Service of type '{type.FullName}' is already registered in this scope.");

            _registrations[type] = new Registration
            {
                Lifetime = lifetime,
                Factory = r => factory(r),
                HasInstance = false,
                OwnsInstance = lifetime == ServiceLifetime.Singleton
            };
        }

        private bool TryResolve(Type type, ServiceContainer requestingScope, HashSet<Type> resolving, out object value)
        {
            ThrowIfDisposed();

            if (_registrations.TryGetValue(type, out var reg))
            {
                if (reg.HasInstance)
                {
                    value = reg.Instance;
                    return true;
                }

                if (resolving == null)
                {
                    resolving = new HashSet<Type>();
                }

                if (!resolving.Add(type))
                {
                    throw new InvalidOperationException($"Circular service dependency detected while resolving '{type.FullName}'.");
                }

                try
                {
                    // Singletons resolve against their declaring scope to prevent captive dependencies.
                    // Transients resolve against the requesting scope so child overrides apply.
                    IServiceResolver resolver = reg.Lifetime == ServiceLifetime.Singleton
                        ? new ScopedResolutionContext(this, resolving)
                        : new ScopedResolutionContext(requestingScope, resolving);

                    object created = reg.Factory(resolver);

                    if (reg.Lifetime == ServiceLifetime.Singleton)
                    {
                        reg.Instance = created;
                        reg.HasInstance = true;
                        if (reg.OwnsInstance && created is IDisposable disposable)
                        {
                            _disposables.Add(disposable);
                        }
                    }

                    value = created;
                    return true;
                }
                finally
                {
                    resolving.Remove(type);
                }
            }

            if (_parent != null)
            {
                return _parent.TryResolve(type, requestingScope, resolving, out value);
            }

            value = null;
            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));
        }

        private sealed class ScopedResolutionContext : IServiceResolver
        {
            private readonly ServiceContainer _container;
            private readonly HashSet<Type> _resolving;

            public ScopedResolutionContext(ServiceContainer container, HashSet<Type> resolving)
            {
                _container = container;
                _resolving = resolving;
            }

            public T Get<T>()
            {
                if (TryGet<T>(out var service))
                    return service;

                throw new InvalidOperationException($"Service of type '{typeof(T).FullName}' is not registered.");
            }

            public bool TryGet<T>(out T service)
            {
                if (_container.TryResolve(typeof(T), _container, _resolving, out object raw))
                {
                    service = (T)raw;
                    return true;
                }

                service = default;
                return false;
            }

            public bool IsRegistered<T>(bool includeParent = true)
            {
                return _container.IsRegistered<T>(includeParent);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace ContentContent.Core.Pooling
{
    /// <summary>
    /// Stack-backed generic object pool with optional collection checks and <see cref="IPoolable"/> support.
    /// </summary>
    public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
    {
        private readonly Stack<T> _inactive;
        private readonly HashSet<T> _inactiveLookup;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;
        private readonly bool _collectionCheck;

        public int CountActive { get; private set; }
        public int CountInactive => _inactive.Count;
        public int CountAll => CountActive + CountInactive;

        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 16,
            int maxSize = 1024)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than 0.");
            if (defaultCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(defaultCapacity), "Default capacity cannot be negative.");

            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            _collectionCheck = collectionCheck;

            int initialCapacity = Math.Min(defaultCapacity, maxSize);
            _inactive = new Stack<T>(initialCapacity);
            if (_collectionCheck)
            {
                _inactiveLookup = new HashSet<T>();
            }
        }

        public T Get()
        {
            T instance;
            if (_inactive.Count > 0)
            {
                instance = _inactive.Pop();
                _inactiveLookup?.Remove(instance);
            }
            else
            {
                instance = _createFunc();
            }

            CountActive++;
            _onGet?.Invoke(instance);
            if (instance is IPoolable poolable)
            {
                poolable.OnSpawned();
            }

            return instance;
        }

        public PooledHandle<T> Get(out T instance)
        {
            instance = Get();
            return new PooledHandle<T>(instance, this);
        }

        public void Release(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (_collectionCheck && !_inactiveLookup.Add(instance))
            {
                throw new InvalidOperationException(
                    $"Attempted to release an object of type {typeof(T).Name} that is already in the pool.");
            }

            if (CountActive > 0)
            {
                CountActive--;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnDespawned();
            }

            _onRelease?.Invoke(instance);

            if (_inactive.Count < _maxSize)
            {
                _inactive.Push(instance);
            }
            else
            {
                _inactiveLookup?.Remove(instance);
                _onDestroy?.Invoke(instance);
            }
        }

        public void Prewarm(int count)
        {
            if (count <= 0)
                return;

            int target = Math.Min(_inactive.Count + count, _maxSize);
            while (_inactive.Count < target)
            {
                T instance = _createFunc();
                _onRelease?.Invoke(instance);
                _inactive.Push(instance);
                _inactiveLookup?.Add(instance);
            }
        }

        public void Clear()
        {
            if (_onDestroy != null)
            {
                while (_inactive.Count > 0)
                {
                    _onDestroy(_inactive.Pop());
                }
            }
            else
            {
                _inactive.Clear();
            }

            _inactiveLookup?.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

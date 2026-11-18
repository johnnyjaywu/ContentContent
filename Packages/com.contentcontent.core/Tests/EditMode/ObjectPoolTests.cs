using System;
using NUnit.Framework;

namespace ContentContent.Core.Pooling.Tests
{
    public class ObjectPoolTests
    {
        private class DummyPoolable : IPoolable
        {
            public int SpawnCount { get; private set; }
            public int DespawnCount { get; private set; }

            public void OnSpawned() => SpawnCount++;
            public void OnDespawned() => DespawnCount++;
        }

        [Test]
        public void GetAndRelease_ReusesInstanceAndUpdatesCounts()
        {
            int created = 0;
            var pool = new ObjectPool<DummyPoolable>(() =>
            {
                created++;
                return new DummyPoolable();
            });

            var first = pool.Get();
            Assert.AreEqual(1, created);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(0, pool.CountInactive);
            Assert.AreEqual(1, first.SpawnCount);

            pool.Release(first);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, first.DespawnCount);

            var second = pool.Get();
            Assert.AreSame(first, second);
            Assert.AreEqual(1, created);
            Assert.AreEqual(2, second.SpawnCount);
        }

        [Test]
        public void PooledHandle_ReleasesAutomaticallyOnDispose()
        {
            var pool = new ObjectPool<DummyPoolable>(() => new DummyPoolable());
            DummyPoolable leased;

            using (pool.Get(out leased))
            {
                Assert.AreEqual(1, pool.CountActive);
                Assert.AreEqual(1, leased.SpawnCount);
            }

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, leased.DespawnCount);
        }

        [Test]
        public void Release_WhenAtMaxSize_InvokesOnDestroy()
        {
            int destroyed = 0;
            var pool = new ObjectPool<DummyPoolable>(
                createFunc: () => new DummyPoolable(),
                onDestroy: _ => destroyed++,
                maxSize: 1);

            var a = pool.Get();
            var b = pool.Get();

            pool.Release(a);
            pool.Release(b);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, destroyed);
        }

        [Test]
        public void Release_DuplicateWithCollectionCheck_Throws()
        {
            var pool = new ObjectPool<DummyPoolable>(() => new DummyPoolable(), collectionCheck: true);
            var item = pool.Get();
            pool.Release(item);

            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }

        [Test]
        public void Prewarm_PopulatesUpToMaxSize()
        {
            var pool = new ObjectPool<DummyPoolable>(() => new DummyPoolable(), maxSize: 3);
            pool.Prewarm(10);

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void PoolRegistry_RegistersRetrievesAndClearsPools()
        {
            int cleared = 0;
            using var registry = new PoolRegistry<string>();
            var pool = new ObjectPool<DummyPoolable>(
                createFunc: () => new DummyPoolable(),
                onDestroy: _ => cleared++);
            pool.Prewarm(2);

            registry.Register("bullets", pool);
            Assert.IsTrue(registry.Contains("bullets"));
            Assert.AreSame(pool, registry.GetPool<DummyPoolable>("bullets"));

            registry.ClearAll();
            Assert.AreEqual(2, cleared);
            Assert.AreEqual(0, pool.CountInactive);
        }
    }
}

using System;
using System.Collections.Generic;
using ContentContent.Core.Services;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class ServiceContainerTests
    {
        private interface IClock { int Now { get; } }
        private sealed class FixedClock : IClock
        {
            public int Now { get; }
            public FixedClock(int now) => Now = now;
        }

        private sealed class TrackedDisposable : IDisposable
        {
            private readonly string _name;
            private readonly List<string> _log;
            public bool Disposed { get; private set; }

            public TrackedDisposable(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Dispose()
            {
                Disposed = true;
                _log.Add(_name);
            }
        }

        private sealed class ServiceA { public ServiceB B; }
        private sealed class ServiceB { public ServiceA A; }

        [Test]
        public void RegisterAndResolve_InstanceSingletonAndTransient()
        {
            using var container = new ServiceContainer();
            int singletonCalls = 0;
            int transientCalls = 0;

            container.RegisterInstance<IClock>(new FixedClock(42));
            container.RegisterSingleton<string>(r =>
            {
                singletonCalls++;
                return $"t={r.Get<IClock>().Now}";
            });
            container.RegisterTransient<object>(_ =>
            {
                transientCalls++;
                return new object();
            });

            Assert.AreEqual(42, container.Get<IClock>().Now);
            Assert.AreEqual("t=42", container.Get<string>());
            Assert.AreEqual("t=42", container.Get<string>());
            Assert.AreEqual(1, singletonCalls);

            var o1 = container.Get<object>();
            var o2 = container.Get<object>();
            Assert.AreNotSame(o1, o2);
            Assert.AreEqual(2, transientCalls);
        }

        [Test]
        public void DuplicateRegistration_ThrowsByDefault_AndReplaceOverrides()
        {
            using var container = new ServiceContainer();
            container.RegisterInstance<IClock>(new FixedClock(1));

            Assert.Throws<InvalidOperationException>(() => container.RegisterInstance<IClock>(new FixedClock(2)));

            container.ReplaceInstance<IClock>(new FixedClock(2));
            Assert.AreEqual(2, container.Get<IClock>().Now);
        }

        [Test]
        public void CreateScope_FallsBackToParent_AndAllowsLocalOverrides()
        {
            using var root = new ServiceContainer();
            root.RegisterInstance<IClock>(new FixedClock(10));
            root.RegisterInstance("root-tag");

            using var child = root.CreateScope();
            child.RegisterInstance<IClock>(new FixedClock(99));

            Assert.AreEqual(99, child.Get<IClock>().Now);
            Assert.AreEqual("root-tag", child.Get<string>());
            Assert.AreEqual(10, root.Get<IClock>().Now);
            Assert.IsTrue(child.IsRegistered<string>(includeParent: true));
            Assert.IsFalse(child.IsRegistered<string>(includeParent: false));
        }

        [Test]
        public void Dispose_DisposesOwnedServicesInReverseOrder_AndSkipsUnowned()
        {
            var log = new List<string>();
            var unowned = new TrackedDisposable("unowned", log);

            using (var container = new ServiceContainer())
            {
                container.RegisterInstance(unowned, ownsInstance: false);
                container.RegisterSingleton<object>(_ => new TrackedDisposable("first", log));
                container.RegisterSingleton<IDisposable>(_ => new TrackedDisposable("second", log));

                container.Get<object>();
                container.Get<IDisposable>();
            }

            Assert.IsFalse(unowned.Disposed);
            CollectionAssert.AreEqual(new[] { "second", "first" }, log);
        }

        [Test]
        public void CircularDependency_ThrowsInvalidOperationException()
        {
            using var container = new ServiceContainer();
            container.RegisterSingleton(r => new ServiceA { B = r.Get<ServiceB>() });
            container.RegisterSingleton(r => new ServiceB { A = r.Get<ServiceA>() });

            Assert.Throws<InvalidOperationException>(() => container.Get<ServiceA>());
        }
    }
}

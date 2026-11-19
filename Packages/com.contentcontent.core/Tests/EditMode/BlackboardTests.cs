using System.Collections.Generic;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Fsm;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class BlackboardTests
    {
        [Test]
        public void SetGetRemove_SupportsDistinctTypesWithSameName()
        {
            var bb = new Blackboard();
            var healthInt = BlackboardKey.Create<int>("Health");
            var healthFloat = BlackboardKey.Create<float>("Health");

            bb.Set(healthInt, 100);
            bb.Set(healthFloat, 99.5f);

            Assert.IsTrue(bb.TryGet(healthInt, out int iVal));
            Assert.IsTrue(bb.TryGet(healthFloat, out float fVal));
            Assert.AreEqual(100, iVal);
            Assert.AreEqual(99.5f, fVal, 1e-4f);

            Assert.IsTrue(bb.Remove(healthInt));
            Assert.IsFalse(bb.Contains(healthInt));
            Assert.IsTrue(bb.Contains(healthFloat));
        }

        [Test]
        public void ParentScope_FallsBackOnRead_AndOverridesLocally()
        {
            var shared = new Blackboard();
            var local = new Blackboard(shared);

            var alertKey = BlackboardKey.Create<bool>("Alerted");
            var targetKey = BlackboardKey.Create<string>("Target");

            shared.Set(alertKey, true);
            shared.Set(targetKey, "Player");

            Assert.IsTrue(local.Get(alertKey));
            Assert.AreEqual("Player", local.Get(targetKey));
            Assert.IsFalse(local.ContainsLocal(alertKey));
            Assert.IsTrue(local.Contains(alertKey));

            // Local override does not mutate parent
            local.Set(targetKey, "Decoy");
            Assert.AreEqual("Decoy", local.Get(targetKey));
            Assert.AreEqual("Player", shared.Get(targetKey));
        }

        [Test]
        public void Subscribe_NotifiesOnlyOnChange_AndSupportsDispose()
        {
            var bb = new Blackboard();
            var ammoKey = BlackboardKey.Create<int>("Ammo");
            var changes = new List<(int oldVal, int newVal)>();

            var sub = bb.Subscribe(ammoKey, (o, n) => changes.Add((o, n)));

            bb.Set(ammoKey, 10);
            bb.Set(ammoKey, 10); // Same value: no notification
            bb.Set(ammoKey, 9);

            sub.Dispose();
            bb.Set(ammoKey, 8);

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual((0, 10), changes[0]);
            Assert.AreEqual((10, 9), changes[1]);
        }

        [Test]
        public void BlackboardFsmExtensions_DrivesStateMachineTransitions()
        {
            var bb = new Blackboard();
            var hasTarget = BlackboardKey.Create<bool>("HasTarget");
            var isDead = BlackboardKey.Create<bool>("IsDead");

            var idle = new ActionState();
            var chase = new ActionState();
            var dead = new ActionState();

            var fsm = new StateMachine(idle);
            fsm.AddTransitionWhen(idle, chase, bb, hasTarget, v => v);
            fsm.AddAnyTransitionWhen(dead, bb, isDead, v => v, priority: 10);

            fsm.OnEnter();
            Assert.AreSame(idle, fsm.CurrentState);

            bb.Set(hasTarget, true);
            fsm.OnTick(0.016f);
            Assert.AreSame(chase, fsm.CurrentState);

            bb.Set(isDead, true);
            fsm.OnTick(0.016f);
            Assert.AreSame(dead, fsm.CurrentState);
        }
    }
}

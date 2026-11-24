using System;
using System.Collections.Generic;
using ContentContent.Core.Events;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class EventBusTests
    {
        private readonly struct DamageEvent
        {
            public readonly int Amount;
            public DamageEvent(int amount) => Amount = amount;
        }

        private readonly struct HealEvent
        {
            public readonly int Amount;
            public HealEvent(int amount) => Amount = amount;
        }

        [Test]
        public void Publish_DeliversToSubscribers_AndIsolatesEventTypes()
        {
            var bus = new EventBus();
            int damageReceived = 0;
            int healReceived = 0;

            bus.Subscribe<DamageEvent>((in DamageEvent e) => damageReceived += e.Amount);
            bus.Subscribe<HealEvent>(e => healReceived += e.Amount);

            bus.Publish(new DamageEvent(25));

            Assert.AreEqual(25, damageReceived);
            Assert.AreEqual(0, healReceived);
            Assert.AreEqual(1, bus.GetSubscriberCount<DamageEvent>());
            Assert.AreEqual(1, bus.GetSubscriberCount<HealEvent>());
        }

        [Test]
        public void Publish_InvokesHigherPriorityFirst_AndPreservesFifoOnTies()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<DamageEvent>(_ => order.Add("normal-1"), priority: 0);
            bus.Subscribe<DamageEvent>(_ => order.Add("high"), priority: 10);
            bus.Subscribe<DamageEvent>(_ => order.Add("low"), priority: -5);
            bus.Subscribe<DamageEvent>(_ => order.Add("normal-2"), priority: 0);

            bus.Publish(new DamageEvent(1));

            CollectionAssert.AreEqual(new[] { "high", "normal-1", "normal-2", "low" }, order);
        }

        [Test]
        public void SubscriptionDispose_IsIdempotent_AndStopsDelivery()
        {
            var bus = new EventBus();
            int calls = 0;
            var sub = bus.Subscribe<DamageEvent>(_ => calls++);

            bus.Publish(new DamageEvent(1));
            sub.Dispose();
            sub.Dispose();
            bus.Publish(new DamageEvent(1));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, bus.GetSubscriberCount<DamageEvent>());
        }

        [Test]
        public void ReentrantPublish_DefersSubscribe_AndSkipsUnsubscribedImmediately()
        {
            var bus = new EventBus();
            var log = new List<string>();
            EventSubscription secondSub = default;

            bus.Subscribe<DamageEvent>(_ =>
            {
                log.Add("first");
                secondSub.Dispose();
                bus.Subscribe<DamageEvent>(_ => log.Add("late-added"));
            }, priority: 10);

            secondSub = bus.Subscribe<DamageEvent>(_ => log.Add("second"), priority: 0);

            bus.Publish(new DamageEvent(1));
            CollectionAssert.AreEqual(new[] { "first" }, log);

            log.Clear();
            bus.Publish(new DamageEvent(2));
            CollectionAssert.AreEqual(new[] { "first", "late-added" }, log);
        }

        [Test]
        public void Publish_FailFastByDefault_AndIsolatesWithErrorHandler()
        {
            var failFastBus = new EventBus();
            bool secondCalled = false;
            failFastBus.Subscribe<DamageEvent>(_ => throw new InvalidOperationException("boom"), priority: 1);
            failFastBus.Subscribe<DamageEvent>(_ => secondCalled = true, priority: 0);

            Assert.Throws<InvalidOperationException>(() => failFastBus.Publish(new DamageEvent(1)));
            Assert.IsFalse(secondCalled);

            Exception captured = null;
            var safeBus = new EventBus(ex => captured = ex);
            safeBus.Subscribe<DamageEvent>(_ => throw new InvalidOperationException("safe-boom"), priority: 1);
            safeBus.Subscribe<DamageEvent>(_ => secondCalled = true, priority: 0);

            safeBus.Publish(new DamageEvent(1));
            Assert.IsNotNull(captured);
            Assert.AreEqual("safe-boom", captured.Message);
            Assert.IsTrue(secondCalled);
        }

        [Test]
        public void Clear_RemovesTypeSpecificOrAllSubscribers()
        {
            var bus = new EventBus();
            bus.Subscribe<DamageEvent>(_ => { });
            bus.Subscribe<HealEvent>(_ => { });

            bus.Clear<DamageEvent>();
            Assert.AreEqual(0, bus.GetSubscriberCount<DamageEvent>());
            Assert.AreEqual(1, bus.GetSubscriberCount<HealEvent>());

            bus.Clear();
            Assert.AreEqual(0, bus.GetSubscriberCount<HealEvent>());
        }

        [Test]
        public void CopyChannelStats_ReportsActiveChannelCounts()
        {
            var bus = new EventBus();
            var sub = bus.Subscribe<DamageEvent>(_ => { });
            bus.Subscribe<HealEvent>(_ => { });

            Assert.AreEqual(2, bus.ChannelCount);

            var stats = new List<KeyValuePair<string, int>>();
            bus.CopyChannelStats(stats);
            Assert.AreEqual(2, stats.Count);

            sub.Dispose();
            bus.CopyChannelStats(stats);
            Assert.Contains(new KeyValuePair<string, int>(nameof(DamageEvent), 0), stats);
            Assert.Contains(new KeyValuePair<string, int>(nameof(HealEvent), 1), stats);
        }
    }
}

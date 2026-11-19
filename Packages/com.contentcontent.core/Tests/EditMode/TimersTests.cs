using System;
using ContentContent.Core.Timers;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class TimersTests
    {
        [Test]
        public void After_FiresOnce_AndDeactivatesHandle()
        {
            var scheduler = new TimerScheduler();
            int fired = 0;

            var handle = scheduler.After(1.0f, () => fired++);
            Assert.IsTrue(handle.IsActive);
            Assert.AreEqual(1, scheduler.ActiveCount);

            scheduler.Tick(0.4f);
            Assert.AreEqual(0, fired);
            Assert.AreEqual(0.6f, handle.Remaining, 1e-4f);

            scheduler.Tick(0.6f);
            Assert.AreEqual(1, fired);
            Assert.IsFalse(handle.IsActive);
            Assert.AreEqual(0, scheduler.ActiveCount);

            scheduler.Tick(5.0f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Every_PreservesRemainder_AndRespectsCatchUpCap()
        {
            var scheduler = new TimerScheduler(maxFiresPerTick: 3);
            int fires = 0;

            scheduler.Every(0.5f, () => fires++);

            // 1.2s => fires at 0.5 and 1.0 (2 times), remainder = 0.3s left until next fire
            scheduler.Tick(1.2f);
            Assert.AreEqual(2, fires);

            // 0.3s more => hits 1.5s (3rd fire)
            scheduler.Tick(0.3f);
            Assert.AreEqual(3, fires);

            // Huge delta (10s) capped to maxFiresPerTick = 3
            fires = 0;
            scheduler.Tick(10.0f);
            Assert.AreEqual(3, fires);
        }

        [Test]
        public void Every_WithFiniteRepeatCount_CompletesAutomatically()
        {
            var scheduler = new TimerScheduler();
            int fires = 0;

            var handle = scheduler.Every(0.25f, () => fires++, repeatCount: 3);
            scheduler.Tick(1.0f);

            Assert.AreEqual(3, fires);
            Assert.IsFalse(handle.IsActive);
            Assert.AreEqual(0, scheduler.ActiveCount);
        }

        [Test]
        public void PauseResumeCancel_AndStaleHandleProtection()
        {
            var scheduler = new TimerScheduler();
            int aFired = 0;
            int bFired = 0;

            var handleA = scheduler.After(1.0f, () => aFired++);
            Assert.IsTrue(handleA.Pause());
            Assert.IsTrue(handleA.IsPaused);

            scheduler.Tick(2.0f);
            Assert.AreEqual(0, aFired);

            Assert.IsTrue(handleA.Resume());
            Assert.IsTrue(handleA.Cancel());
            Assert.IsFalse(handleA.IsActive);

            // Reallocate slot with handleB; stale handleA must not cancel handleB
            var handleB = scheduler.After(0.5f, () => bFired++);
            Assert.IsFalse(handleA.Cancel());
            Assert.IsTrue(handleB.IsActive);

            scheduler.Tick(0.5f);
            Assert.AreEqual(0, aFired);
            Assert.AreEqual(1, bFired);
        }

        [Test]
        public void CallbackCanScheduleNewTimer_WithoutFiringOnSameTick()
        {
            var scheduler = new TimerScheduler();
            int chainedFired = 0;

            scheduler.After(0.5f, () =>
            {
                scheduler.After(0.1f, () => chainedFired++);
            });

            scheduler.Tick(1.0f);
            Assert.AreEqual(0, chainedFired);

            scheduler.Tick(0.1f);
            Assert.AreEqual(1, chainedFired);
        }

        [Test]
        public void Cooldown_TracksProgressAndConsume()
        {
            var cd = new Cooldown(2.0f);
            Assert.IsTrue(cd.IsReady);
            Assert.AreEqual(1f, cd.Progress, 1e-4f);

            Assert.IsTrue(cd.TryConsume());
            Assert.IsFalse(cd.IsReady);
            Assert.IsFalse(cd.TryConsume());
            Assert.AreEqual(0f, cd.Progress, 1e-4f);

            Assert.IsFalse(cd.Tick(1.0f));
            Assert.AreEqual(0.5f, cd.Progress, 1e-4f);

            Assert.IsTrue(cd.Tick(1.5f));
            Assert.IsTrue(cd.IsReady);
            Assert.AreEqual(0f, cd.Remaining);
            Assert.AreEqual(1f, cd.Progress, 1e-4f);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using ContentContent.Core.Events;
using NUnit.Framework;

namespace ContentContent.Camera.Tests
{
    public class CameraDirectorTests
    {
        [Test]
        public void ZoneStack_PriorityAndRecencyTieBreakAndEvents()
        {
            var bus = new EventBus();
            var changedEvents = new List<CameraZoneChangedEvent>();
            bus.Subscribe<CameraZoneChangedEvent>((in CameraZoneChangedEvent e) => changedEvents.Add(e));

            var stack = new CameraZoneStack(bus);
            Assert.IsNull(stack.ActiveZone);

            var roomA = new CameraZoneConfig { ZoneId = "RoomA", Priority = 1 };
            var roomB = new CameraZoneConfig { ZoneId = "RoomB", Priority = 1 };
            var boss = new CameraZoneConfig { ZoneId = "Boss", Priority = 10 };

            stack.EnterZone(roomA);
            Assert.AreEqual("RoomA", stack.ActiveZone.ZoneId);

            // Equal priority -> most recently entered wins
            stack.EnterZone(roomB);
            Assert.AreEqual("RoomB", stack.ActiveZone.ZoneId);

            // Higher priority wins
            stack.EnterZone(boss);
            Assert.AreEqual("Boss", stack.ActiveZone.ZoneId);
            Assert.AreEqual(3, stack.Count);

            // Exiting boss falls back to RoomB
            Assert.IsTrue(stack.ExitZone("Boss"));
            Assert.AreEqual("RoomB", stack.ActiveZone.ZoneId);

            stack.Clear();
            Assert.IsNull(stack.ActiveZone);
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(5, changedEvents.Count);
            Assert.AreEqual("RoomA", changedEvents[0].ActiveZoneId);
            Assert.AreEqual(string.Empty, changedEvents[4].ActiveZoneId);
        }

        [Test]
        public void LookAhead_RespectsTurnDelayAndConvergesFrameRateIndependently()
        {
            var config30 = new CameraFramingConfig
            {
                LookAheadDistance = 2f,
                LookAheadSmoothSpeed = 6f,
                LookAheadTurnDelay = 0.1f
            };
            var config120 = config30.Clone();

            var director30 = new CameraDirector(config30);
            var director120 = new CameraDirector(config120);

            var faceRight = new CameraFrameInput { FacingDirection = Vector3.UnitX };
            var faceLeft = new CameraFrameInput { FacingDirection = -Vector3.UnitX };

            // Initialize facing right for 0.5s at 30 Hz vs 120 Hz
            for (int i = 0; i < 15; i++)
                director30.Tick(1f / 30f, in faceRight);
            for (int i = 0; i < 60; i++)
                director120.Tick(1f / 120f, in faceRight);

            Assert.AreEqual(director30.CurrentLookAheadOffset, director120.CurrentLookAheadOffset, 0.05f);
            Assert.Greater(director120.CurrentLookAheadOffset, 1.85f);

            // Flip facing left for 0.05s (< LookAheadTurnDelay 0.1s) -> still targets +2f
            float beforeTurnDelay = director120.CurrentLookAheadOffset;
            for (int i = 0; i < 6; i++)
                director120.Tick(1f / 120f, in faceLeft);
            Assert.GreaterOrEqual(director120.CurrentLookAheadOffset, beforeTurnDelay);

            // Continue holding left past turn delay -> swings toward -2f
            for (int i = 0; i < 60; i++)
                director120.Tick(1f / 120f, in faceLeft);
            Assert.Less(director120.CurrentLookAheadOffset, -1.5f);
        }

        [Test]
        public void VerticalPeek_RequiresGroundedAndStationaryHoldDelayAndResetsOnMove()
        {
            var config = new CameraFramingConfig
            {
                LookAheadDistance = 0f,
                VerticalPeekUpDistance = 3f,
                VerticalPeekDownDistance = 4f,
                VerticalPeekHoldDelay = 0.4f,
                VerticalPeekSmoothSpeed = 20f
            };
            var director = new CameraDirector(config);

            // Airborne hold down -> does not peek
            var airborneHoldDown = new CameraFrameInput
            {
                VerticalLookInput = -1f,
                IsTargetGrounded = false,
                IsTargetStationary = true
            };
            director.Tick(0.6f, in airborneHoldDown);
            Assert.AreEqual(0f, director.CurrentVerticalPeekOffset, 0.001f);

            // Grounded + stationary hold down for 0.2s (< 0.4s delay) -> still 0
            var groundedHoldDown = new CameraFrameInput
            {
                VerticalLookInput = -1f,
                IsTargetGrounded = true,
                IsTargetStationary = true
            };
            director.Tick(0.2f, in groundedHoldDown);
            Assert.AreEqual(0f, director.CurrentVerticalPeekOffset, 0.001f);

            // Hold past delay -> moves down toward -4f
            director.Tick(0.25f, in groundedHoldDown);
            director.Tick(0.15f, in groundedHoldDown);
            Assert.Less(director.CurrentVerticalPeekOffset, -3f);

            // Start moving -> resets target back toward 0
            var moving = new CameraFrameInput
            {
                VerticalLookInput = -1f,
                IsTargetGrounded = true,
                IsTargetStationary = false
            };
            for (int i = 0; i < 20; i++)
                director.Tick(0.05f, in moving);
            Assert.AreEqual(0f, director.CurrentVerticalPeekOffset, 0.01f);
        }

        [Test]
        public void BossZone_LocksPositionOverridesZoomAndDisablesLookAhead()
        {
            var config = new CameraFramingConfig
            {
                DefaultOrthographicSize = 6f,
                LookAheadDistance = 2f,
                LookAheadSmoothSpeed = 20f,
                ZoomSmoothSpeed = 20f
            };
            var director = new CameraDirector(config);

            director.Zones.EnterZone(new CameraZoneConfig
            {
                ZoneId = "BossArena",
                Priority = 10,
                LockPosition = true,
                LockedPosition = new Vector3(50f, 12f, 0f),
                OrthographicSizeOverride = 9f,
                StaticOffset = new Vector3(0f, 1f, 0f),
                AllowLookAhead = false,
                AllowVerticalPeek = false,
                BoundsId = "BossBounds"
            });

            var input = new CameraFrameInput
            {
                TargetPosition = new Vector3(10f, 5f, 0f),
                FacingDirection = Vector3.UnitX,
                VerticalLookInput = 1f,
                IsTargetGrounded = true,
                IsTargetStationary = true
            };

            CameraFrameOutput output = default;
            for (int i = 0; i < 20; i++)
            {
                output = director.Tick(0.05f, in input);
            }

            Assert.IsTrue(output.IsPositionLocked);
            Assert.AreEqual("BossArena", output.ActiveZoneId);
            Assert.AreEqual("BossBounds", output.ActiveBoundsId);
            Assert.AreEqual(50f, output.FollowAnchorPosition.X, 0.01f);
            Assert.AreEqual(13f, output.FollowAnchorPosition.Y, 0.01f);
            Assert.AreEqual(0f, director.CurrentLookAheadOffset, 0.001f);
            Assert.AreEqual(9f, output.OrthographicSize, 0.05f);
        }

        [Test]
        public void RequestSnap_SnapsImmediatelyForSingleTickAndPublishesEvent()
        {
            var bus = new EventBus();
            int snapEvents = 0;
            bus.Subscribe<CameraSnapRequestedEvent>((in CameraSnapRequestedEvent _) => snapEvents++);

            var config = new CameraFramingConfig
            {
                DefaultOrthographicSize = 6f,
                LookAheadDistance = 3f,
                LookAheadSmoothSpeed = 1f,
                ZoomSmoothSpeed = 1f
            };
            var director = new CameraDirector(config, bus);
            director.Zones.EnterZone(new CameraZoneConfig
            {
                ZoneId = "WideRoom",
                OrthographicSizeOverride = 10f
            });

            director.RequestSnap(new Vector3(20f, 4f, 0f), "RoomTransition");
            Assert.AreEqual(1, snapEvents);

            var input = new CameraFrameInput
            {
                TargetPosition = new Vector3(20f, 4f, 0f),
                FacingDirection = Vector3.UnitX
            };

            CameraFrameOutput first = director.Tick(0.016f, in input);
            Assert.IsTrue(first.SnapRequested);
            Assert.AreEqual(3f, director.CurrentLookAheadOffset, 0.001f);
            Assert.AreEqual(10f, first.OrthographicSize, 0.001f);
            Assert.AreEqual(23f, first.FollowAnchorPosition.X, 0.001f);

            // Second tick clears SnapRequested
            CameraFrameOutput second = director.Tick(0.016f, in input);
            Assert.IsFalse(second.SnapRequested);
        }

        [Test]
        public void RequestImpulse_PublishesOnEventBusAndCallback()
        {
            var bus = new EventBus();
            var busImpulses = new List<CameraImpulseRequestedEvent>();
            var callbackImpulses = new List<CameraImpulseRequestedEvent>();

            bus.Subscribe<CameraImpulseRequestedEvent>((in CameraImpulseRequestedEvent e) => busImpulses.Add(e));
            var director = new CameraDirector(eventBus: bus);
            director.OnImpulseRequested += e => callbackImpulses.Add(e);

            director.RequestImpulse(new Vector3(0f, -1f, 0f), 0.8f, "HeavyLand");

            Assert.AreEqual(1, busImpulses.Count);
            Assert.AreEqual(1, callbackImpulses.Count);
            Assert.AreEqual("HeavyLand", busImpulses[0].SourceId);
            Assert.AreEqual(0.8f, busImpulses[0].Strength, 0.001f);
            Assert.AreEqual(-1f, busImpulses[0].Velocity.Y, 0.001f);
        }
    }
}

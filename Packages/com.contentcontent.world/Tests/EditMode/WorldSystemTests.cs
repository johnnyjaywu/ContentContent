using System.Collections.Generic;
using System.Numerics;
using ContentContent.Core.Events;
using NUnit.Framework;

namespace ContentContent.World.Tests
{
    public class WorldSystemTests
    {
        private sealed class DummyEnemy : IResettable
        {
            public bool IsAlive = true;
            public int ResetCount { get; private set; }

            public void ResetState()
            {
                IsAlive = true;
                ResetCount++;
            }
        }

        [Test]
        public void WorldFlagStore_StoresTypedFlagsTracksVisitsAndRoundTripsSnapshots()
        {
            var bus = new EventBus();
            var changes = new List<WorldFlagChangedEvent>();
            bus.Subscribe<WorldFlagChangedEvent>((in WorldFlagChangedEvent e) => changes.Add(e));

            var store = new WorldFlagStore(bus);
            store.SetBool("BossDefeated", true);
            store.SetInt("GrubsRescued", 12);
            store.SetString("LastBench", "Dirtmouth");

            Assert.IsTrue(store.GetBool("BossDefeated"));
            Assert.AreEqual(12, store.GetInt("GrubsRescued"));
            Assert.AreEqual("Dirtmouth", store.GetString("LastBench"));
            Assert.AreEqual(3, changes.Count);

            // Setting identical value does not re-fire
            store.SetInt("GrubsRescued", 12);
            Assert.AreEqual(3, changes.Count);

            Assert.IsTrue(store.MarkRoomVisited("Crossroads_01"));
            Assert.IsFalse(store.MarkRoomVisited("Crossroads_01"));
            Assert.IsTrue(store.HasVisitedRoom("Crossroads_01"));
            Assert.AreEqual(1, store.VisitedRoomCount);

            Dictionary<string, WorldFlagValue> flagSnapshot = store.ExportFlags();
            List<string> visitedSnapshot = store.ExportVisitedRooms();

            store.Clear();
            Assert.AreEqual(0, store.FlagCount);
            Assert.AreEqual(0, store.VisitedRoomCount);

            store.ImportFlags(flagSnapshot);
            store.ImportVisitedRooms(visitedSnapshot);

            Assert.IsTrue(store.GetBool("BossDefeated"));
            Assert.AreEqual(12, store.GetInt("GrubsRescued"));
            Assert.AreEqual("Dirtmouth", store.GetString("LastBench"));
            Assert.IsTrue(store.HasVisitedRoom("Crossroads_01"));
        }

        [Test]
        public void WorldGraph_ValidatesGatedExitsAndTraversesUnlockedPaths()
        {
            var graph = new WorldGraph();
            graph.AddRoom("Town", hasCheckpoint: true);
            graph.AddRoom("Caverns");
            graph.AddRoom("Peak");

            graph.AddBidirectionalConnection("Town", "Caverns", "ToCaverns", "ToTown");
            graph.AddExit("Caverns", new RoomExit("ToPeak", "Peak", "PeakEntry", requiredAbilities: new[] { "DoubleJump" }, requiredFlags: new[] { "LeverPulled" }));

            var controller = new RoomLifecycleController(graph);
            var abilities = new HashSet<string>();

            controller.TransitionTo("Town");
            Assert.IsTrue(controller.TryTraverseExit("ToCaverns", abilities.Contains));
            Assert.AreEqual("Caverns", controller.CurrentRoomId);

            // Locked: lacks DoubleJump and LeverPulled
            Assert.IsFalse(controller.TryTraverseExit("ToPeak", abilities.Contains));
            Assert.AreEqual("Caverns", controller.CurrentRoomId);

            // Missing flag still blocks
            abilities.Add("DoubleJump");
            Assert.IsFalse(controller.TryTraverseExit("ToPeak", abilities.Contains));

            // Both satisfied -> unlocks
            controller.Flags.SetBool("LeverPulled", true);
            Assert.IsTrue(controller.TryTraverseExit("ToPeak", abilities.Contains));
            Assert.AreEqual("Peak", controller.CurrentRoomId);
            Assert.AreEqual("PeakEntry", controller.CurrentSpawnPointId);
        }

        [Test]
        public void ReachabilityValidator_DetectsMissingAbilityAndKeyBehindOwnDoorSoftLock()
        {
            var graph = new WorldGraph();
            graph.AddRoom("Start", hasCheckpoint: true);
            graph.AddRoom("LockedVault");
            graph.AddRoom("Goal");

            // Key ("VaultKey") sits inside LockedVault, which requires "VaultKey" to enter!
            graph.AddRoom("LockedVault").GrantFlag("VaultKey");
            graph.AddExit("Start", new RoomExit("Door", "LockedVault", requiredFlags: new[] { "VaultKey" }));
            graph.AddExit("LockedVault", new RoomExit("ToGoal", "Goal"));

            Assert.IsFalse(WorldReachabilityValidator.CanReach(graph, "Start", "Goal"));
            CollectionAssert.AreEquivalent(new[] { "LockedVault", "Goal" }, WorldReachabilityValidator.FindUnreachableRooms(graph, "Start"));

            // Put ability in reachable side room -> progression chain succeeds
            graph.AddRoom("SideRoom").GrantAbility("MantisClaw");
            graph.AddBidirectionalConnection("Start", "SideRoom");
            graph.AddExit("Start", new RoomExit("UpperShaft", "Goal", requiredAbilities: new[] { "MantisClaw" }));

            Assert.IsTrue(WorldReachabilityValidator.CanReach(graph, "Start", "Goal"));
        }

        [Test]
        public void ReachabilityValidator_DetectsTrappedOneWayDropSoftLock()
        {
            var graph = new WorldGraph();
            graph.AddRoom("Hub", hasCheckpoint: true);
            graph.AddRoom("Ledge");
            graph.AddRoom("PitTrap"); // No exits back, no checkpoint!

            graph.AddBidirectionalConnection("Hub", "Ledge");
            graph.AddExit("Ledge", new RoomExit("OneWayDrop", "PitTrap"));

            List<string> softLocks = WorldReachabilityValidator.FindSoftLockRooms(graph, "Hub");
            CollectionAssert.AreEqual(new[] { "PitTrap" }, softLocks);

            // Add return tunnel -> soft-lock disappears
            graph.AddExit("PitTrap", new RoomExit("EscapeTunnel", "Hub"));
            Assert.AreEqual(0, WorldReachabilityValidator.FindSoftLockRooms(graph, "Hub").Count);
        }

        [Test]
        public void CheckpointService_DistinguishesHazardSafeGroundFromHardCheckpoint()
        {
            var service = new CheckpointService();
            var bench = new CheckpointData("Bench_01", "Town", "BenchSpawn", new Vector3(1f, 2f, 0f));
            service.ActivateCheckpoint(bench);

            // With safe ground in current room -> hazard recovery returns safe ground without resetting room
            service.RecordSafeGround("Caverns", new Vector3(15f, 4f, 0f));
            RespawnRequestedEvent hazard = service.RequestHazardRespawn("Caverns");
            Assert.AreEqual(RespawnKind.HazardSafeGround, hazard.Kind);
            Assert.AreEqual("Caverns", hazard.RoomId);
            Assert.AreEqual(15f, hazard.Position.X, 0.001f);
            Assert.IsFalse(hazard.ResetRoomEntities);

            // Different room with no safe ground -> falls back to hard checkpoint & resets room
            RespawnRequestedEvent fallback = service.RequestHazardRespawn("DeepPit");
            Assert.AreEqual(RespawnKind.HardCheckpoint, fallback.Kind);
            Assert.AreEqual("Town", fallback.RoomId);
            Assert.IsTrue(fallback.ResetRoomEntities);

            // Death always respawns at hard checkpoint with reset flag
            RespawnRequestedEvent death = service.RequestDeathRespawn();
            Assert.AreEqual(RespawnKind.HardCheckpoint, death.Kind);
            Assert.AreEqual("Town", death.RoomId);
            Assert.AreEqual("BenchSpawn", death.SpawnPointId);
            Assert.IsTrue(death.ResetRoomEntities);
        }

        [Test]
        public void RoomLifecycleController_ResetsRoomEntitiesOnEntryAndDeathRespawnAndPublishesEvents()
        {
            var bus = new EventBus();
            var entered = new List<RoomEnteredEvent>();
            var exited = new List<RoomExitedEvent>();
            bus.Subscribe<RoomEnteredEvent>((in RoomEnteredEvent e) => entered.Add(e));
            bus.Subscribe<RoomExitedEvent>((in RoomExitedEvent e) => exited.Add(e));

            var controller = new RoomLifecycleController(eventBus: bus);
            var benchEnemy = new DummyEnemy();
            var hallEnemy = new DummyEnemy();

            controller.RegisterResettable("Hub", benchEnemy);
            controller.RegisterResettable("Hall", hallEnemy);

            controller.TransitionTo("Hub", "Bench");
            controller.Checkpoints.ActivateCheckpoint(new CheckpointData("Bench_Hub", "Hub", "Bench", Vector3.Zero));

            // Enter Hall -> resets hallEnemy once, marks first visit
            controller.TransitionTo("Hall", "West");
            Assert.AreEqual(1, hallEnemy.ResetCount);
            Assert.IsTrue(entered[1].IsFirstVisit);

            // Defeat hallEnemy and touch hazard with safe ground -> hallEnemy stays defeated!
            hallEnemy.IsAlive = false;
            controller.Checkpoints.RecordSafeGround("Hall", new Vector3(3f, 0f, 0f));
            RespawnRequestedEvent hazard = controller.RespawnFromHazard();
            Assert.AreEqual(RespawnKind.HazardSafeGround, hazard.Kind);
            Assert.IsFalse(hallEnemy.IsAlive);

            // Die -> returns to Hub bench and resets Hub; re-entering Hall resets hallEnemy back to alive
            RespawnRequestedEvent death = controller.RespawnFromDeath();
            Assert.AreEqual("Hub", controller.CurrentRoomId);
            Assert.AreEqual(2, benchEnemy.ResetCount);
            Assert.IsTrue(death.ResetRoomEntities);

            controller.TransitionTo("Hall", "West");
            Assert.IsFalse(entered[entered.Count - 1].IsFirstVisit);
            Assert.IsTrue(hallEnemy.IsAlive);
            Assert.AreEqual(2, hallEnemy.ResetCount);
            Assert.AreEqual(3, exited.Count);
        }
    }
}

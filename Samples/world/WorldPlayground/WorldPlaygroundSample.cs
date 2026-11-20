using System.Collections.Generic;
using ContentContent.Core.Events;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.World.Samples
{
    /// <summary>
    /// Interactive playground demonstrating <see cref="RoomLifecycleController"/>,
    /// <see cref="WorldGraph"/> ability gates, <see cref="WorldReachabilityValidator"/>,
    /// bench checkpoints, hazard safe-ground respawn, and deterministic <see cref="IResettable"/> resets.
    /// Uses Unity's New Input System (<c>com.unity.inputsystem</c>).
    /// </summary>
    public class WorldPlaygroundSample : MonoBehaviour
    {
        private sealed class SampleRoomEnemy : IResettable
        {
            public bool Defeated { get; set; }
            public int ResetCount { get; private set; }

            public void ResetState()
            {
                Defeated = false;
                ResetCount++;
            }
        }

        private EventBus _bus;
        private RoomLifecycleController _world;
        private readonly HashSet<string> _abilities = new HashSet<string>();
        private readonly SampleRoomEnemy _hallEnemy = new SampleRoomEnemy();
        private readonly List<string> _log = new List<string>();
        private string _validationReport = string.Empty;

        private void Start()
        {
            _bus = new EventBus();
            var graph = new WorldGraph();

            RoomNode hub = graph.AddRoom("Crossroads_Hub", "Crossroads Bench", "Crossroads", hasCheckpoint: true);
            RoomNode hall = graph.AddRoom("Mantis_Hall", "Mantis Hall", "FungalWastes");
            RoomNode shrine = graph.AddRoom("Mantis_Shrine", "Claw Shrine", "FungalWastes");
            shrine.GrantAbility("WallJump");
            RoomNode boss = graph.AddRoom("Lords_Arena", "Mantis Lords", "FungalWastes");

            graph.AddBidirectionalConnection(hub.RoomId, hall.RoomId, "EastDoor", "WestDoor");
            graph.AddBidirectionalConnection(hall.RoomId, shrine.RoomId, "ShrinePath", "ExitShrine");
            graph.AddBidirectionalConnection(
                hall.RoomId,
                boss.RoomId,
                "ArenaGate",
                "ExitArena",
                requiredAbilities: new[] { "WallJump" });

            _world = new RoomLifecycleController(graph, new WorldFlagStore(_bus), new CheckpointService(_bus), _bus);
            _world.RegisterResettable("Mantis_Hall", _hallEnemy);

            _bus.Subscribe<RoomEnteredEvent>((in RoomEnteredEvent e) =>
                AppendLog($"[Room] {e.PreviousRoomId} -> {e.RoomId} (FirstVisit={e.IsFirstVisit})"));
            _bus.Subscribe<RespawnRequestedEvent>((in RespawnRequestedEvent e) =>
                AppendLog($"[Respawn] {e.Kind} in {e.RoomId} (ResetEntities={e.ResetRoomEntities})"));

            _world.TransitionTo("Crossroads_Hub", "Bench");
            _world.Checkpoints.ActivateCheckpoint(new CheckpointData("Bench_Crossroads", "Crossroads_Hub", "Bench", NumVector3.Zero));

            RunValidator();
        }

        private void Update()
        {
            if (_world == null)
                return;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) _world.TransitionTo("Crossroads_Hub", "Bench");
                if (kb.digit2Key.wasPressedThisFrame) _world.TransitionTo("Mantis_Hall", "WestDoor");
                if (kb.digit3Key.wasPressedThisFrame)
                {
                    if (!_world.TryTraverseExit("ArenaGate", _abilities.Contains))
                        AppendLog("[Gate] ArenaGate locked (needs WallJump)");
                }
                if (kb.gKey.wasPressedThisFrame)
                {
                    _abilities.Add("WallJump");
                    AppendLog("[Ability] Acquired WallJump!");
                }
                if (kb.hKey.wasPressedThisFrame) _world.RespawnFromHazard();
                if (kb.kKey.wasPressedThisFrame) _world.RespawnFromDeath();
            }
#endif
        }

        private void RunValidator()
        {
            bool bossReachable = WorldReachabilityValidator.CanReach(_world.Graph, "Crossroads_Hub", "Lords_Arena");
            List<string> unreachable = WorldReachabilityValidator.FindUnreachableRooms(_world.Graph, "Crossroads_Hub");
            List<string> softLocks = WorldReachabilityValidator.FindSoftLockRooms(_world.Graph, "Crossroads_Hub");
            _validationReport = $"BossReachable={bossReachable} | Unreachable={unreachable.Count} | SoftLocks={softLocks.Count}";
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 7)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (_world == null)
                return;

            GUILayout.BeginArea(new Rect(16, 16, 480, 380), "World Playground Sample", GUI.skin.window);
            GUILayout.Label($"Current Room: {_world.CurrentRoomId}  |  Visited Rooms: {_world.Flags.VisitedRoomCount}");
            GUILayout.Label($"Hall Enemy: {(_hallEnemy.Defeated ? "Defeated" : "Alive")} (Resets: {_hallEnemy.ResetCount})");
            GUILayout.Label($"Validator: {_validationReport}");
            GUILayout.Label("Keys (New Input System): [1] Hub | [2] Hall | [3] BossGate | [G] Grant WallJump | [H] Hazard | [K] Death");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enter Hall")) _world.TransitionTo("Mantis_Hall", "WestDoor");
            if (GUILayout.Button("Defeat Enemy")) _hallEnemy.Defeated = true;
            if (GUILayout.Button("Set Safe Ground")) _world.Checkpoints.RecordSafeGround(_world.CurrentRoomId, new NumVector3(4f, 0f, 0f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Grant WallJump")) _abilities.Add("WallJump");
            if (GUILayout.Button("Try BossGate"))
            {
                if (!_world.TryTraverseExit("ArenaGate", _abilities.Contains))
                    AppendLog("[Gate] ArenaGate locked (needs WallJump)");
            }
            if (GUILayout.Button("Hazard Respawn")) _world.RespawnFromHazard();
            if (GUILayout.Button("Death Respawn")) _world.RespawnFromDeath();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            for (int i = 0; i < _log.Count; i++)
                GUILayout.Label(_log[i]);

            GUILayout.EndArea();
        }
    }
}

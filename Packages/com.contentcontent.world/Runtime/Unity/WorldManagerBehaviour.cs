using System;
using System.Collections.Generic;
using ContentContent.Core.Events.Unity;
using UnityEngine;
using UnityEngine.Events;
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.World.Unity
{
    /// <summary>
    /// Thin MonoBehaviour manager coordinating pure-C# <see cref="RoomLifecycleController"/>,
    /// <see cref="WorldGraph"/>, <see cref="WorldFlagStore"/>, and <see cref="CheckpointService"/>
    /// alongside scene spawn points and optional player teleportation.
    /// </summary>
    [AddComponentMenu("ContentContent/World/World Manager")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class WorldManagerBehaviour : MonoBehaviour
    {
        [Header("Core Integration")]
        [Tooltip("Optional EventBusBehaviour receiving world events (room enter/exit, flags, checkpoints, respawn).")]
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;

        [Tooltip("Optional WorldGraphAsset preloaded into WorldGraph on startup for reachability & exit checks.")]
        [SerializeField] private WorldGraphAsset _worldGraphAsset;

        [Tooltip("Player Transform teleported to RoomSpawnPoints on transitions and respawns.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Initial State (Optional)")]
        [Tooltip("Starting room id entered automatically on Start().")]
        [SerializeField] private string _initialRoomId = string.Empty;

        [Tooltip("Starting spawn point id inside Initial Room Id.")]
        [SerializeField] private string _initialSpawnPointId = string.Empty;

        [Header("Events")]
        [SerializeField] private UnityEvent<RespawnRequestedEvent> _onRespawnRequested = new UnityEvent<RespawnRequestedEvent>();
        [SerializeField] private UnityEvent<string> _onRoomEntered = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onCheckpointActivated = new UnityEvent<string>();

        private readonly Dictionary<string, RoomSpawnPoint> _spawnPoints = new Dictionary<string, RoomSpawnPoint>(StringComparer.Ordinal);
        private RoomLifecycleController _controller;

        public WorldGraphAsset WorldGraphAsset
        {
            get => _worldGraphAsset;
            set => _worldGraphAsset = value;
        }

        public Transform PlayerTransform
        {
            get => _playerTransform;
            set => _playerTransform = value;
        }

        public UnityEvent<RespawnRequestedEvent> OnRespawnRequested => _onRespawnRequested ?? (_onRespawnRequested = new UnityEvent<RespawnRequestedEvent>());
        public UnityEvent<string> OnRoomEntered => _onRoomEntered ?? (_onRoomEntered = new UnityEvent<string>());
        public UnityEvent<string> OnCheckpointActivated => _onCheckpointActivated ?? (_onCheckpointActivated = new UnityEvent<string>());

        public WorldGraph Graph => Controller.Graph;
        public WorldFlagStore Flags => Controller.Flags;
        public CheckpointService Checkpoints => Controller.Checkpoints;

        public RoomLifecycleController Controller
        {
            get
            {
                if (_controller == null)
                {
                    var bus = _eventBusBehaviour != null ? _eventBusBehaviour.Bus : null;
                    WorldGraph graph = _worldGraphAsset != null ? _worldGraphAsset.BuildGraph() : new WorldGraph();
                    var flags = new WorldFlagStore(bus);
                    var checkpoints = new CheckpointService(bus);
                    _controller = new RoomLifecycleController(graph, flags, checkpoints, bus);
                    _controller.OnRoomEntered += HandleRoomEntered;
                    _controller.Checkpoints.OnCheckpointActivated += HandleCheckpointActivated;
                }

                return _controller;
            }
        }

        private void Awake()
        {
            _ = Controller;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_initialRoomId) && string.IsNullOrEmpty(Controller.CurrentRoomId))
            {
                TransitionToRoom(_initialRoomId, _initialSpawnPointId, resetTargetRoom: true);
            }
        }

        private void OnDestroy()
        {
            if (_controller == null)
                return;

            _controller.OnRoomEntered -= HandleRoomEntered;
            _controller.Checkpoints.OnCheckpointActivated -= HandleCheckpointActivated;
        }

        public void ResetCurrentRoom()
        {
            if (!string.IsNullOrEmpty(Controller.CurrentRoomId))
            {
                Controller.ResetRoom(Controller.CurrentRoomId);
            }
        }

        private void HandleRoomEntered(RoomEnteredEvent evt) => _onRoomEntered?.Invoke(evt.RoomId);

        private void HandleCheckpointActivated(CheckpointActivatedEvent evt) => _onCheckpointActivated?.Invoke(evt.Checkpoint.CheckpointId);

        public void RegisterSpawnPoint(RoomSpawnPoint spawnPoint)
        {
            if (spawnPoint == null || string.IsNullOrEmpty(spawnPoint.RoomId))
                return;

            string key = BuildSpawnKey(spawnPoint.RoomId, spawnPoint.SpawnPointId);
            _spawnPoints[key] = spawnPoint;
        }

        public void UnregisterSpawnPoint(RoomSpawnPoint spawnPoint)
        {
            if (spawnPoint == null || string.IsNullOrEmpty(spawnPoint.RoomId))
                return;

            string key = BuildSpawnKey(spawnPoint.RoomId, spawnPoint.SpawnPointId);
            if (_spawnPoints.TryGetValue(key, out RoomSpawnPoint existing) && existing == spawnPoint)
            {
                _spawnPoints.Remove(key);
            }
        }

        public bool TryGetSpawnPoint(string roomId, string spawnPointId, out RoomSpawnPoint spawnPoint)
        {
            return _spawnPoints.TryGetValue(BuildSpawnKey(roomId, spawnPointId), out spawnPoint);
        }

        public void TransitionToRoom(string roomId, string spawnPointId = null, bool resetTargetRoom = true)
        {
            Controller.TransitionTo(roomId, spawnPointId, resetTargetRoom);

            if (_playerTransform != null && TryGetSpawnPoint(roomId, spawnPointId, out RoomSpawnPoint point))
            {
                _playerTransform.position = point.transform.position;
            }
        }

        public void ActivateCheckpoint(string checkpointId, string roomId, string spawnPointId, Vector3 position)
        {
            var data = new CheckpointData(
                checkpointId,
                roomId,
                spawnPointId,
                new NumVector3(position.x, position.y, position.z));
            Checkpoints.ActivateCheckpoint(data);
        }

        public void RecordSafeGround(Vector3 position)
        {
            string roomId = Controller.CurrentRoomId;
            if (!string.IsNullOrEmpty(roomId))
            {
                Checkpoints.RecordSafeGround(roomId, new NumVector3(position.x, position.y, position.z));
            }
        }

        public RespawnRequestedEvent TriggerHazardRespawn()
        {
            RespawnRequestedEvent evt = Controller.RespawnFromHazard();
            ApplyRespawnPosition(evt);
            _onRespawnRequested?.Invoke(evt);
            return evt;
        }

        public RespawnRequestedEvent TriggerDeathRespawn()
        {
            RespawnRequestedEvent evt = Controller.RespawnFromDeath();
            ApplyRespawnPosition(evt);
            _onRespawnRequested?.Invoke(evt);
            return evt;
        }

        private void ApplyRespawnPosition(RespawnRequestedEvent evt)
        {
            if (_playerTransform == null)
                return;

            if (!string.IsNullOrEmpty(evt.SpawnPointId) &&
                TryGetSpawnPoint(evt.RoomId, evt.SpawnPointId, out RoomSpawnPoint spawn))
            {
                _playerTransform.position = spawn.transform.position;
            }
            else
            {
                _playerTransform.position = new Vector3(evt.Position.X, evt.Position.Y, evt.Position.Z);
            }
        }

        private static string BuildSpawnKey(string roomId, string spawnPointId)
        {
            return (roomId ?? string.Empty) + "::" + (spawnPointId ?? string.Empty);
        }
    }
}

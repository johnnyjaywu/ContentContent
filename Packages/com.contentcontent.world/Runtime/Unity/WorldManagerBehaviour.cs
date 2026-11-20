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
    public class WorldManagerBehaviour : MonoBehaviour
    {
        [Header("Core Integration")]
        [SerializeField] private EventBusBehaviour _eventBusBehaviour;
        [SerializeField] private Transform _playerTransform;

        [Header("Initial State (Optional)")]
        [SerializeField] private string _initialRoomId = string.Empty;
        [SerializeField] private string _initialSpawnPointId = string.Empty;

        [Header("Events")]
        [SerializeField] private UnityEvent<RespawnRequestedEvent> _onRespawnRequested;

        private readonly Dictionary<string, RoomSpawnPoint> _spawnPoints = new Dictionary<string, RoomSpawnPoint>(StringComparer.Ordinal);
        private RoomLifecycleController _controller;

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
                    _controller = new RoomLifecycleController(
                        new WorldGraph(),
                        new WorldFlagStore(bus),
                        new CheckpointService(bus),
                        bus);
                }

                return _controller;
            }
        }

        public UnityEvent<RespawnRequestedEvent> OnRespawnRequested => _onRespawnRequested;

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

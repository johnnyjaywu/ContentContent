using System;
using System.Collections.Generic;
using ContentContent.Core.Events;

namespace ContentContent.World
{
    /// <summary>
    /// Coordinates room transitions, visited tracking, exit validation, deterministic
    /// <see cref="IResettable"/> resets, and hazard/death respawns.
    /// </summary>
    public sealed class RoomLifecycleController
    {
        private readonly Dictionary<string, List<IResettable>> _resettables = new Dictionary<string, List<IResettable>>(StringComparer.Ordinal);
        private readonly IEventBus _eventBus;

        public WorldGraph Graph { get; }
        public WorldFlagStore Flags { get; }
        public CheckpointService Checkpoints { get; }

        public string CurrentRoomId { get; private set; } = string.Empty;
        public string CurrentSpawnPointId { get; private set; } = string.Empty;

        public event Action<RoomExitedEvent> OnRoomExited;
        public event Action<RoomEnteredEvent> OnRoomEntered;

        public RoomLifecycleController(
            WorldGraph graph = null,
            WorldFlagStore flags = null,
            CheckpointService checkpoints = null,
            IEventBus eventBus = null)
        {
            _eventBus = eventBus;
            Graph = graph ?? new WorldGraph();
            Flags = flags ?? new WorldFlagStore(eventBus);
            Checkpoints = checkpoints ?? new CheckpointService(eventBus);
        }

        public void RegisterResettable(string roomId, IResettable target)
        {
            if (string.IsNullOrEmpty(roomId) || target == null)
                return;

            if (!_resettables.TryGetValue(roomId, out List<IResettable> list))
            {
                list = new List<IResettable>();
                _resettables[roomId] = list;
            }

            if (!list.Contains(target))
            {
                list.Add(target);
            }
        }

        public void UnregisterResettable(string roomId, IResettable target)
        {
            if (string.IsNullOrEmpty(roomId) || target == null)
                return;

            if (_resettables.TryGetValue(roomId, out List<IResettable> list))
            {
                list.Remove(target);
            }
        }

        public void ResetRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || !_resettables.TryGetValue(roomId, out List<IResettable> list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                list[i]?.ResetState();
            }
        }

        /// <summary>
        /// Transitions into <paramref name="roomId"/> at <paramref name="spawnPointId"/>,
        /// recording room discovery and resetting target room entities when requested.
        /// </summary>
        public void TransitionTo(string roomId, string spawnPointId = null, bool resetTargetRoom = true)
        {
            if (string.IsNullOrEmpty(roomId))
                throw new ArgumentException("RoomId must not be null or empty.", nameof(roomId));

            string previousRoom = CurrentRoomId;
            string targetSpawn = spawnPointId ?? string.Empty;
            bool roomChanged = !string.Equals(previousRoom, roomId, StringComparison.Ordinal);

            if (roomChanged && !string.IsNullOrEmpty(previousRoom))
            {
                var exited = new RoomExitedEvent(previousRoom, roomId, targetSpawn);
                OnRoomExited?.Invoke(exited);
                _eventBus?.Publish(in exited);
            }

            if (roomChanged)
            {
                Checkpoints.ClearSafeGround();
            }

            CurrentRoomId = roomId;
            CurrentSpawnPointId = targetSpawn;

            if (resetTargetRoom)
            {
                ResetRoom(roomId);
            }

            if (roomChanged)
            {
                bool firstVisit = Flags.MarkRoomVisited(roomId);
                var entered = new RoomEnteredEvent(previousRoom, roomId, targetSpawn, firstVisit);
                OnRoomEntered?.Invoke(entered);
                _eventBus?.Publish(in entered);
            }
        }

        /// <summary>
        /// Attempts to traverse <paramref name="exitId"/> out of <see cref="CurrentRoomId"/>,
        /// validating required abilities and <see cref="WorldFlagStore"/> flags.
        /// </summary>
        public bool TryTraverseExit(string exitId, Func<string, bool> hasAbility = null, bool resetTargetRoom = true)
        {
            if (string.IsNullOrEmpty(CurrentRoomId) || !Graph.TryFindExit(CurrentRoomId, exitId, out RoomExit exit))
                return false;

            if (!exit.IsUnlocked(hasAbility, flag => Flags.GetBool(flag)))
                return false;

            TransitionTo(exit.DestinationRoomId, exit.DestinationSpawnId, resetTargetRoom);
            return true;
        }

        /// <summary>
        /// Triggers a non-fatal hazard recovery (returns to safe ground without resetting the room,
        /// or falls back to the active checkpoint if no safe ground is available).
        /// </summary>
        public RespawnRequestedEvent RespawnFromHazard()
        {
            RespawnRequestedEvent evt = Checkpoints.RequestHazardRespawn(CurrentRoomId);
            if (evt.ResetRoomEntities)
            {
                TransitionTo(evt.RoomId, evt.SpawnPointId, resetTargetRoom: true);
            }

            return evt;
        }

        /// <summary>
        /// Triggers a full death respawn back to the active checkpoint and resets the target room.
        /// </summary>
        public RespawnRequestedEvent RespawnFromDeath()
        {
            RespawnRequestedEvent evt = Checkpoints.RequestDeathRespawn();
            TransitionTo(evt.RoomId, evt.SpawnPointId, resetTargetRoom: true);
            return evt;
        }
    }
}

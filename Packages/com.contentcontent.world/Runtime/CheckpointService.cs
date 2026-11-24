using System;
using System.Numerics;
using ContentContent.Core.Events;

namespace ContentContent.World
{
    /// <summary>
    /// Tracks both hard checkpoints (sanctuaries, shrines, save points) and transient safe-ground anchors
    /// (last valid footing before falling into hazards or pits).
    /// </summary>
    public sealed class CheckpointService
    {
        private readonly IEventBus _eventBus;

        public CheckpointData? ActiveCheckpoint { get; private set; }
        public bool HasActiveCheckpoint => ActiveCheckpoint.HasValue;

        public string LastSafeGroundRoomId { get; private set; } = string.Empty;
        public Vector3 LastSafeGroundPosition { get; private set; }
        public bool HasSafeGround { get; private set; }

        public event Action<CheckpointActivatedEvent> OnCheckpointActivated;
        public event Action<SafeGroundUpdatedEvent> OnSafeGroundUpdated;
        public event Action<RespawnRequestedEvent> OnRespawnRequested;

        public CheckpointService(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void ActivateCheckpoint(CheckpointData checkpoint)
        {
            if (string.IsNullOrEmpty(checkpoint.RoomId))
                throw new ArgumentException("Checkpoint RoomId must not be null or empty.", nameof(checkpoint));

            ActiveCheckpoint = checkpoint;
            var evt = new CheckpointActivatedEvent(checkpoint);
            OnCheckpointActivated?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }

        public void RecordSafeGround(string roomId, Vector3 position)
        {
            if (string.IsNullOrEmpty(roomId))
                return;

            LastSafeGroundRoomId = roomId;
            LastSafeGroundPosition = position;
            HasSafeGround = true;

            var evt = new SafeGroundUpdatedEvent(roomId, position);
            OnSafeGroundUpdated?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }

        public void ClearSafeGround()
        {
            LastSafeGroundRoomId = string.Empty;
            LastSafeGroundPosition = Vector3.Zero;
            HasSafeGround = false;
        }

        /// <summary>
        /// Resolves a hazard recovery target: returns safe-ground inside <paramref name="currentRoomId"/>
        /// without resetting room entities, or falls back to <see cref="ActiveCheckpoint"/> if no safe ground exists.
        /// </summary>
        public RespawnRequestedEvent RequestHazardRespawn(string currentRoomId)
        {
            RespawnRequestedEvent evt;
            if (HasSafeGround && string.Equals(LastSafeGroundRoomId, currentRoomId, StringComparison.Ordinal))
            {
                evt = new RespawnRequestedEvent(
                    RespawnKind.HazardSafeGround,
                    LastSafeGroundRoomId,
                    string.Empty,
                    LastSafeGroundPosition,
                    resetRoomEntities: false);
            }
            else if (ActiveCheckpoint.HasValue)
            {
                CheckpointData cp = ActiveCheckpoint.Value;
                evt = new RespawnRequestedEvent(
                    RespawnKind.HardCheckpoint,
                    cp.RoomId,
                    cp.SpawnPointId,
                    cp.Position,
                    resetRoomEntities: true);
            }
            else
            {
                throw new InvalidOperationException("Cannot respawn from hazard: neither safe ground nor an active checkpoint is set.");
            }

            OnRespawnRequested?.Invoke(evt);
            _eventBus?.Publish(in evt);
            return evt;
        }

        /// <summary>
        /// Resolves a full death respawn back to <see cref="ActiveCheckpoint"/> and requests room entity reset.
        /// </summary>
        public RespawnRequestedEvent RequestDeathRespawn()
        {
            if (!ActiveCheckpoint.HasValue)
                throw new InvalidOperationException("Cannot respawn from death: no active checkpoint has been set.");

            CheckpointData cp = ActiveCheckpoint.Value;
            var evt = new RespawnRequestedEvent(
                RespawnKind.HardCheckpoint,
                cp.RoomId,
                cp.SpawnPointId,
                cp.Position,
                resetRoomEntities: true);

            OnRespawnRequested?.Invoke(evt);
            _eventBus?.Publish(in evt);
            return evt;
        }

        public void Reset()
        {
            ActiveCheckpoint = null;
            ClearSafeGround();
        }
    }
}

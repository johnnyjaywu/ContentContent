using System;
using System.Numerics;

namespace ContentContent.World
{
    /// <summary>Distinguishes quick local hazard recovery from full hard-checkpoint respawn.</summary>
    public enum RespawnKind
    {
        HazardSafeGround = 0,
        HardCheckpoint = 1
    }

    /// <summary>Immutable descriptor for a hard checkpoint (sanctuary, shrine, chapter start).</summary>
    public readonly struct CheckpointData : IEquatable<CheckpointData>
    {
        public string CheckpointId { get; }
        public string RoomId { get; }
        public string SpawnPointId { get; }
        public Vector3 Position { get; }

        public CheckpointData(string checkpointId, string roomId, string spawnPointId, Vector3 position)
        {
            CheckpointId = checkpointId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            SpawnPointId = spawnPointId ?? string.Empty;
            Position = position;
        }

        public bool Equals(CheckpointData other)
        {
            return string.Equals(CheckpointId, other.CheckpointId, StringComparison.Ordinal) &&
                   string.Equals(RoomId, other.RoomId, StringComparison.Ordinal) &&
                   string.Equals(SpawnPointId, other.SpawnPointId, StringComparison.Ordinal) &&
                   Position.Equals(other.Position);
        }

        public override bool Equals(object obj) => obj is CheckpointData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(CheckpointId, RoomId, SpawnPointId, Position);
    }

    /// <summary>Published when the player enters a world room.</summary>
    public readonly struct RoomEnteredEvent
    {
        public string PreviousRoomId { get; }
        public string RoomId { get; }
        public string SpawnPointId { get; }
        public bool IsFirstVisit { get; }

        public RoomEnteredEvent(string previousRoomId, string roomId, string spawnPointId, bool isFirstVisit)
        {
            PreviousRoomId = previousRoomId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            SpawnPointId = spawnPointId ?? string.Empty;
            IsFirstVisit = isFirstVisit;
        }
    }

    /// <summary>Published when the player leaves a world room.</summary>
    public readonly struct RoomExitedEvent
    {
        public string RoomId { get; }
        public string DestinationRoomId { get; }
        public string TargetSpawnPointId { get; }

        public RoomExitedEvent(string roomId, string destinationRoomId, string targetSpawnPointId)
        {
            RoomId = roomId ?? string.Empty;
            DestinationRoomId = destinationRoomId ?? string.Empty;
            TargetSpawnPointId = targetSpawnPointId ?? string.Empty;
        }
    }

    /// <summary>Published when a hard checkpoint is activated.</summary>
    public readonly struct CheckpointActivatedEvent
    {
        public CheckpointData Checkpoint { get; }

        public CheckpointActivatedEvent(CheckpointData checkpoint)
        {
            Checkpoint = checkpoint;
        }
    }

    /// <summary>Published when a safe grounded respawn anchor is recorded inside a room.</summary>
    public readonly struct SafeGroundUpdatedEvent
    {
        public string RoomId { get; }
        public Vector3 Position { get; }

        public SafeGroundUpdatedEvent(string roomId, Vector3 position)
        {
            RoomId = roomId ?? string.Empty;
            Position = position;
        }
    }

    /// <summary>Published when either hazard recovery or death respawn is triggered.</summary>
    public readonly struct RespawnRequestedEvent
    {
        public RespawnKind Kind { get; }
        public string RoomId { get; }
        public string SpawnPointId { get; }
        public Vector3 Position { get; }
        public bool ResetRoomEntities { get; }

        public RespawnRequestedEvent(
            RespawnKind kind,
            string roomId,
            string spawnPointId,
            Vector3 position,
            bool resetRoomEntities)
        {
            Kind = kind;
            RoomId = roomId ?? string.Empty;
            SpawnPointId = spawnPointId ?? string.Empty;
            Position = position;
            ResetRoomEntities = resetRoomEntities;
        }
    }

    /// <summary>Published when a persistent flag changes in <see cref="WorldFlagStore"/>.</summary>
    public readonly struct WorldFlagChangedEvent
    {
        public string Key { get; }
        public WorldFlagValue PreviousValue { get; }
        public WorldFlagValue NewValue { get; }

        public WorldFlagChangedEvent(string key, WorldFlagValue previousValue, WorldFlagValue newValue)
        {
            Key = key ?? string.Empty;
            PreviousValue = previousValue;
            NewValue = newValue;
        }
    }
}

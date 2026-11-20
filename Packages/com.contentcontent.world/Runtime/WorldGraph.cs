using System;
using System.Collections.Generic;

namespace ContentContent.World
{
    /// <summary>Directed exit connecting a <see cref="RoomNode"/> to another room with optional ability/flag gates.</summary>
    public sealed class RoomExit
    {
        private readonly List<string> _requiredAbilities;
        private readonly List<string> _requiredFlags;

        public string ExitId { get; }
        public string DestinationRoomId { get; }
        public string DestinationSpawnId { get; }
        public IReadOnlyList<string> RequiredAbilities => _requiredAbilities;
        public IReadOnlyList<string> RequiredFlags => _requiredFlags;

        public RoomExit(
            string exitId,
            string destinationRoomId,
            string destinationSpawnId = null,
            IEnumerable<string> requiredAbilities = null,
            IEnumerable<string> requiredFlags = null)
        {
            if (string.IsNullOrEmpty(exitId))
                throw new ArgumentException("ExitId must not be null or empty.", nameof(exitId));
            if (string.IsNullOrEmpty(destinationRoomId))
                throw new ArgumentException("DestinationRoomId must not be null or empty.", nameof(destinationRoomId));

            ExitId = exitId;
            DestinationRoomId = destinationRoomId;
            DestinationSpawnId = destinationSpawnId ?? string.Empty;
            _requiredAbilities = requiredAbilities != null ? new List<string>(requiredAbilities) : new List<string>();
            _requiredFlags = requiredFlags != null ? new List<string>(requiredFlags) : new List<string>();
        }

        /// <summary>Returns true when all required abilities and flags are satisfied.</summary>
        public bool IsUnlocked(Func<string, bool> hasAbility, Func<string, bool> hasFlag)
        {
            for (int i = 0; i < _requiredAbilities.Count; i++)
            {
                if (hasAbility == null || !hasAbility(_requiredAbilities[i]))
                    return false;
            }

            for (int i = 0; i < _requiredFlags.Count; i++)
            {
                if (hasFlag == null || !hasFlag(_requiredFlags[i]))
                    return false;
            }

            return true;
        }
    }

    /// <summary>Graph node representing a discrete world room or zone.</summary>
    public sealed class RoomNode
    {
        private readonly List<RoomExit> _exits = new List<RoomExit>();
        private readonly List<string> _grantedAbilities = new List<string>();
        private readonly List<string> _grantedFlags = new List<string>();

        public string RoomId { get; }
        public string DisplayName { get; }
        public string AreaId { get; }
        public bool HasCheckpoint { get; }

        public IReadOnlyList<RoomExit> Exits => _exits;
        public IReadOnlyList<string> GrantedAbilities => _grantedAbilities;
        public IReadOnlyList<string> GrantedFlags => _grantedFlags;

        public RoomNode(
            string roomId,
            string displayName = null,
            string areaId = null,
            bool hasCheckpoint = false)
        {
            if (string.IsNullOrEmpty(roomId))
                throw new ArgumentException("RoomId must not be null or empty.", nameof(roomId));

            RoomId = roomId;
            DisplayName = string.IsNullOrEmpty(displayName) ? roomId : displayName;
            AreaId = areaId ?? string.Empty;
            HasCheckpoint = hasCheckpoint;
        }

        public RoomNode AddExit(RoomExit exit)
        {
            if (exit == null)
                throw new ArgumentNullException(nameof(exit));

            _exits.Add(exit);
            return this;
        }

        public RoomNode GrantAbility(string abilityId)
        {
            if (!string.IsNullOrEmpty(abilityId) && !_grantedAbilities.Contains(abilityId))
                _grantedAbilities.Add(abilityId);
            return this;
        }

        public RoomNode GrantFlag(string flagKey)
        {
            if (!string.IsNullOrEmpty(flagKey) && !_grantedFlags.Contains(flagKey))
                _grantedFlags.Add(flagKey);
            return this;
        }
    }

    /// <summary>
    /// Directed room connectivity graph supporting ability-gated doors, one-way drops,
    /// and progression validation via <see cref="WorldReachabilityValidator"/>.
    /// </summary>
    public sealed class WorldGraph
    {
        private readonly Dictionary<string, RoomNode> _rooms = new Dictionary<string, RoomNode>(StringComparer.Ordinal);

        public IReadOnlyCollection<RoomNode> Rooms => _rooms.Values;

        public RoomNode AddRoom(RoomNode room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            _rooms[room.RoomId] = room;
            return room;
        }

        public RoomNode AddRoom(
            string roomId,
            string displayName = null,
            string areaId = null,
            bool hasCheckpoint = false)
        {
            var node = new RoomNode(roomId, displayName, areaId, hasCheckpoint);
            _rooms[roomId] = node;
            return node;
        }

        public void AddExit(string fromRoomId, RoomExit exit)
        {
            if (!_rooms.TryGetValue(fromRoomId, out RoomNode room))
                throw new KeyNotFoundException($"Room '{fromRoomId}' not found in WorldGraph.");

            room.AddExit(exit);
        }

        public void AddBidirectionalConnection(
            string roomA,
            string roomB,
            string exitIdAtoB = null,
            string exitIdBtoA = null,
            IEnumerable<string> requiredAbilities = null,
            IEnumerable<string> requiredFlags = null,
            string spawnInA = null,
            string spawnInB = null)
        {
            string abId = string.IsNullOrEmpty(exitIdAtoB) ? $"{roomA}->{roomB}" : exitIdAtoB;
            string baId = string.IsNullOrEmpty(exitIdBtoA) ? $"{roomB}->{roomA}" : exitIdBtoA;

            AddExit(roomA, new RoomExit(abId, roomB, spawnInB, requiredAbilities, requiredFlags));
            AddExit(roomB, new RoomExit(baId, roomA, spawnInA, requiredAbilities, requiredFlags));
        }

        public bool TryGetRoom(string roomId, out RoomNode room)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                room = null;
                return false;
            }

            return _rooms.TryGetValue(roomId, out room);
        }

        public bool TryFindExit(string roomId, string exitId, out RoomExit exit)
        {
            if (TryGetRoom(roomId, out RoomNode room) && !string.IsNullOrEmpty(exitId))
            {
                for (int i = 0; i < room.Exits.Count; i++)
                {
                    if (string.Equals(room.Exits[i].ExitId, exitId, StringComparison.Ordinal))
                    {
                        exit = room.Exits[i];
                        return true;
                    }
                }
            }

            exit = null;
            return false;
        }

        public List<RoomExit> GetAvailableExits(string roomId, Func<string, bool> hasAbility = null, Func<string, bool> hasFlag = null)
        {
            var result = new List<RoomExit>();
            if (!TryGetRoom(roomId, out RoomNode room))
                return result;

            for (int i = 0; i < room.Exits.Count; i++)
            {
                RoomExit exit = room.Exits[i];
                if (exit.IsUnlocked(hasAbility, hasFlag))
                {
                    result.Add(exit);
                }
            }

            return result;
        }
    }
}

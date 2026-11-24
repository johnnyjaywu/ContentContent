using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.World.Unity
{
    public struct WorldValidationReport
    {
        public bool HasStartRoom;
        public List<string> DuplicateRoomIds;
        public List<string> UnreachableRooms;
        public List<string> SoftLockRooms;

        public bool IsValid =>
            HasStartRoom &&
            (DuplicateRoomIds == null || DuplicateRoomIds.Count == 0) &&
            (UnreachableRooms == null || UnreachableRooms.Count == 0) &&
            (SoftLockRooms == null || SoftLockRooms.Count == 0);
    }

    /// <summary>
    /// Designer-authored <see cref="ScriptableObject"/> defining rooms, ability/flag gates, and exits,
    /// with live edit-time <see cref="WorldReachabilityValidator"/> soft-lock checks.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGraph", menuName = "ContentContent/World/World Graph")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/world/quickstart")]
    public class WorldGraphAsset : ScriptableObject
    {
        [Serializable]
        public struct ExitData
        {
            [Tooltip("Identifier for this exit inside the source room.")]
            public string exitId;

            [Tooltip("Room ID entered when traversing this exit.")]
            public string destinationRoomId;

            [Tooltip("SpawnPoint ID placed inside the destination room (optional).")]
            public string destinationSpawnId;

            [Tooltip("Abilities required to pass this exit (e.g., WallJump, DoubleJump).")]
            public List<string> requiredAbilities;

            [Tooltip("WorldFlagStore boolean flags required to pass this exit (e.g., lever_pulled).")]
            public List<string> requiredFlags;
        }

        [Serializable]
        public struct RoomData
        {
            [Tooltip("Unique identifier for this room or zone.")]
            public string roomId;

            [Tooltip("Human-readable label shown on maps and debuggers.")]
            public string displayName;

            [Tooltip("Optional region or chapter grouping id.")]
            public string areaId;

            [Tooltip("Marks this room as containing a safe hard checkpoint.")]
            public bool hasCheckpoint;

            [Tooltip("Abilities earned inside this room (used by soft-lock validator).")]
            public List<string> grantedAbilities;

            [Tooltip("World flags toggled inside this room (used by soft-lock validator).")]
            public List<string> grantedFlags;

            [Tooltip("Outgoing connections from this room.")]
            public List<ExitData> exits;
        }

        [Tooltip("Starting room ID used as the origin for reachability & soft-lock validation.")]
        [SerializeField] private string _startRoomId = "Start";

        [Tooltip("Authored rooms and outgoing connections.")]
        [SerializeField] private List<RoomData> _rooms = new List<RoomData>();

        public string StartRoomId => _startRoomId;
        public IReadOnlyList<RoomData> Rooms => _rooms;

        /// <summary>
        /// Builds a pure C# <see cref="WorldGraph"/> from this asset.
        /// </summary>
        public WorldGraph BuildGraph()
        {
            var graph = new WorldGraph();
            if (_rooms == null)
                return graph;

            for (int i = 0; i < _rooms.Count; i++)
            {
                RoomData r = _rooms[i];
                if (string.IsNullOrWhiteSpace(r.roomId))
                    continue;

                string id = r.roomId.Trim();
                if (graph.TryGetRoom(id, out _))
                    continue;

                RoomNode node = graph.AddRoom(id, r.displayName, r.areaId, r.hasCheckpoint);

                if (r.grantedAbilities != null)
                {
                    for (int a = 0; a < r.grantedAbilities.Count; a++)
                    {
                        if (!string.IsNullOrWhiteSpace(r.grantedAbilities[a]))
                            node.GrantAbility(r.grantedAbilities[a].Trim());
                    }
                }

                if (r.grantedFlags != null)
                {
                    for (int f = 0; f < r.grantedFlags.Count; f++)
                    {
                        if (!string.IsNullOrWhiteSpace(r.grantedFlags[f]))
                            node.GrantFlag(r.grantedFlags[f].Trim());
                    }
                }
            }

            for (int i = 0; i < _rooms.Count; i++)
            {
                RoomData r = _rooms[i];
                if (string.IsNullOrWhiteSpace(r.roomId) || r.exits == null)
                    continue;

                string sourceId = r.roomId.Trim();
                for (int e = 0; e < r.exits.Count; e++)
                {
                    ExitData exit = r.exits[e];
                    if (string.IsNullOrWhiteSpace(exit.exitId) || string.IsNullOrWhiteSpace(exit.destinationRoomId))
                        continue;

                    graph.AddExit(
                        sourceId,
                        new RoomExit(
                            exit.exitId.Trim(),
                            exit.destinationRoomId.Trim(),
                            exit.destinationSpawnId,
                            exit.requiredAbilities,
                            exit.requiredFlags));
                }
            }

            return graph;
        }

        /// <summary>
        /// Validates duplicate IDs, unreachable rooms, and soft-lock rooms using <see cref="WorldReachabilityValidator"/>.
        /// </summary>
        public WorldValidationReport ValidateReachability(
            IEnumerable<string> initialAbilities = null,
            IEnumerable<string> initialFlags = null)
        {
            var report = new WorldValidationReport
            {
                DuplicateRoomIds = new List<string>(),
                UnreachableRooms = new List<string>(),
                SoftLockRooms = new List<string>()
            };

            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (_rooms != null)
            {
                for (int i = 0; i < _rooms.Count; i++)
                {
                    string raw = _rooms[i].roomId;
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    string trimmed = raw.Trim();
                    if (!seen.Add(trimmed) && !report.DuplicateRoomIds.Contains(trimmed))
                    {
                        report.DuplicateRoomIds.Add(trimmed);
                    }
                }
            }

            WorldGraph graph = BuildGraph();
            string start = string.IsNullOrWhiteSpace(_startRoomId) ? string.Empty : _startRoomId.Trim();
            report.HasStartRoom = !string.IsNullOrEmpty(start) && graph.TryGetRoom(start, out _);

            if (report.HasStartRoom)
            {
                report.UnreachableRooms = WorldReachabilityValidator.FindUnreachableRooms(graph, start, initialAbilities, initialFlags);
                report.SoftLockRooms = WorldReachabilityValidator.FindSoftLockRooms(graph, start, initialAbilities, initialFlags);
            }

            return report;
        }
    }
}

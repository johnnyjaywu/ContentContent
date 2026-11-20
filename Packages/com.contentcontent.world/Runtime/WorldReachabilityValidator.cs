using System;
using System.Collections.Generic;

namespace ContentContent.World
{
    /// <summary>
    /// Static BFS solver over <see cref="WorldGraph"/> states <c>(roomId, heldAbilities, heldFlags)</c>
    /// to verify critical-path reachability and catch Metroidvania soft-locks at edit/test time.
    /// </summary>
    public static class WorldReachabilityValidator
    {
        private readonly struct SearchState
        {
            public string RoomId { get; }
            public HashSet<string> Abilities { get; }
            public HashSet<string> Flags { get; }

            public SearchState(string roomId, HashSet<string> abilities, HashSet<string> flags)
            {
                RoomId = roomId;
                Abilities = abilities;
                Flags = flags;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="goalRoomId"/> can be reached from <paramref name="startRoomId"/>
        /// while respecting ability/key acquisition order.
        /// </summary>
        public static bool CanReach(
            WorldGraph graph,
            string startRoomId,
            string goalRoomId,
            IEnumerable<string> initialAbilities = null,
            IEnumerable<string> initialFlags = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (string.IsNullOrEmpty(startRoomId) || string.IsNullOrEmpty(goalRoomId))
                return false;
            if (!graph.TryGetRoom(startRoomId, out _))
                return false;

            return CanReachPredicate(
                graph,
                startRoomId,
                roomId => string.Equals(roomId, goalRoomId, StringComparison.Ordinal),
                initialAbilities,
                initialFlags);
        }

        /// <summary>
        /// Returns all room IDs in <paramref name="graph"/> that are unreachable from <paramref name="startRoomId"/>.
        /// </summary>
        public static List<string> FindUnreachableRooms(
            WorldGraph graph,
            string startRoomId,
            IEnumerable<string> initialAbilities = null,
            IEnumerable<string> initialFlags = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            var reachableRooms = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(startRoomId) && graph.TryGetRoom(startRoomId, out _))
            {
                Explore(graph, startRoomId, initialAbilities, initialFlags, reachableRooms, null);
            }

            var unreachable = new List<string>();
            foreach (RoomNode room in graph.Rooms)
            {
                if (!reachableRooms.Contains(room.RoomId))
                {
                    unreachable.Add(room.RoomId);
                }
            }

            return unreachable;
        }

        /// <summary>
        /// Finds reachable rooms from which the player can become soft-locked (unable to return to
        /// <paramref name="startRoomId"/> or reach any room with <see cref="RoomNode.HasCheckpoint"/>).
        /// Useful for catching un-escaped one-way drops.
        /// </summary>
        public static List<string> FindSoftLockRooms(
            WorldGraph graph,
            string startRoomId,
            IEnumerable<string> initialAbilities = null,
            IEnumerable<string> initialFlags = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            var softLocked = new List<string>();
            if (string.IsNullOrEmpty(startRoomId) || !graph.TryGetRoom(startRoomId, out _))
                return softLocked;

            var reachableStates = new List<SearchState>();
            var reachableRooms = new HashSet<string>(StringComparer.Ordinal);
            Explore(graph, startRoomId, initialAbilities, initialFlags, reachableRooms, reachableStates);

            var flaggedSoftLock = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < reachableStates.Count; i++)
            {
                SearchState state = reachableStates[i];
                if (flaggedSoftLock.Contains(state.RoomId))
                    continue;

                bool canReturn = CanReachPredicate(
                    graph,
                    state.RoomId,
                    candidateId =>
                        string.Equals(candidateId, startRoomId, StringComparison.Ordinal) ||
                        (graph.TryGetRoom(candidateId, out RoomNode n) && n.HasCheckpoint),
                    state.Abilities,
                    state.Flags);

                if (!canReturn)
                {
                    flaggedSoftLock.Add(state.RoomId);
                    softLocked.Add(state.RoomId);
                }
            }

            return softLocked;
        }

        private static bool CanReachPredicate(
            WorldGraph graph,
            string startRoomId,
            Func<string, bool> goalPredicate,
            IEnumerable<string> initialAbilities,
            IEnumerable<string> initialFlags)
        {
            var queue = new Queue<SearchState>();
            var visited = new HashSet<string>(StringComparer.Ordinal);

            var startAbilities = CreateSet(initialAbilities);
            var startFlags = CreateSet(initialFlags);

            if (!graph.TryGetRoom(startRoomId, out RoomNode startRoom))
                return false;

            AcquireRoomGrants(startRoom, startAbilities, startFlags);
            string startKey = BuildStateKey(startRoomId, startAbilities, startFlags);
            visited.Add(startKey);
            queue.Enqueue(new SearchState(startRoomId, startAbilities, startFlags));

            while (queue.Count > 0)
            {
                SearchState current = queue.Dequeue();
                if (goalPredicate(current.RoomId))
                    return true;

                if (!graph.TryGetRoom(current.RoomId, out RoomNode room))
                    continue;

                for (int i = 0; i < room.Exits.Count; i++)
                {
                    RoomExit exit = room.Exits[i];
                    if (!exit.IsUnlocked(current.Abilities.Contains, current.Flags.Contains))
                        continue;

                    if (!graph.TryGetRoom(exit.DestinationRoomId, out RoomNode destRoom))
                        continue;

                    var nextAbilities = new HashSet<string>(current.Abilities, StringComparer.Ordinal);
                    var nextFlags = new HashSet<string>(current.Flags, StringComparer.Ordinal);
                    AcquireRoomGrants(destRoom, nextAbilities, nextFlags);

                    string key = BuildStateKey(destRoom.RoomId, nextAbilities, nextFlags);
                    if (visited.Add(key))
                    {
                        queue.Enqueue(new SearchState(destRoom.RoomId, nextAbilities, nextFlags));
                    }
                }
            }

            return false;
        }

        private static void Explore(
            WorldGraph graph,
            string startRoomId,
            IEnumerable<string> initialAbilities,
            IEnumerable<string> initialFlags,
            HashSet<string> visitedRooms,
            List<SearchState> collectedStates)
        {
            var queue = new Queue<SearchState>();
            var visitedKeys = new HashSet<string>(StringComparer.Ordinal);

            if (!graph.TryGetRoom(startRoomId, out RoomNode startRoom))
                return;

            var startAbilities = CreateSet(initialAbilities);
            var startFlags = CreateSet(initialFlags);
            AcquireRoomGrants(startRoom, startAbilities, startFlags);

            visitedKeys.Add(BuildStateKey(startRoomId, startAbilities, startFlags));
            queue.Enqueue(new SearchState(startRoomId, startAbilities, startFlags));

            while (queue.Count > 0)
            {
                SearchState current = queue.Dequeue();
                visitedRooms.Add(current.RoomId);
                collectedStates?.Add(current);

                if (!graph.TryGetRoom(current.RoomId, out RoomNode room))
                    continue;

                for (int i = 0; i < room.Exits.Count; i++)
                {
                    RoomExit exit = room.Exits[i];
                    if (!exit.IsUnlocked(current.Abilities.Contains, current.Flags.Contains))
                        continue;

                    if (!graph.TryGetRoom(exit.DestinationRoomId, out RoomNode destRoom))
                        continue;

                    var nextAbilities = new HashSet<string>(current.Abilities, StringComparer.Ordinal);
                    var nextFlags = new HashSet<string>(current.Flags, StringComparer.Ordinal);
                    AcquireRoomGrants(destRoom, nextAbilities, nextFlags);

                    string key = BuildStateKey(destRoom.RoomId, nextAbilities, nextFlags);
                    if (visitedKeys.Add(key))
                    {
                        queue.Enqueue(new SearchState(destRoom.RoomId, nextAbilities, nextFlags));
                    }
                }
            }
        }

        private static HashSet<string> CreateSet(IEnumerable<string> source)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (source == null)
                return set;

            foreach (string item in source)
            {
                if (!string.IsNullOrEmpty(item))
                    set.Add(item);
            }

            return set;
        }

        private static void AcquireRoomGrants(RoomNode room, HashSet<string> abilities, HashSet<string> flags)
        {
            for (int i = 0; i < room.GrantedAbilities.Count; i++)
                abilities.Add(room.GrantedAbilities[i]);

            for (int i = 0; i < room.GrantedFlags.Count; i++)
                flags.Add(room.GrantedFlags[i]);
        }

        private static string BuildStateKey(string roomId, HashSet<string> abilities, HashSet<string> flags)
        {
            var sortedAbilities = new List<string>(abilities);
            sortedAbilities.Sort(StringComparer.Ordinal);

            var sortedFlags = new List<string>(flags);
            sortedFlags.Sort(StringComparer.Ordinal);

            return roomId + "|A:" + string.Join(",", sortedAbilities) + "|F:" + string.Join(",", sortedFlags);
        }
    }
}

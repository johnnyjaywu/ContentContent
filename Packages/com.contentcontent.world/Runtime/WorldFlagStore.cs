using System;
using System.Collections.Generic;
using ContentContent.Core.Events;

namespace ContentContent.World
{
    /// <summary>Value type stored in <see cref="WorldFlagStore"/>.</summary>
    public enum WorldFlagType
    {
        None = 0,
        Bool = 1,
        Int = 2,
        String = 3
    }

    /// <summary>Immutable typed union value for persistent world flags.</summary>
    public readonly struct WorldFlagValue : IEquatable<WorldFlagValue>
    {
        public static WorldFlagValue None => default;

        public WorldFlagType Type { get; }
        public bool BoolValue { get; }
        public int IntValue { get; }
        public string StringValue { get; }

        private WorldFlagValue(WorldFlagType type, bool boolValue, int intValue, string stringValue)
        {
            Type = type;
            BoolValue = boolValue;
            IntValue = intValue;
            StringValue = stringValue ?? string.Empty;
        }

        public static WorldFlagValue FromBool(bool value) => new WorldFlagValue(WorldFlagType.Bool, value, value ? 1 : 0, null);
        public static WorldFlagValue FromInt(int value) => new WorldFlagValue(WorldFlagType.Int, value != 0, value, null);
        public static WorldFlagValue FromString(string value) => new WorldFlagValue(WorldFlagType.String, !string.IsNullOrEmpty(value), 0, value);

        public bool AsBool(bool defaultValue = false) => Type == WorldFlagType.None ? defaultValue : BoolValue;
        public int AsInt(int defaultValue = 0) => Type == WorldFlagType.None ? defaultValue : IntValue;
        public string AsString(string defaultValue = "") => Type == WorldFlagType.None ? defaultValue : StringValue;

        public bool Equals(WorldFlagValue other)
        {
            return Type == other.Type &&
                   BoolValue == other.BoolValue &&
                   IntValue == other.IntValue &&
                   string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is WorldFlagValue other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Type, BoolValue, IntValue, StringValue);
    }

    /// <summary>
    /// Stores persistent world flags (opened shortcuts, defeated bosses, collected secrets)
    /// and visited room IDs (minimap discovery) with snapshot import/export.
    /// </summary>
    public sealed class WorldFlagStore
    {
        private readonly Dictionary<string, WorldFlagValue> _flags = new Dictionary<string, WorldFlagValue>(StringComparer.Ordinal);
        private readonly HashSet<string> _visitedRooms = new HashSet<string>(StringComparer.Ordinal);
        private readonly IEventBus _eventBus;

        /// <summary>Number of stored flags.</summary>
        public int FlagCount => _flags.Count;

        /// <summary>Number of visited room IDs.</summary>
        public int VisitedRoomCount => _visitedRooms.Count;

        /// <summary>Fired whenever any flag is created, modified, or removed.</summary>
        public event Action<WorldFlagChangedEvent> OnFlagChanged;

        public WorldFlagStore(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void SetBool(string key, bool value) => SetValue(key, WorldFlagValue.FromBool(value));
        public bool GetBool(string key, bool defaultValue = false) => TryGetValue(key, out WorldFlagValue v) ? v.AsBool(defaultValue) : defaultValue;

        public void SetInt(string key, int value) => SetValue(key, WorldFlagValue.FromInt(value));
        public int GetInt(string key, int defaultValue = 0) => TryGetValue(key, out WorldFlagValue v) ? v.AsInt(defaultValue) : defaultValue;

        public void SetString(string key, string value) => SetValue(key, WorldFlagValue.FromString(value));
        public string GetString(string key, string defaultValue = "") => TryGetValue(key, out WorldFlagValue v) ? v.AsString(defaultValue) : defaultValue;

        public bool HasFlag(string key) => !string.IsNullOrEmpty(key) && _flags.ContainsKey(key);

        public bool TryGetValue(string key, out WorldFlagValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = WorldFlagValue.None;
                return false;
            }

            return _flags.TryGetValue(key, out value);
        }

        public bool RemoveFlag(string key)
        {
            if (string.IsNullOrEmpty(key) || !_flags.TryGetValue(key, out WorldFlagValue prev))
                return false;

            _flags.Remove(key);
            PublishChange(key, prev, WorldFlagValue.None);
            return true;
        }

        /// <summary>Marks <paramref name="roomId"/> as visited; returns true if this was the first visit.</summary>
        public bool MarkRoomVisited(string roomId)
        {
            if (string.IsNullOrEmpty(roomId))
                return false;

            return _visitedRooms.Add(roomId);
        }

        public bool HasVisitedRoom(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && _visitedRooms.Contains(roomId);
        }

        public IReadOnlyCollection<string> GetVisitedRooms() => _visitedRooms;

        public Dictionary<string, WorldFlagValue> ExportFlags()
        {
            return new Dictionary<string, WorldFlagValue>(_flags, StringComparer.Ordinal);
        }

        public void ImportFlags(IDictionary<string, WorldFlagValue> snapshot, bool clearExisting = true)
        {
            if (clearExisting)
                _flags.Clear();

            if (snapshot == null)
                return;

            foreach (KeyValuePair<string, WorldFlagValue> kvp in snapshot)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value.Type != WorldFlagType.None)
                {
                    _flags[kvp.Key] = kvp.Value;
                }
            }
        }

        public List<string> ExportVisitedRooms()
        {
            return new List<string>(_visitedRooms);
        }

        public void ImportVisitedRooms(IEnumerable<string> roomIds, bool clearExisting = true)
        {
            if (clearExisting)
                _visitedRooms.Clear();

            if (roomIds == null)
                return;

            foreach (string roomId in roomIds)
            {
                if (!string.IsNullOrEmpty(roomId))
                {
                    _visitedRooms.Add(roomId);
                }
            }
        }

        public void Clear()
        {
            _flags.Clear();
            _visitedRooms.Clear();
        }

        private void SetValue(string key, WorldFlagValue value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Flag key must not be null or empty.", nameof(key));

            _flags.TryGetValue(key, out WorldFlagValue prev);
            if (prev.Equals(value))
                return;

            _flags[key] = value;
            PublishChange(key, prev, value);
        }

        private void PublishChange(string key, WorldFlagValue previous, WorldFlagValue next)
        {
            var evt = new WorldFlagChangedEvent(key, previous, next);
            OnFlagChanged?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }
    }
}

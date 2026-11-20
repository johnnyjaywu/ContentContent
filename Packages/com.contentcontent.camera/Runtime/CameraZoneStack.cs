using System;
using System.Collections.Generic;
using ContentContent.Core.Events;

namespace ContentContent.Camera
{
    /// <summary>
    /// Priority stack managing overlapping camera zones (rooms, boss arenas, tight corridors).
    /// Higher <see cref="CameraZoneConfig.Priority"/> wins; equal priorities resolve to the most recently entered zone.
    /// </summary>
    public sealed class CameraZoneStack
    {
        private readonly List<CameraZoneConfig> _zones = new List<CameraZoneConfig>();
        private readonly IEventBus _eventBus;

        /// <summary>Currently winning camera zone, or null if no zones are active.</summary>
        public CameraZoneConfig ActiveZone { get; private set; }

        /// <summary>Number of active zones currently in the stack.</summary>
        public int Count => _zones.Count;

        /// <summary>Fired whenever the winning <see cref="ActiveZone"/> changes.</summary>
        public event Action<CameraZoneChangedEvent> OnActiveZoneChanged;

        public CameraZoneStack(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// Registers or refreshes a camera zone on the stack and re-evaluates the active zone.
        /// </summary>
        public void EnterZone(CameraZoneConfig zone)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));

            if (string.IsNullOrEmpty(zone.ZoneId))
                throw new ArgumentException("ZoneId must not be null or empty.", nameof(zone));

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_zones[i].ZoneId, zone.ZoneId, StringComparison.Ordinal))
                {
                    _zones.RemoveAt(i);
                }
            }

            _zones.Add(zone);
            ReevaluateActiveZone();
        }

        /// <summary>
        /// Removes the zone matching <paramref name="zoneId"/> and re-evaluates the active zone.
        /// </summary>
        public bool ExitZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;

            bool removed = false;
            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_zones[i].ZoneId, zoneId, StringComparison.Ordinal))
                {
                    _zones.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                ReevaluateActiveZone();
            }

            return removed;
        }

        /// <summary>Returns true if a zone with <paramref name="zoneId"/> is currently on the stack.</summary>
        public bool Contains(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;

            for (int i = 0; i < _zones.Count; i++)
            {
                if (string.Equals(_zones[i].ZoneId, zoneId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>Clears all active zones.</summary>
        public void Clear()
        {
            if (_zones.Count == 0 && ActiveZone == null)
                return;

            _zones.Clear();
            ReevaluateActiveZone();
        }

        private void ReevaluateActiveZone()
        {
            CameraZoneConfig previous = ActiveZone;
            CameraZoneConfig next = null;

            for (int i = 0; i < _zones.Count; i++)
            {
                CameraZoneConfig candidate = _zones[i];
                if (next == null || candidate.Priority >= next.Priority)
                {
                    next = candidate;
                }
            }

            if (ReferenceEquals(previous, next))
                return;

            ActiveZone = next;

            var evt = new CameraZoneChangedEvent(
                previous != null ? previous.ZoneId : string.Empty,
                next != null ? next.ZoneId : string.Empty,
                next);

            OnActiveZoneChanged?.Invoke(evt);
            _eventBus?.Publish(in evt);
        }
    }
}

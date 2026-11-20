using System.Numerics;

namespace ContentContent.Camera
{
    /// <summary>Published when the highest-priority active camera zone changes.</summary>
    public readonly struct CameraZoneChangedEvent
    {
        public string PreviousZoneId { get; }
        public string ActiveZoneId { get; }
        public CameraZoneConfig ActiveZone { get; }

        public CameraZoneChangedEvent(string previousZoneId, string activeZoneId, CameraZoneConfig activeZone)
        {
            PreviousZoneId = previousZoneId;
            ActiveZoneId = activeZoneId;
            ActiveZone = activeZone;
        }
    }

    /// <summary>Published when an immediate camera cut/snap is requested (e.g., room change or respawn).</summary>
    public readonly struct CameraSnapRequestedEvent
    {
        public Vector3 WorldPosition { get; }
        public string Reason { get; }

        public CameraSnapRequestedEvent(Vector3 worldPosition, string reason)
        {
            WorldPosition = worldPosition;
            Reason = reason;
        }
    }

    /// <summary>Published when a gameplay event requests a camera impulse/shake.</summary>
    public readonly struct CameraImpulseRequestedEvent
    {
        public Vector3 Velocity { get; }
        public float Strength { get; }
        public string SourceId { get; }

        public CameraImpulseRequestedEvent(Vector3 velocity, float strength, string sourceId)
        {
            Velocity = velocity;
            Strength = strength;
            SourceId = sourceId;
        }
    }
}

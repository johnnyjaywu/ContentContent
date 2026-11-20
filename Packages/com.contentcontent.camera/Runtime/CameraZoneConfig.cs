using System;
using System.Numerics;

namespace ContentContent.Camera
{
    /// <summary>
    /// Configuration for a camera zone override (e.g., room bounds, boss arena lock, or zoom shift).
    /// Higher <see cref="Priority"/> zones take precedence on <see cref="CameraZoneStack"/>.
    /// </summary>
    [Serializable]
    public sealed class CameraZoneConfig
    {
        /// <summary>Unique identifier for this zone.</summary>
        public string ZoneId = string.Empty;

        /// <summary>Priority of this zone; higher values win over lower values.</summary>
        public int Priority = 0;

        /// <summary>If true, locks the camera follow anchor to <see cref="LockedPosition"/>.</summary>
        public bool LockPosition = false;

        /// <summary>World-space anchor position used when <see cref="LockPosition"/> is true.</summary>
        public Vector3 LockedPosition = Vector3.Zero;

        /// <summary>If &gt; 0, overrides the camera orthographic size while this zone is active.</summary>
        public float OrthographicSizeOverride = 0f;

        /// <summary>Static world-space offset added to the framing anchor while this zone is active.</summary>
        public Vector3 StaticOffset = Vector3.Zero;

        /// <summary>Allows directional horizontal look-ahead while this zone is active.</summary>
        public bool AllowLookAhead = true;

        /// <summary>Allows holding up/down to peek vertically while this zone is active.</summary>
        public bool AllowVerticalPeek = true;

        /// <summary>Optional bounds identifier used by Unity confiners (e.g., <c>CinemachineConfiner2D</c>).</summary>
        public string BoundsId = string.Empty;

        /// <summary>Creates a copy of this zone configuration.</summary>
        public CameraZoneConfig Clone()
        {
            return (CameraZoneConfig)MemberwiseClone();
        }
    }
}

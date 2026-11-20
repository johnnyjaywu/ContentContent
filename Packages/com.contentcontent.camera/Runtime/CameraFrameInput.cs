using System.Numerics;

namespace ContentContent.Camera
{
    /// <summary>Per-tick target state consumed by <see cref="CameraDirector"/>.</summary>
    public struct CameraFrameInput
    {
        /// <summary>Current world-space position of the follow target.</summary>
        public Vector3 TargetPosition;

        /// <summary>Normalized facing direction of the target.</summary>
        public Vector3 FacingDirection;

        /// <summary>Vertical look input (-1 down .. +1 up) for grounded peek.</summary>
        public float VerticalLookInput;

        /// <summary>True when the target is grounded.</summary>
        public bool IsTargetGrounded;

        /// <summary>True when the target is essentially stationary.</summary>
        public bool IsTargetStationary;
    }

    /// <summary>Computed camera framing output produced by <see cref="CameraDirector.Tick"/>.</summary>
    public readonly struct CameraFrameOutput
    {
        public Vector3 FollowAnchorPosition { get; }
        public Vector3 FramingOffset { get; }
        public float OrthographicSize { get; }
        public string ActiveZoneId { get; }
        public string ActiveBoundsId { get; }
        public bool IsPositionLocked { get; }
        public bool SnapRequested { get; }

        public CameraFrameOutput(
            Vector3 followAnchorPosition,
            Vector3 framingOffset,
            float orthographicSize,
            string activeZoneId,
            string activeBoundsId,
            bool isPositionLocked,
            bool snapRequested)
        {
            FollowAnchorPosition = followAnchorPosition;
            FramingOffset = framingOffset;
            OrthographicSize = orthographicSize;
            ActiveZoneId = activeZoneId;
            ActiveBoundsId = activeBoundsId;
            IsPositionLocked = isPositionLocked;
            SnapRequested = snapRequested;
        }
    }
}

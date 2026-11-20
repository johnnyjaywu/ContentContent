using System;

namespace ContentContent.Camera
{
    /// <summary>
    /// Tuning parameters for <see cref="CameraDirector"/> framing, look-ahead, vertical peek, and zoom smoothing.
    /// </summary>
    [Serializable]
    public sealed class CameraFramingConfig
    {
        /// <summary>Default camera orthographic half-height when no zone overrides it.</summary>
        public float DefaultOrthographicSize = 6f;

        /// <summary>Horizontal lead distance ahead of facing direction (units).</summary>
        public float LookAheadDistance = 2f;

        /// <summary>Exponential smoothing rate for horizontal look-ahead transitions.</summary>
        public float LookAheadSmoothSpeed = 6f;

        /// <summary>Delay before flipping look-ahead after reversing horizontal facing direction (seconds).</summary>
        public float LookAheadTurnDelay = 0.08f;

        /// <summary>Vertical offset applied when holding up while grounded and stationary.</summary>
        public float VerticalPeekUpDistance = 3f;

        /// <summary>Vertical downward magnitude applied when holding down while grounded and stationary.</summary>
        public float VerticalPeekDownDistance = 3.5f;

        /// <summary>Hold duration required before vertical peek activates (seconds).</summary>
        public float VerticalPeekHoldDelay = 0.45f;

        /// <summary>Exponential smoothing rate for vertical peek transitions.</summary>
        public float VerticalPeekSmoothSpeed = 8f;

        /// <summary>Exponential smoothing rate for orthographic zoom transitions.</summary>
        public float ZoomSmoothSpeed = 5f;

        /// <summary>Creates a deep copy of this configuration.</summary>
        public CameraFramingConfig Clone()
        {
            return (CameraFramingConfig)MemberwiseClone();
        }

        /// <summary>Creates a Hollow Knight–style Metroidvania camera framing preset.</summary>
        public static CameraFramingConfig CreateHollowKnightPreset()
        {
            return new CameraFramingConfig
            {
                DefaultOrthographicSize = 6.5f,
                LookAheadDistance = 2.2f,
                LookAheadSmoothSpeed = 5.5f,
                LookAheadTurnDelay = 0.1f,
                VerticalPeekUpDistance = 3.2f,
                VerticalPeekDownDistance = 3.8f,
                VerticalPeekHoldDelay = 0.45f,
                VerticalPeekSmoothSpeed = 8f,
                ZoomSmoothSpeed = 4.5f
            };
        }
    }
}

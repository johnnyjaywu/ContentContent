using System;

namespace ContentContent.Character
{
    /// <summary>
    /// Pure C# configuration for <see cref="CharacterMotor"/> and <see cref="CharacterController"/>.
    /// </summary>
    [Serializable]
    public class CharacterConfig
    {
        /// <summary>Spatial plane and gravity model.</summary>
        public MovementMode Mode = MovementMode.SideScroller2D;

        // --- Planar / Horizontal Movement ---

        /// <summary>Maximum target speed along the movement plane (units/sec).</summary>
        public float MaxSpeed = 8f;

        /// <summary>Acceleration rate while grounded (units/sec^2).</summary>
        public float GroundAcceleration = 60f;

        /// <summary>Deceleration rate while grounded when no movement input is given (units/sec^2).</summary>
        public float GroundDeceleration = 70f;

        /// <summary>Acceleration rate while airborne (units/sec^2).</summary>
        public float AirAcceleration = 35f;

        /// <summary>Deceleration rate while airborne when no movement input is given (units/sec^2).</summary>
        public float AirDeceleration = 15f;

        /// <summary>Minimum input or planar speed magnitude considered intentional movement.</summary>
        public float MinMoveThreshold = 0.05f;

        // --- Gravity & Vertical ---

        /// <summary>Downward gravity magnitude along Y (units/sec^2). Ignored in <see cref="MovementMode.TopDown2D"/>.</summary>
        public float Gravity = 30f;

        /// <summary>Gravity multiplier applied while falling (vertical velocity &lt; 0) for snappy arcs.</summary>
        public float FallGravityMultiplier = 1.5f;

        /// <summary>Maximum downward fall speed magnitude (units/sec).</summary>
        public float MaxFallSpeed = 25f;

        /// <summary>Small downward velocity applied while grounded to maintain ground contact on slopes.</summary>
        public float GroundStickVelocity = 0.5f;

        // --- Jump Feel ---

        /// <summary>Upward launch speed on jump (units/sec).</summary>
        public float JumpVelocity = 12f;

        /// <summary>
        /// Multiplier applied to positive vertical velocity when the jump button is released early
        /// (0..1; e.g. 0.5 halves remaining upward speed). Set to 1 to disable variable jump height.
        /// </summary>
        public float JumpCutMultiplier = 0.5f;

        /// <summary>Grace period after leaving the ground during which a grounded jump is still allowed (seconds).</summary>
        public float CoyoteTime = 0.12f;

        /// <summary>Window before landing during which a jump press is buffered and executed on touchdown (seconds).</summary>
        public float JumpBufferTime = 0.12f;

        /// <summary>Number of additional mid-air jumps allowed after leaving the ground (0 = single jump only).</summary>
        public int MaxAirJumps = 0;

        // --- Dash ---

        /// <summary>Speed applied along the dash direction (units/sec).</summary>
        public float DashSpeed = 20f;

        /// <summary>Duration of the dash burst (seconds).</summary>
        public float DashDuration = 0.18f;

        /// <summary>Cooldown before another dash can start (seconds).</summary>
        public float DashCooldown = 0.6f;

        /// <summary>Window during which a dash press is buffered if dash is not yet ready (seconds).</summary>
        public float DashBufferTime = 0.1f;

        /// <summary>If true, gravity is suspended and vertical velocity is zeroed during a dash.</summary>
        public bool DashSuspendsGravity = true;

        /// <summary>If true, dashing is permitted while airborne.</summary>
        public bool AllowAirDash = true;

        /// <summary>Maximum number of air dashes before touching the ground again (ignored if <see cref="AllowAirDash"/> is false).</summary>
        public int MaxAirDashes = 1;

        // --- External Impulses / Knockback ---

        /// <summary>Exponential/linear decay rate applied to external impulse velocity (units/sec^2).</summary>
        public float KnockbackDecay = 35f;

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        public CharacterConfig Clone()
        {
            return (CharacterConfig)MemberwiseClone();
        }
    }
}

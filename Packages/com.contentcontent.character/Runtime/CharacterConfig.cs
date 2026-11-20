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

        /// <summary>
        /// Vertical speed magnitude threshold around the jump apex within which gravity is scaled by
        /// <see cref="ApexGravityMultiplier"/> while jump is held (0 disables apex hang).
        /// </summary>
        public float ApexVelocityThreshold = 0f;

        /// <summary>Gravity multiplier applied near the jump apex when <see cref="ApexVelocityThreshold"/> &gt; 0 and jump is held.</summary>
        public float ApexGravityMultiplier = 0.5f;

        /// <summary>
        /// Optional increased fall speed cap while holding down (&lt; -0.5) when falling (0 disables fast-fall).
        /// </summary>
        public float FastFallMaxSpeed = 0f;

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

        /// <summary>
        /// If true in <see cref="MovementMode.SideScroller2D"/>, dashes can aim in 8 directions using both
        /// Move.X and Move.Y instead of horizontal-only.
        /// </summary>
        public bool AllowOmnidirectionalDash2D = false;

        /// <summary>Multiplier applied to locomotion velocity on the tick a dash finishes (0..1).</summary>
        public float DashEndSpeedMultiplier = 1f;

        // --- Wall Slide & Wall Jump ---

        /// <summary>If true in <see cref="MovementMode.SideScroller2D"/>, falling against a wall clamps descent speed.</summary>
        public bool EnableWallSlide = true;

        /// <summary>If true, the player must push horizontally toward the wall to engage wall slide.</summary>
        public bool WallSlideRequiresInput = true;

        /// <summary>Maximum downward fall speed while wall-sliding (units/sec).</summary>
        public float WallSlideMaxFallSpeed = 3.5f;

        /// <summary>If true in <see cref="MovementMode.SideScroller2D"/>, allows jumping away from walls.</summary>
        public bool EnableWallJump = true;

        /// <summary>Horizontal speed launched away from the wall on a wall jump (units/sec).</summary>
        public float WallJumpHorizontalSpeed = 9f;

        /// <summary>Upward speed launched on a wall jump (units/sec).</summary>
        public float WallJumpVerticalSpeed = 12f;

        /// <summary>Grace period after detaching from a wall during which a wall jump is still allowed (seconds).</summary>
        public float WallJumpCoyoteTime = 0.1f;

        /// <summary>Duration after a wall jump during which horizontal steering is locked to preserve kickback arc (seconds).</summary>
        public float WallJumpLockTime = 0.14f;

        /// <summary>If true, engaging wall slide or wall-jumping refreshes mid-air dash charges.</summary>
        public bool RefreshAirDashOnWallSlide = false;

        /// <summary>If true, executing a wall jump refreshes mid-air jump charges.</summary>
        public bool RefreshAirJumpOnWallJump = false;

        // --- Optional Wall Climb & Stamina ---

        /// <summary>If true in <see cref="MovementMode.SideScroller2D"/>, holding Grab on a wall allows vertical climbing.</summary>
        public bool EnableWallClimb = false;

        /// <summary>Vertical climbing speed while holding Grab on a wall (units/sec).</summary>
        public float WallClimbSpeed = 4f;

        /// <summary>Maximum stamina available for wall climbing before touching ground.</summary>
        public float MaxClimbStamina = 110f;

        /// <summary>Stamina drained per second while actively climbing upward.</summary>
        public float ClimbStaminaDrainPerSecond = 45f;

        /// <summary>Stamina drained per second while clinging still or climbing down.</summary>
        public float ClimbHoldStaminaDrainPerSecond = 10f;

        /// <summary>Stamina cost consumed when wall-jumping while holding Grab.</summary>
        public float WallJumpStaminaCost = 27.5f;

        // --- Moving & One-Way Platforms ---

        /// <summary>Fraction of carried platform velocity inherited as jump momentum when jumping off a moving platform.</summary>
        public float PlatformMomentumInheritance = 1f;

        /// <summary>Duration during which a one-way platform is ignored after pressing drop-through (seconds).</summary>
        public float OneWayDropThroughDuration = 0.2f;

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

        /// <summary>
        /// Creates a snappy Hollow Knight–style 2D Metroidvania configuration preset
        /// (horizontal air dash, Mantis Claw wall slide/jump refreshing air abilities, double jump, no wall-climb stamina).
        /// </summary>
        public static CharacterConfig CreateHollowKnightPreset()
        {
            return new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 8.5f,
                GroundAcceleration = 85f,
                GroundDeceleration = 95f,
                AirAcceleration = 65f,
                AirDeceleration = 55f,
                Gravity = 36f,
                FallGravityMultiplier = 1.6f,
                MaxFallSpeed = 20f,
                ApexVelocityThreshold = 2.5f,
                ApexGravityMultiplier = 0.55f,
                JumpVelocity = 14f,
                JumpCutMultiplier = 0.45f,
                CoyoteTime = 0.1f,
                JumpBufferTime = 0.12f,
                MaxAirJumps = 1,
                AllowOmnidirectionalDash2D = false,
                DashSpeed = 20f,
                DashDuration = 0.16f,
                DashCooldown = 0.55f,
                DashBufferTime = 0.1f,
                DashSuspendsGravity = true,
                AllowAirDash = true,
                MaxAirDashes = 1,
                DashEndSpeedMultiplier = 0.5f,
                EnableWallSlide = true,
                WallSlideRequiresInput = true,
                WallSlideMaxFallSpeed = 3.5f,
                EnableWallJump = true,
                WallJumpHorizontalSpeed = 10f,
                WallJumpVerticalSpeed = 13.5f,
                WallJumpCoyoteTime = 0.1f,
                WallJumpLockTime = 0.14f,
                RefreshAirDashOnWallSlide = true,
                RefreshAirJumpOnWallJump = true,
                EnableWallClimb = false
            };
        }

        /// <summary>
        /// Creates a Celeste-style precision 2D platformer configuration preset
        /// (8-way air dash, apex hang, fast-fall, wall grab/climb with stamina).
        /// </summary>
        public static CharacterConfig CreateCelestePreset()
        {
            return new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D,
                MaxSpeed = 9f,
                GroundAcceleration = 95f,
                GroundDeceleration = 100f,
                AirAcceleration = 65f,
                AirDeceleration = 45f,
                Gravity = 34f,
                FallGravityMultiplier = 1.4f,
                MaxFallSpeed = 18f,
                ApexVelocityThreshold = 3f,
                ApexGravityMultiplier = 0.5f,
                FastFallMaxSpeed = 28f,
                JumpVelocity = 13f,
                JumpCutMultiplier = 0.4f,
                CoyoteTime = 0.1f,
                JumpBufferTime = 0.12f,
                MaxAirJumps = 0,
                AllowOmnidirectionalDash2D = true,
                DashSpeed = 22f,
                DashDuration = 0.16f,
                DashCooldown = 0.25f,
                DashBufferTime = 0.1f,
                DashSuspendsGravity = true,
                AllowAirDash = true,
                MaxAirDashes = 1,
                DashEndSpeedMultiplier = 0.65f,
                EnableWallSlide = true,
                WallSlideRequiresInput = true,
                WallSlideMaxFallSpeed = 3.5f,
                EnableWallJump = true,
                WallJumpHorizontalSpeed = 10f,
                WallJumpVerticalSpeed = 13f,
                WallJumpCoyoteTime = 0.1f,
                WallJumpLockTime = 0.15f,
                EnableWallClimb = true,
                MaxClimbStamina = 110f
            };
        }
    }
}

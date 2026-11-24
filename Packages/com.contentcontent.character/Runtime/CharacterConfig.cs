using System;
using System.Collections.Generic;

namespace ContentContent.Character
{
    /// <summary>
    /// Built-in movement feel presets for <see cref="CharacterConfig"/>.
    /// </summary>
    public enum CharacterPresetType
    {
        SideScroller = 0,
        ActionPlatformer = 1,
        PrecisionClimber = 2,
        TopDown = 3,
        ThirdPerson = 4
    }

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
        /// Copies all settings from <paramref name="other"/> into this instance and clamps invalid values.
        /// </summary>
        public void CopyFrom(CharacterConfig other)
        {
            if (other == null)
            {
                return;
            }

            Mode = other.Mode;

            MaxSpeed = other.MaxSpeed;
            GroundAcceleration = other.GroundAcceleration;
            GroundDeceleration = other.GroundDeceleration;
            AirAcceleration = other.AirAcceleration;
            AirDeceleration = other.AirDeceleration;
            MinMoveThreshold = other.MinMoveThreshold;

            Gravity = other.Gravity;
            FallGravityMultiplier = other.FallGravityMultiplier;
            MaxFallSpeed = other.MaxFallSpeed;
            ApexVelocityThreshold = other.ApexVelocityThreshold;
            ApexGravityMultiplier = other.ApexGravityMultiplier;
            FastFallMaxSpeed = other.FastFallMaxSpeed;
            GroundStickVelocity = other.GroundStickVelocity;

            JumpVelocity = other.JumpVelocity;
            JumpCutMultiplier = other.JumpCutMultiplier;
            CoyoteTime = other.CoyoteTime;
            JumpBufferTime = other.JumpBufferTime;
            MaxAirJumps = other.MaxAirJumps;

            DashSpeed = other.DashSpeed;
            DashDuration = other.DashDuration;
            DashCooldown = other.DashCooldown;
            DashBufferTime = other.DashBufferTime;
            DashSuspendsGravity = other.DashSuspendsGravity;
            AllowAirDash = other.AllowAirDash;
            MaxAirDashes = other.MaxAirDashes;
            AllowOmnidirectionalDash2D = other.AllowOmnidirectionalDash2D;
            DashEndSpeedMultiplier = other.DashEndSpeedMultiplier;

            EnableWallSlide = other.EnableWallSlide;
            WallSlideRequiresInput = other.WallSlideRequiresInput;
            WallSlideMaxFallSpeed = other.WallSlideMaxFallSpeed;
            EnableWallJump = other.EnableWallJump;
            WallJumpHorizontalSpeed = other.WallJumpHorizontalSpeed;
            WallJumpVerticalSpeed = other.WallJumpVerticalSpeed;
            WallJumpCoyoteTime = other.WallJumpCoyoteTime;
            WallJumpLockTime = other.WallJumpLockTime;
            RefreshAirDashOnWallSlide = other.RefreshAirDashOnWallSlide;
            RefreshAirJumpOnWallJump = other.RefreshAirJumpOnWallJump;

            EnableWallClimb = other.EnableWallClimb;
            WallClimbSpeed = other.WallClimbSpeed;
            MaxClimbStamina = other.MaxClimbStamina;
            ClimbStaminaDrainPerSecond = other.ClimbStaminaDrainPerSecond;
            ClimbHoldStaminaDrainPerSecond = other.ClimbHoldStaminaDrainPerSecond;
            WallJumpStaminaCost = other.WallJumpStaminaCost;

            PlatformMomentumInheritance = other.PlatformMomentumInheritance;
            OneWayDropThroughDuration = other.OneWayDropThroughDuration;

            KnockbackDecay = other.KnockbackDecay;

            Sanitize();
        }

        /// <summary>
        /// Clamps numeric fields into safe ranges so impossible values cannot destabilize movement.
        /// </summary>
        public void Sanitize()
        {
            MaxSpeed = Math.Max(0f, MaxSpeed);
            GroundAcceleration = Math.Max(0f, GroundAcceleration);
            GroundDeceleration = Math.Max(0f, GroundDeceleration);
            AirAcceleration = Math.Max(0f, AirAcceleration);
            AirDeceleration = Math.Max(0f, AirDeceleration);
            MinMoveThreshold = ClampRange(MinMoveThreshold, 0f, 1f);

            Gravity = Math.Max(0f, Gravity);
            FallGravityMultiplier = Math.Max(0f, FallGravityMultiplier);
            MaxFallSpeed = Math.Max(0f, MaxFallSpeed);
            ApexVelocityThreshold = Math.Max(0f, ApexVelocityThreshold);
            ApexGravityMultiplier = ClampRange(ApexGravityMultiplier, 0f, 1f);
            FastFallMaxSpeed = Math.Max(0f, FastFallMaxSpeed);
            GroundStickVelocity = Math.Max(0f, GroundStickVelocity);

            JumpVelocity = Math.Max(0f, JumpVelocity);
            JumpCutMultiplier = ClampRange(JumpCutMultiplier, 0f, 1f);
            CoyoteTime = Math.Max(0f, CoyoteTime);
            JumpBufferTime = Math.Max(0f, JumpBufferTime);
            MaxAirJumps = Math.Max(0, MaxAirJumps);

            DashSpeed = Math.Max(0f, DashSpeed);
            DashDuration = Math.Max(0f, DashDuration);
            DashCooldown = Math.Max(0f, DashCooldown);
            DashBufferTime = Math.Max(0f, DashBufferTime);
            MaxAirDashes = Math.Max(0, MaxAirDashes);
            DashEndSpeedMultiplier = ClampRange(DashEndSpeedMultiplier, 0f, 1f);

            WallSlideMaxFallSpeed = Math.Max(0f, WallSlideMaxFallSpeed);
            WallJumpHorizontalSpeed = Math.Max(0f, WallJumpHorizontalSpeed);
            WallJumpVerticalSpeed = Math.Max(0f, WallJumpVerticalSpeed);
            WallJumpCoyoteTime = Math.Max(0f, WallJumpCoyoteTime);
            WallJumpLockTime = Math.Max(0f, WallJumpLockTime);

            WallClimbSpeed = Math.Max(0f, WallClimbSpeed);
            MaxClimbStamina = Math.Max(0f, MaxClimbStamina);
            ClimbStaminaDrainPerSecond = Math.Max(0f, ClimbStaminaDrainPerSecond);
            ClimbHoldStaminaDrainPerSecond = Math.Max(0f, ClimbHoldStaminaDrainPerSecond);
            WallJumpStaminaCost = Math.Max(0f, WallJumpStaminaCost);

            PlatformMomentumInheritance = Math.Max(0f, PlatformMomentumInheritance);
            OneWayDropThroughDuration = Math.Max(0f, OneWayDropThroughDuration);

            KnockbackDecay = Math.Max(0f, KnockbackDecay);
        }

        /// <summary>
        /// Populates <paramref name="warnings"/> with human-readable configuration issues.
        /// </summary>
        public void Validate(List<string> warnings)
        {
            if (warnings == null)
            {
                throw new ArgumentNullException(nameof(warnings));
            }

            warnings.Clear();

            if (MaxSpeed <= 0f)
            {
                warnings.Add("MaxSpeed is 0 or less, so the character cannot move.");
            }

            if (Mode != MovementMode.TopDown2D)
            {
                if (JumpVelocity > 0f && Gravity <= 0f)
                {
                    warnings.Add("Gravity is 0 while JumpVelocity is positive; the character will float upward after jumping.");
                }

                if (JumpVelocity <= 0f && MaxAirJumps > 0)
                {
                    warnings.Add("MaxAirJumps is set, but JumpVelocity is 0 so jumps will have no upward height.");
                }

                if (AllowAirDash && MaxAirDashes <= 0)
                {
                    warnings.Add("AllowAirDash is enabled, but MaxAirDashes is 0 so mid-air dashes will not trigger.");
                }
            }

            if (DashSpeed > 0f && DashDuration <= 0f)
            {
                warnings.Add("DashSpeed is set, but DashDuration is 0 so dashing will not trigger.");
            }

            if (Mode == MovementMode.SideScroller2D)
            {
                if (EnableWallClimb && MaxClimbStamina <= 0f)
                {
                    warnings.Add("EnableWallClimb is active, but MaxClimbStamina is 0 so climbing will stop immediately.");
                }

                if (EnableWallJump && WallJumpHorizontalSpeed <= 0f && WallJumpVerticalSpeed <= 0f)
                {
                    warnings.Add("EnableWallJump is active, but both wall-jump speeds are 0.");
                }
            }
        }

        /// <summary>
        /// Creates a new configuration from a built-in <see cref="CharacterPresetType"/>.
        /// </summary>
        public static CharacterConfig CreatePreset(CharacterPresetType preset)
        {
            switch (preset)
            {
                case CharacterPresetType.ActionPlatformer:
                    return CreateActionPlatformerPreset();
                case CharacterPresetType.PrecisionClimber:
                    return CreatePrecisionClimberPreset();
                case CharacterPresetType.TopDown:
                    return CreateTopDownPreset();
                case CharacterPresetType.ThirdPerson:
                    return CreateThirdPersonPreset();
                case CharacterPresetType.SideScroller:
                default:
                    return CreateSideScrollerPreset();
            }
        }

        /// <summary>
        /// Balanced 2D side-scroller platformer preset with forgiving coyote time, jump buffering, and wall slide/jump.
        /// </summary>
        public static CharacterConfig CreateSideScrollerPreset()
        {
            return new CharacterConfig
            {
                Mode = MovementMode.SideScroller2D
            };
        }

        /// <summary>
        /// Snappy 2D action-platformer preset (horizontal air dash, wall slide/jump refreshing air abilities, double jump).
        /// </summary>
        public static CharacterConfig CreateActionPlatformerPreset()
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
        /// Precision 2D platformer preset (8-way air dash, apex hang, fast-fall, wall grab/climb with stamina).
        /// </summary>
        public static CharacterConfig CreatePrecisionClimberPreset()
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

        /// <summary>
        /// Crisp 2D top-down preset (XY plane movement, snappy stops, directional dash).
        /// </summary>
        public static CharacterConfig CreateTopDownPreset()
        {
            return new CharacterConfig
            {
                Mode = MovementMode.TopDown2D,
                MaxSpeed = 7.5f,
                GroundAcceleration = 75f,
                GroundDeceleration = 85f,
                DashSpeed = 18f,
                DashDuration = 0.15f,
                DashCooldown = 0.45f,
                DashBufferTime = 0.1f,
                DashEndSpeedMultiplier = 0.6f,
                EnableWallSlide = false,
                EnableWallJump = false,
                EnableWallClimb = false
            };
        }

        /// <summary>
        /// Responsive 3D third-person preset (XZ ground movement, Y gravity/jump, dash).
        /// </summary>
        public static CharacterConfig CreateThirdPersonPreset()
        {
            return new CharacterConfig
            {
                Mode = MovementMode.ThirdPerson3D,
                MaxSpeed = 7f,
                GroundAcceleration = 55f,
                GroundDeceleration = 65f,
                AirAcceleration = 25f,
                AirDeceleration = 12f,
                Gravity = 28f,
                FallGravityMultiplier = 1.4f,
                MaxFallSpeed = 25f,
                JumpVelocity = 11f,
                JumpCutMultiplier = 0.55f,
                CoyoteTime = 0.12f,
                JumpBufferTime = 0.12f,
                DashSpeed = 18f,
                DashDuration = 0.18f,
                DashCooldown = 0.65f,
                AllowAirDash = true,
                MaxAirDashes = 1,
                EnableWallSlide = false,
                EnableWallJump = false,
                EnableWallClimb = false
            };
        }

        private static float ClampRange(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

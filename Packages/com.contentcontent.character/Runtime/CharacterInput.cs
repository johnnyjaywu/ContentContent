using System.Numerics;

namespace ContentContent.Character
{
    /// <summary>
    /// Per-tick player or AI input snapshot consumed by <see cref="CharacterMotor"/> and <see cref="CharacterController"/>.
    /// </summary>
    public struct CharacterInput
    {
        /// <summary>
        /// Desired movement direction.
        /// - <see cref="MovementMode.SideScroller2D"/>: uses X (horizontal).
        /// - <see cref="MovementMode.TopDown2D"/>: uses X and Y.
        /// - <see cref="MovementMode.ThirdPerson3D"/>: uses X and Z (world or camera-relative planar direction).
        /// Magnitudes greater than 1 are clamped to 1.
        /// </summary>
        public Vector3 Move;

        /// <summary>True on the tick jump was pressed.</summary>
        public bool JumpPressed;

        /// <summary>True while the jump input is held down.</summary>
        public bool JumpHeld;

        /// <summary>True on the tick jump was released (triggers variable jump cut when ascending).</summary>
        public bool JumpReleased;

        /// <summary>True on the tick dash was pressed.</summary>
        public bool DashPressed;

        /// <summary>True while wall-grab / wall-climb input is held.</summary>
        public bool GrabHeld;

        /// <summary>True on the tick drop-through (down + drop) was pressed on a one-way platform.</summary>
        public bool DropThroughPressed;

        /// <summary>An empty input snapshot with no movement or button presses.</summary>
        public static CharacterInput None => default;
    }

    /// <summary>
    /// Per-tick environment contact snapshot supplied by the physics engine (e.g. Unity 2D/3D casts or unit test fixtures).
    /// </summary>
    public struct MotorSensors
    {
        /// <summary>True if the character is currently supported by ground.</summary>
        public bool IsGrounded;

        /// <summary>Surface normal of the ground contact (defaults to <see cref="Vector3.UnitY"/> when grounded).</summary>
        public Vector3 GroundNormal;

        /// <summary>True if the character's head is touching a ceiling.</summary>
        public bool IsTouchingCeiling;

        /// <summary>True if the character is touching a wall.</summary>
        public bool IsTouchingWall;

        /// <summary>Surface normal of the wall contact, if <see cref="IsTouchingWall"/> is true.</summary>
        public Vector3 WallNormal;

        /// <summary>World-space velocity of the supporting moving platform, if any.</summary>
        public Vector3 PlatformVelocity;

        /// <summary>True if the supporting ground surface is a one-way platform.</summary>
        public bool IsOnOneWayPlatform;

        /// <summary>Convenience sensor state representing flat, level ground.</summary>
        public static MotorSensors Grounded => new MotorSensors
        {
            IsGrounded = true,
            GroundNormal = Vector3.UnitY
        };

        /// <summary>Convenience sensor state representing mid-air with no contacts.</summary>
        public static MotorSensors Airborne => default;
    }
}

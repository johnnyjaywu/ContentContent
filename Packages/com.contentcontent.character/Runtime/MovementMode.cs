using System;

namespace ContentContent.Character
{
    /// <summary>
    /// Spatial coordinate convention and gravity model for a <see cref="CharacterMotor"/>.
    /// </summary>
    public enum MovementMode
    {
        /// <summary>2D side-scroller on the XY plane: horizontal movement on X, gravity and jumping on Y.</summary>
        SideScroller2D = 0,

        /// <summary>2D top-down on the XY plane: planar movement on X and Y with no vertical gravity.</summary>
        TopDown2D = 1,

        /// <summary>3D locomotion on the XZ ground plane with gravity and jumping on Y.</summary>
        ThirdPerson3D = 2
    }

    /// <summary>
    /// Bitmask of movement and input capabilities suppressed while an action or external status lock is active.
    /// </summary>
    [Flags]
    public enum ActionLockFlags
    {
        /// <summary>No capabilities are locked.</summary>
        None = 0,

        /// <summary>Prevents horizontal/planar movement input from accelerating the character.</summary>
        BlockMovement = 1 << 0,

        /// <summary>Prevents facing direction updates from movement input.</summary>
        BlockTurn = 1 << 1,

        /// <summary>Prevents starting a jump (including consuming buffered jumps).</summary>
        BlockJump = 1 << 2,

        /// <summary>Prevents starting a dash (including consuming buffered dashes).</summary>
        BlockDash = 1 << 3,

        /// <summary>Suspends gravity integration while active.</summary>
        SuspendGravity = 1 << 4,

        /// <summary>Locks movement, turning, jumping, dashing, and gravity.</summary>
        All = BlockMovement | BlockTurn | BlockJump | BlockDash | SuspendGravity
    }
}

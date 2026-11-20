using System.Numerics;
using ContentContent.Core.Blackboards;

namespace ContentContent.Character
{
    /// <summary>
    /// Typed <see cref="BlackboardKey{T}"/> definitions automatically synchronized by <see cref="CharacterController"/>
    /// for HFSM transitions, animation drivers, AI perception, and cross-package integration.
    /// </summary>
    public static class CharacterBlackboardKeys
    {
        /// <summary>Current total velocity vector.</summary>
        public static readonly BlackboardKey<Vector3> Velocity = BlackboardKey.Create<Vector3>("Character.Velocity");

        /// <summary>Current total speed magnitude.</summary>
        public static readonly BlackboardKey<float> Speed = BlackboardKey.Create<float>("Character.Speed");

        /// <summary>Current planar movement speed (X in SideScroller2D, XY in TopDown2D, XZ in ThirdPerson3D).</summary>
        public static readonly BlackboardKey<float> PlanarSpeed = BlackboardKey.Create<float>("Character.PlanarSpeed");

        /// <summary>Current vertical speed (Y component).</summary>
        public static readonly BlackboardKey<float> VerticalSpeed = BlackboardKey.Create<float>("Character.VerticalSpeed");

        /// <summary>Normalized facing direction vector.</summary>
        public static readonly BlackboardKey<Vector3> FacingDirection = BlackboardKey.Create<Vector3>("Character.FacingDirection");

        /// <summary>True when supported by ground (always true in TopDown2D).</summary>
        public static readonly BlackboardKey<bool> IsGrounded = BlackboardKey.Create<bool>("Character.IsGrounded");

        /// <summary>True while actively dashing.</summary>
        public static readonly BlackboardKey<bool> IsDashing = BlackboardKey.Create<bool>("Character.IsDashing");

        /// <summary>True while actively wall-sliding.</summary>
        public static readonly BlackboardKey<bool> IsWallSliding = BlackboardKey.Create<bool>("Character.IsWallSliding");

        /// <summary>True while actively wall-climbing.</summary>
        public static readonly BlackboardKey<bool> IsWallClimbing = BlackboardKey.Create<bool>("Character.IsWallClimbing");

        /// <summary>Normalized outward normal of the currently contacted wall, or zero if none.</summary>
        public static readonly BlackboardKey<Vector3> WallNormal = BlackboardKey.Create<Vector3>("Character.WallNormal");

        /// <summary>Remaining mid-air jumps before next landing.</summary>
        public static readonly BlackboardKey<int> AirJumpsRemaining = BlackboardKey.Create<int>("Character.AirJumpsRemaining");

        /// <summary>Remaining mid-air dashes before next landing.</summary>
        public static readonly BlackboardKey<int> AirDashesRemaining = BlackboardKey.Create<int>("Character.AirDashesRemaining");

        /// <summary>Remaining wall-climb stamina.</summary>
        public static readonly BlackboardKey<float> ClimbStamina = BlackboardKey.Create<float>("Character.ClimbStamina");

        /// <summary>ID of the currently running <see cref="ICharacterAction"/>, or empty string if none.</summary>
        public static readonly BlackboardKey<string> ActiveActionId = BlackboardKey.Create<string>("Character.ActiveActionId");

        /// <summary>True if any <see cref="ICharacterAction"/> is currently executing.</summary>
        public static readonly BlackboardKey<bool> IsActionActive = BlackboardKey.Create<bool>("Character.IsActionActive");
    }
}

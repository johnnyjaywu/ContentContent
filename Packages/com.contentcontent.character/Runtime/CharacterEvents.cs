using System.Numerics;
using ContentContent.Core.Fsm;

namespace ContentContent.Character
{
    /// <summary>
    /// Published when a character executes a grounded, coyote, wall, or air jump.
    /// </summary>
    public readonly struct CharacterJumpedEvent
    {
        /// <summary>True if the jump was executed mid-air using an air jump charge.</summary>
        public bool IsAirJump { get; }

        /// <summary>True if the jump kicked off a wall.</summary>
        public bool IsWallJump { get; }

        /// <summary>Remaining air jumps after this jump.</summary>
        public int AirJumpsRemaining { get; }

        /// <summary>Character velocity immediately after the jump launch.</summary>
        public Vector3 Velocity { get; }

        public CharacterJumpedEvent(bool isAirJump, int airJumpsRemaining, Vector3 velocity, bool isWallJump = false)
        {
            IsAirJump = isAirJump;
            IsWallJump = isWallJump;
            AirJumpsRemaining = airJumpsRemaining;
            Velocity = velocity;
        }
    }

    /// <summary>
    /// Published when a character kicks off a wall via wall jump.
    /// </summary>
    public readonly struct CharacterWallJumpedEvent
    {
        /// <summary>Normalized outward surface normal of the wall kicked off from.</summary>
        public Vector3 WallNormal { get; }

        /// <summary>Character velocity immediately after the wall jump launch.</summary>
        public Vector3 Velocity { get; }

        public CharacterWallJumpedEvent(Vector3 wallNormal, Vector3 velocity)
        {
            WallNormal = wallNormal;
            Velocity = velocity;
        }
    }

    /// <summary>
    /// Published when a character transitions from airborne to grounded.
    /// </summary>
    public readonly struct CharacterLandedEvent
    {
        /// <summary>Downward impact speed (positive magnitude) at the moment of touchdown.</summary>
        public float ImpactSpeed { get; }

        /// <summary>Surface normal of the ground landed on.</summary>
        public Vector3 GroundNormal { get; }

        public CharacterLandedEvent(float impactSpeed, Vector3 groundNormal)
        {
            ImpactSpeed = impactSpeed;
            GroundNormal = groundNormal;
        }
    }

    /// <summary>
    /// Published when a character starts a dash burst.
    /// </summary>
    public readonly struct CharacterDashedEvent
    {
        /// <summary>Normalized direction of the dash.</summary>
        public Vector3 Direction { get; }

        /// <summary>True if the dash was initiated while airborne.</summary>
        public bool IsAirDash { get; }

        public CharacterDashedEvent(Vector3 direction, bool isAirDash)
        {
            Direction = direction;
            IsAirDash = isAirDash;
        }
    }

    /// <summary>
    /// Published when an <see cref="ICharacterAction"/> begins executing.
    /// </summary>
    public readonly struct CharacterActionStartedEvent
    {
        /// <summary>Identifier of the started action.</summary>
        public string ActionId { get; }

        /// <summary>The action instance that started.</summary>
        public ICharacterAction Action { get; }

        public CharacterActionStartedEvent(string actionId, ICharacterAction action)
        {
            ActionId = actionId;
            Action = action;
        }
    }

    /// <summary>
    /// Published when an <see cref="ICharacterAction"/> completes or is interrupted.
    /// </summary>
    public readonly struct CharacterActionEndedEvent
    {
        /// <summary>Identifier of the ended action.</summary>
        public string ActionId { get; }

        /// <summary>The action instance that ended.</summary>
        public ICharacterAction Action { get; }

        /// <summary>True if the action was interrupted before natural completion.</summary>
        public bool Interrupted { get; }

        public CharacterActionEndedEvent(string actionId, ICharacterAction action, bool interrupted)
        {
            ActionId = actionId;
            Action = action;
            Interrupted = interrupted;
        }
    }

    /// <summary>
    /// Published when the character's locomotion <see cref="StateMachine"/> transitions states.
    /// </summary>
    public readonly struct CharacterStateChangedEvent
    {
        /// <summary>State transitioned away from (may be null on initial entry).</summary>
        public IState PreviousState { get; }

        /// <summary>Newly active state.</summary>
        public IState NextState { get; }

        public CharacterStateChangedEvent(IState previousState, IState nextState)
        {
            PreviousState = previousState;
            NextState = nextState;
        }
    }
}

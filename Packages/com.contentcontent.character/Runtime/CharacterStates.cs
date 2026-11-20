using ContentContent.Core.Fsm;

namespace ContentContent.Character
{
    /// <summary>
    /// Base class for built-in locomotion states driven by Core's <see cref="StateMachine"/>.
    /// </summary>
    public abstract class CharacterLocomotionState : StateBase<CharacterContext>
    {
        /// <summary>Human-readable state name.</summary>
        public string Name { get; }

        protected CharacterLocomotionState(CharacterContext context, string name) : base(context)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    /// <summary>Grounded and stationary locomotion state.</summary>
    public sealed class IdleState : CharacterLocomotionState
    {
        public IdleState(CharacterContext context) : base(context, "Idle") { }
    }

    /// <summary>Grounded and moving locomotion state.</summary>
    public sealed class MoveState : CharacterLocomotionState
    {
        public MoveState(CharacterContext context) : base(context, "Move") { }
    }

    /// <summary>Airborne (jumping or falling) locomotion state.</summary>
    public sealed class AirborneState : CharacterLocomotionState
    {
        public AirborneState(CharacterContext context) : base(context, "Airborne") { }
    }

    /// <summary>Active dash burst locomotion state.</summary>
    public sealed class DashState : CharacterLocomotionState
    {
        public DashState(CharacterContext context) : base(context, "Dash") { }
    }

    /// <summary>Wall-sliding locomotion state (falling slowly while pressed against a wall).</summary>
    public sealed class WallSlideState : CharacterLocomotionState
    {
        public WallSlideState(CharacterContext context) : base(context, "WallSlide") { }
    }

    /// <summary>Wall-climbing locomotion state (holding Grab on a wall).</summary>
    public sealed class WallClimbState : CharacterLocomotionState
    {
        public WallClimbState(CharacterContext context) : base(context, "WallClimb") { }
    }
}

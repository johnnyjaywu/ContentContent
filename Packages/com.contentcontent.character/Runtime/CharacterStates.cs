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
}

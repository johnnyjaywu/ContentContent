namespace GameSystem.Fsm
{
    /// <summary>
    /// Contract for a state inside a <see cref="StateMachine"/>.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the state becomes active.</summary>
        void OnEnter();

        /// <summary>Called every update tick while the state is active.</summary>
        void OnTick(float deltaTime);

        /// <summary>Called every fixed-timestep physics tick while the state is active.</summary>
        void OnFixedTick(float fixedDeltaTime);

        /// <summary>Called once right before transitioning away from this state.</summary>
        void OnExit();
    }
}

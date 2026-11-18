namespace GameSystem.Fsm
{
    /// <summary>
    /// Convenience base class implementing <see cref="IState"/> with virtual no-op hooks.
    /// </summary>
    public abstract class StateBase : IState
    {
        public virtual void OnEnter() { }
        public virtual void OnTick(float deltaTime) { }
        public virtual void OnFixedTick(float fixedDeltaTime) { }
        public virtual void OnExit() { }
    }

    /// <summary>
    /// Convenience base class for states that operate on a typed shared context object.
    /// </summary>
    public abstract class StateBase<TContext> : StateBase
    {
        protected TContext Context { get; }

        protected StateBase(TContext context)
        {
            Context = context;
        }
    }
}

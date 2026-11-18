namespace ContentContent.Core.Fsm
{
    /// <summary>
    /// Contract for a conditional transition to a target <see cref="IState"/>.
    /// </summary>
    public interface ITransition
    {
        IState TargetState { get; }
        int Priority { get; }
        bool AllowSelfTransition { get; }
        bool Evaluate();
    }
}

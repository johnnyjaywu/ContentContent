using System;

namespace ContentContent.Core.Fsm
{
    /// <summary>
    /// Predicate-driven transition implementation. Higher <see cref="Priority"/> transitions are evaluated first.
    /// </summary>
    public sealed class Transition : ITransition
    {
        private readonly Func<bool> _condition;

        public IState TargetState { get; }
        public int Priority { get; }
        public bool AllowSelfTransition { get; }

        public Transition(IState targetState, Func<bool> condition, int priority = 0, bool allowSelfTransition = false)
        {
            TargetState = targetState ?? throw new ArgumentNullException(nameof(targetState));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Priority = priority;
            AllowSelfTransition = allowSelfTransition;
        }

        public bool Evaluate() => _condition();
    }
}

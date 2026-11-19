using System;
using ContentContent.Core.Fsm;

namespace ContentContent.Core.Blackboards
{
    /// <summary>
    /// Opt-in extensions connecting <see cref="Blackboard"/> keys to <see cref="StateMachine"/> transitions.
    /// </summary>
    public static class BlackboardFsmExtensions
    {
        /// <summary>
        /// Adds a transition from <paramref name="from"/> to <paramref name="to"/> that triggers when
        /// <paramref name="blackboard"/> contains <paramref name="key"/> and <paramref name="predicate"/> returns true.
        /// </summary>
        public static void AddTransitionWhen<T>(
            this StateMachine machine,
            IState from,
            IState to,
            Blackboard blackboard,
            BlackboardKey<T> key,
            Func<T, bool> predicate,
            int priority = 0,
            bool allowSelfTransition = false)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            if (blackboard == null) throw new ArgumentNullException(nameof(blackboard));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            machine.AddTransition(
                from,
                to,
                () => blackboard.TryGet(key, out T val) && predicate(val),
                priority,
                allowSelfTransition);
        }

        /// <summary>
        /// Adds a global (any-state) transition to <paramref name="to"/> that triggers when
        /// <paramref name="blackboard"/> contains <paramref name="key"/> and <paramref name="predicate"/> returns true.
        /// </summary>
        public static void AddAnyTransitionWhen<T>(
            this StateMachine machine,
            IState to,
            Blackboard blackboard,
            BlackboardKey<T> key,
            Func<T, bool> predicate,
            int priority = 0,
            bool allowSelfTransition = false)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            if (blackboard == null) throw new ArgumentNullException(nameof(blackboard));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            machine.AddAnyTransition(
                to,
                () => blackboard.TryGet(key, out T val) && predicate(val),
                priority,
                allowSelfTransition);
        }
    }
}

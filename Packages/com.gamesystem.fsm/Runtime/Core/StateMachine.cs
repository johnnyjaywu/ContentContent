using System;
using System.Collections.Generic;

namespace GameSystem.Fsm
{
    /// <summary>
    /// Hierarchical Finite State Machine (HFSM).
    /// Because <see cref="StateMachine"/> itself implements <see cref="IState"/>, any state machine
    /// can be nested inside a parent state machine as a sub-state.
    /// </summary>
    public class StateMachine : IState
    {
        private readonly Dictionary<IState, List<ITransition>> _transitions = new Dictionary<IState, List<ITransition>>();
        private readonly List<ITransition> _anyTransitions = new List<ITransition>();
        private List<ITransition> _currentTransitions;

        /// <summary>Currently active state, or null if not entered.</summary>
        public IState CurrentState { get; private set; }

        /// <summary>Default state entered when <see cref="OnEnter"/> is called.</summary>
        public IState InitialState { get; set; }

        /// <summary>Invoked whenever the active state changes: (previousState, nextState).</summary>
        public event Action<IState, IState> OnStateChanged;

        public StateMachine(IState initialState = null)
        {
            InitialState = initialState;
        }

        /// <summary>
        /// Registers a conditional transition from <paramref name="from"/> to <paramref name="to"/>.
        /// Higher <paramref name="priority"/> transitions are evaluated first.
        /// </summary>
        public void AddTransition(IState from, IState to, Func<bool> condition, int priority = 0, bool allowSelfTransition = false)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            AddTransition(from, new Transition(to, condition, priority, allowSelfTransition));
        }

        /// <summary>
        /// Registers a custom <see cref="ITransition"/> originating from <paramref name="from"/>.
        /// </summary>
        public void AddTransition(IState from, ITransition transition)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (transition == null) throw new ArgumentNullException(nameof(transition));

            if (!_transitions.TryGetValue(from, out var list))
            {
                list = new List<ITransition>();
                _transitions[from] = list;
            }

            InsertByPriority(list, transition);

            if (ReferenceEquals(CurrentState, from))
            {
                _currentTransitions = list;
            }
        }

        /// <summary>
        /// Registers a global transition evaluated from any active state before state-specific transitions.
        /// </summary>
        public void AddAnyTransition(IState to, Func<bool> condition, int priority = 0, bool allowSelfTransition = false)
        {
            AddAnyTransition(new Transition(to, condition, priority, allowSelfTransition));
        }

        /// <summary>
        /// Registers a custom global <see cref="ITransition"/> evaluated from any active state.
        /// </summary>
        public void AddAnyTransition(ITransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            InsertByPriority(_anyTransitions, transition);
        }

        /// <summary>
        /// Immediately transitions to <paramref name="nextState"/>.
        /// </summary>
        public void SetState(IState nextState, bool allowSelfTransition = false)
        {
            if (!allowSelfTransition && ReferenceEquals(CurrentState, nextState))
                return;

            IState previous = CurrentState;
            previous?.OnExit();

            CurrentState = nextState;
            _currentTransitions = nextState != null && _transitions.TryGetValue(nextState, out var list)
                ? list
                : null;

            CurrentState?.OnEnter();
            OnStateChanged?.Invoke(previous, CurrentState);
        }

        /// <summary>
        /// Enters the state machine by transitioning to <see cref="InitialState"/>.
        /// </summary>
        public void OnEnter()
        {
            if (InitialState != null)
            {
                SetState(InitialState, allowSelfTransition: true);
            }
        }

        /// <summary>
        /// Evaluates transitions and ticks the active state.
        /// </summary>
        public void OnTick(float deltaTime)
        {
            ITransition transition = FindTriggeredTransition();
            if (transition != null)
            {
                SetState(transition.TargetState, transition.AllowSelfTransition);
            }

            CurrentState?.OnTick(deltaTime);
        }

        /// <summary>
        /// Forwards fixed-timestep updates to the active state.
        /// </summary>
        public void OnFixedTick(float fixedDeltaTime)
        {
            CurrentState?.OnFixedTick(fixedDeltaTime);
        }

        /// <summary>
        /// Exits the currently active state (and recursively any nested state machines).
        /// </summary>
        public void OnExit()
        {
            if (CurrentState == null)
                return;

            IState previous = CurrentState;
            previous.OnExit();
            CurrentState = null;
            _currentTransitions = null;
            OnStateChanged?.Invoke(previous, null);
        }

        private ITransition FindTriggeredTransition()
        {
            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                ITransition t = _anyTransitions[i];
                if (!t.AllowSelfTransition && ReferenceEquals(t.TargetState, CurrentState))
                    continue;
                if (t.Evaluate())
                    return t;
            }

            if (_currentTransitions != null)
            {
                for (int i = 0; i < _currentTransitions.Count; i++)
                {
                    ITransition t = _currentTransitions[i];
                    if (!t.AllowSelfTransition && ReferenceEquals(t.TargetState, CurrentState))
                        continue;
                    if (t.Evaluate())
                        return t;
                }
            }

            return null;
        }

        private static void InsertByPriority(List<ITransition> list, ITransition transition)
        {
            int index = 0;
            while (index < list.Count && list[index].Priority >= transition.Priority)
            {
                index++;
            }
            list.Insert(index, transition);
        }
    }
}

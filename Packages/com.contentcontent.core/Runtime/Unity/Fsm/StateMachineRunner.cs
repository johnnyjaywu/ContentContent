using UnityEngine;

namespace ContentContent.Core.Fsm.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter that drives a <see cref="StateMachine"/> using Unity's
    /// <see cref="Update"/> and <see cref="FixedUpdate"/> loops.
    /// </summary>
    public class StateMachineRunner : MonoBehaviour
    {
        [SerializeField] private bool _useUnscaledTime;

        public StateMachine Machine { get; private set; }

        /// <summary>Human-readable name of the active state for debugging/inspectors.</summary>
        public string CurrentStateName => Machine?.CurrentState?.ToString() ?? "None";

        /// <summary>
        /// Binds and optionally enters a <see cref="StateMachine"/>.
        /// </summary>
        public void Initialize(StateMachine machine, bool enterImmediately = true)
        {
            Machine?.OnExit();
            Machine = machine;

            if (enterImmediately)
            {
                Machine?.OnEnter();
            }
        }

        private void Update()
        {
            if (Machine == null)
                return;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Machine.OnTick(dt);
        }

        private void FixedUpdate()
        {
            if (Machine == null)
                return;

            float fixedDt = _useUnscaledTime ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime;
            Machine.OnFixedTick(fixedDt);
        }

        private void OnDisable()
        {
            Machine?.OnExit();
        }
    }
}

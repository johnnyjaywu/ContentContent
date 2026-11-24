using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Core.Fsm.Unity
{
    /// <summary>
    /// MonoBehaviour adapter driving a pure-C# <see cref="StateMachine"/> using Unity's
    /// <see cref="Update"/> and <see cref="FixedUpdate"/> loops.
    /// </summary>
    [AddComponentMenu("ContentContent/Core/State Machine Runner")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/core/quickstart")]
    public class StateMachineRunner : MonoBehaviour
    {
        [Tooltip("Uses Time.unscaledDeltaTime so UI or pause menus keep running when Time.timeScale is 0.")]
        [SerializeField] private bool _useUnscaledTime;

        [Header("Events")]
        [Tooltip("Invoked whenever the active state changes, passing the new state name.")]
        [SerializeField] private UnityEvent<string> _onStateChanged = new UnityEvent<string>();

        public StateMachine Machine { get; private set; }

        /// <summary>Human-readable name of the active state for debugging and Inspectors.</summary>
        public string CurrentStateName => Machine?.CurrentState?.ToString() ?? "None";

        /// <summary>Inspector-wireable event emitted whenever the active state changes.</summary>
        public UnityEvent<string> OnStateChanged => _onStateChanged ?? (_onStateChanged = new UnityEvent<string>());

        /// <summary>
        /// Binds and optionally enters a <see cref="StateMachine"/>.
        /// </summary>
        public void Initialize(StateMachine machine, bool enterImmediately = true)
        {
            if (Machine != null)
            {
                Machine.OnStateChanged -= HandleStateChanged;
                Machine.OnExit();
            }

            Machine = machine;

            if (Machine != null)
            {
                Machine.OnStateChanged += HandleStateChanged;
                if (enterImmediately)
                {
                    Machine.OnEnter();
                }
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

        private void OnDestroy()
        {
            if (Machine != null)
            {
                Machine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(IState previous, IState next)
        {
            _onStateChanged?.Invoke(next != null ? next.ToString() : "None");
        }
    }
}

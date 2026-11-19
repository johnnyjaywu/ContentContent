using System;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter hosting a <see cref="DialogRunner"/> and <see cref="DialogVariableStore"/>.
    /// </summary>
    public class DialogRunnerBehaviour : MonoBehaviour
    {
        [SerializeField] private DialogGraphAsset _initialGraph;
        [SerializeField] private DialogLocalizationTableAsset _localizationTable;
        [SerializeField] private bool _playOnStart;

        [Header("Events")]
        [SerializeField] private UnityEvent _onDialogStarted = new UnityEvent();
        [SerializeField] private UnityEvent _onDialogEnded = new UnityEvent();
        [SerializeField] private UnityEvent<string> _onCommandExecuted = new UnityEvent<string>();

        private DialogRunner _runner;
        private DialogVariableStore _variables;

        public DialogRunner Runner
        {
            get
            {
                EnsureInitialized();
                return _runner;
            }
        }

        public DialogVariableStore Variables
        {
            get
            {
                EnsureInitialized();
                return _variables;
            }
        }

        public bool IsRunning => _runner != null && _runner.IsRunning;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (_playOnStart && _initialGraph != null)
                StartDialog(_initialGraph);
        }

        private void OnDestroy()
        {
            if (_runner == null)
                return;

            _runner.OnStarted -= HandleStarted;
            _runner.OnEnded -= HandleEnded;
            _runner.OnCommandExecuted -= HandleCommandExecuted;
        }

        public void SetView(IDialogView view)
        {
            EnsureInitialized();
            _runner.View = view;
        }

        public void SetLocalizer(IDialogLocalizer localizer)
        {
            EnsureInitialized();
            _runner.Localizer = localizer ?? PassthroughLocalizer.Instance;
        }

        public void StartDialog(DialogGraphAsset graphAsset, string startNodeId = null)
        {
            if (graphAsset == null)
                return;

            StartDialog(graphAsset.BuildGraph(), startNodeId);
        }

        public void StartDialog(DialogGraph graph, string startNodeId = null, bool applyDefaultVariables = true)
        {
            if (graph == null)
                return;

            EnsureInitialized();
            _runner.Start(graph, startNodeId, applyDefaultVariables);
        }

        public void Continue() => Runner.Continue();

        public void SelectChoice(int visibleChoiceIndex) => Runner.SelectChoice(visibleChoiceIndex);

        public void StopDialog() => Runner.Stop();

        public void RegisterCommandHandler(string commandName, Action<DialogCommand> handler) =>
            Runner.RegisterCommandHandler(commandName, handler);

        private void EnsureInitialized()
        {
            if (_runner != null)
                return;

            _variables = new DialogVariableStore();
            IDialogLocalizer localizer = _localizationTable != null
                ? (IDialogLocalizer)_localizationTable
                : PassthroughLocalizer.Instance;

            _runner = new DialogRunner(_variables, localizer);
            _runner.OnStarted += HandleStarted;
            _runner.OnEnded += HandleEnded;
            _runner.OnCommandExecuted += HandleCommandExecuted;
        }

        private void HandleStarted(DialogGraph _) => _onDialogStarted?.Invoke();
        private void HandleEnded(DialogGraph _) => _onDialogEnded?.Invoke();
        private void HandleCommandExecuted(DialogCommand command) => _onCommandExecuted?.Invoke(command.Name);
    }
}

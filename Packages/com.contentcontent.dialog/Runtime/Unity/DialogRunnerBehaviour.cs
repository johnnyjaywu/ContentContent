using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Code-free MonoBehaviour adapter hosting a <see cref="DialogRunner"/> and <see cref="DialogVariableStore"/>,
    /// with automatic child <see cref="IDialogView"/> discovery and Inspector command bindings.
    /// </summary>
    [AddComponentMenu("ContentContent/Dialog/Dialog Runner")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/dialog/quickstart")]
    public class DialogRunnerBehaviour : MonoBehaviour
    {
        [Serializable]
        public struct CommandEventBinding
        {
            [Tooltip("Case-insensitive command name (e.g., open_gate, play_sfx).")]
            public string commandName;

            [Tooltip("UnityEvent invoked with space-joined command arguments whenever this command executes.")]
            public UnityEvent<string> onInvoked;
        }

        [Header("Content")]
        [Tooltip("Optional ScriptableObject dialog graph to start first when Play On Start is enabled.")]
        [SerializeField] private DialogGraphAsset _initialGraph;

        [Tooltip("Optional Yarn-style .txt/.yarn TextAsset script started when no Initial Graph is assigned.")]
        [SerializeField] private TextAsset _initialScript;

        [Tooltip("Optional ScriptableObject string table for localizing line keys.")]
        [SerializeField] private DialogLocalizationTableAsset _localizationTable;

        [Tooltip("Automatically starts Initial Graph or Initial Script during Start().")]
        [SerializeField] private bool _playOnStart;

        [Header("Command Hooks (Code-Free)")]
        [Tooltip("Maps dialog <<command args...>> directives directly to scene UnityEvents without C#.")]
        [SerializeField] private List<CommandEventBinding> _commandBindings = new List<CommandEventBinding>();

        [Header("Events")]
        [SerializeField] private UnityEvent _onDialogStarted = new UnityEvent();
        [SerializeField] private UnityEvent _onDialogEnded = new UnityEvent();
        [SerializeField] private UnityEvent<string, string> _onLineShown = new UnityEvent<string, string>();
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

        public DialogGraphAsset InitialGraph => _initialGraph;
        public TextAsset InitialScript => _initialScript;
        public IReadOnlyList<CommandEventBinding> CommandBindings => _commandBindings;
        public bool IsRunning => _runner != null && _runner.IsRunning;

        public UnityEvent OnDialogStarted => _onDialogStarted ?? (_onDialogStarted = new UnityEvent());
        public UnityEvent OnDialogEnded => _onDialogEnded ?? (_onDialogEnded = new UnityEvent());
        public UnityEvent<string, string> OnLineShown => _onLineShown ?? (_onLineShown = new UnityEvent<string, string>());
        public UnityEvent<string> OnCommandExecuted => _onCommandExecuted ?? (_onCommandExecuted = new UnityEvent<string>());

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (!_playOnStart)
                return;

            StartInitialDialog();
        }

        private void OnDestroy()
        {
            if (_runner == null)
                return;

            _runner.OnStarted -= HandleStarted;
            _runner.OnLineShown -= HandleLineShown;
            _runner.OnEnded -= HandleEnded;
            _runner.OnCommandExecuted -= HandleCommandExecuted;
        }

        /// <summary>Starts <see cref="InitialGraph"/> if assigned, otherwise <see cref="InitialScript"/>.</summary>
        public void StartInitialDialog()
        {
            if (_initialGraph != null)
            {
                StartDialog(_initialGraph);
            }
            else if (_initialScript != null)
            {
                StartDialog(_initialScript);
            }
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

        public void StartDialog(TextAsset scriptAsset, string startNodeId = null, bool applyDefaultVariables = true)
        {
            if (scriptAsset == null)
                return;

            DialogGraph graph = DialogScriptParser.Parse(scriptAsset.text, scriptAsset.name, startNodeId);
            StartDialog(graph, startNodeId, applyDefaultVariables);
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

            IDialogView childView = GetComponentInChildren<IDialogView>(includeInactive: true);
            _runner = new DialogRunner(_variables, localizer, childView);
            _runner.OnStarted += HandleStarted;
            _runner.OnLineShown += HandleLineShown;
            _runner.OnEnded += HandleEnded;
            _runner.OnCommandExecuted += HandleCommandExecuted;
        }

        private void HandleStarted(DialogGraph _) => _onDialogStarted?.Invoke();

        private void HandleLineShown(DialogLineViewModel line) => _onLineShown?.Invoke(line.Speaker, line.Text);

        private void HandleEnded(DialogGraph _) => _onDialogEnded?.Invoke();

        private void HandleCommandExecuted(DialogCommand command)
        {
            _onCommandExecuted?.Invoke(command.Name);

            if (_commandBindings == null || _commandBindings.Count == 0)
                return;

            string argPayload = command.Arguments != null && command.Arguments.Count > 0
                ? string.Join(" ", command.Arguments)
                : string.Empty;

            for (int i = 0; i < _commandBindings.Count; i++)
            {
                CommandEventBinding binding = _commandBindings[i];
                if (string.IsNullOrWhiteSpace(binding.commandName))
                    continue;

                if (string.Equals(binding.commandName.Trim(), command.Name, StringComparison.OrdinalIgnoreCase))
                {
                    binding.onInvoked?.Invoke(argPayload);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Pure C# dialog state machine that walks a <see cref="DialogGraph"/>, evaluates conditions,
    /// applies mutations, dispatches commands, interpolates {variables}, and drives an <see cref="IDialogView"/>.
    /// </summary>
    public sealed class DialogRunner
    {
        private readonly Dictionary<string, Action<DialogCommand>> _commandHandlers =
            new Dictionary<string, Action<DialogCommand>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<DialogChoice> _activeChoices = new List<DialogChoice>();
        private readonly List<DialogChoiceViewModel> _activeChoiceViewModels = new List<DialogChoiceViewModel>();

        public IDialogVariableStore Variables { get; }
        public IDialogLocalizer Localizer { get; set; }
        public IDialogView View { get; set; }

        public DialogGraph CurrentGraph { get; private set; }
        public DialogNode CurrentNode { get; private set; }
        public bool IsRunning { get; private set; }
        public IReadOnlyList<DialogChoiceViewModel> CurrentChoices => _activeChoiceViewModels;

        public event Action<DialogGraph> OnStarted;
        public event Action<DialogLineViewModel> OnLineShown;
        public event Action<IReadOnlyList<DialogChoiceViewModel>> OnChoicesShown;
        public event Action<DialogCommand> OnCommandExecuted;
        public event Action<DialogGraph> OnEnded;

        public DialogRunner(
            IDialogVariableStore variables = null,
            IDialogLocalizer localizer = null,
            IDialogView view = null)
        {
            Variables = variables ?? new DialogVariableStore();
            Localizer = localizer ?? PassthroughLocalizer.Instance;
            View = view;
        }

        public void RegisterCommandHandler(string commandName, Action<DialogCommand> handler)
        {
            if (string.IsNullOrEmpty(commandName))
                throw new ArgumentException("Command name cannot be null or empty.", nameof(commandName));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _commandHandlers[commandName] = handler;
        }

        public bool UnregisterCommandHandler(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
                return false;

            return _commandHandlers.Remove(commandName);
        }

        public void Start(DialogGraph graph, string startNodeId = null, bool applyDefaultVariables = true)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            if (IsRunning)
                Stop();

            string entryNodeId = string.IsNullOrEmpty(startNodeId) ? graph.StartNodeId : startNodeId;
            if (!graph.TryGetNode(entryNodeId, out DialogNode _))
                throw new InvalidOperationException($"Dialog start node '{entryNodeId}' was not found in graph '{graph.Id}'.");

            if (applyDefaultVariables)
            {
                foreach (KeyValuePair<string, DialogValue> pair in graph.DefaultVariables)
                {
                    if (!Variables.Has(pair.Key))
                        Variables.Set(pair.Key, pair.Value);
                }
            }

            CurrentGraph = graph;
            IsRunning = true;

            View?.OnDialogStarted(graph);
            OnStarted?.Invoke(graph);

            EnterNode(entryNodeId);
        }

        public void Continue()
        {
            if (!IsRunning || CurrentNode == null)
                return;

            if (_activeChoices.Count > 0)
                return;

            if (CurrentNode.IsEnd || string.IsNullOrEmpty(CurrentNode.NextNodeId))
            {
                EndDialog();
                return;
            }

            EnterNode(CurrentNode.NextNodeId);
        }

        public void SelectChoice(int visibleChoiceIndex)
        {
            if (!IsRunning || CurrentNode == null)
                return;

            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= _activeChoices.Count)
                throw new ArgumentOutOfRangeException(nameof(visibleChoiceIndex));

            DialogChoice chosen = _activeChoices[visibleChoiceIndex];
            _activeChoices.Clear();
            _activeChoiceViewModels.Clear();

            ApplyMutations(chosen.Mutations);
            ExecuteCommands(chosen.Commands);

            if (string.IsNullOrEmpty(chosen.TargetNodeId))
            {
                EndDialog();
                return;
            }

            EnterNode(chosen.TargetNodeId);
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            EndDialog();
        }

        public string FormatText(string key)
        {
            IDialogLocalizer localizer = Localizer ?? PassthroughLocalizer.Instance;
            string localized = localizer.Localize(key);
            return InterpolateVariables(localized);
        }

        private void EnterNode(string nodeId)
        {
            if (!CurrentGraph.TryGetNode(nodeId, out DialogNode node))
                throw new InvalidOperationException($"Dialog node '{nodeId}' was not found in graph '{CurrentGraph.Id}'.");

            CurrentNode = node;
            _activeChoices.Clear();
            _activeChoiceViewModels.Clear();

            ApplyMutations(node.OnEnterMutations);
            ExecuteCommands(node.OnEnterCommands);

            if (!IsRunning)
                return;

            if (node.IsEnd && string.IsNullOrEmpty(node.LineKey))
            {
                EndDialog();
                return;
            }

            if (node.HasChoices)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    DialogChoice choice = node.Choices[i];
                    if (!choice.IsAvailable(Variables))
                        continue;

                    int visibleIndex = _activeChoices.Count;
                    _activeChoices.Add(choice);
                    _activeChoiceViewModels.Add(new DialogChoiceViewModel(
                        visibleIndex,
                        FormatText(choice.TextKey),
                        choice.TargetNodeId));
                }
            }

            var lineVm = new DialogLineViewModel(
                node.Id,
                FormatText(node.Speaker),
                FormatText(node.LineKey),
                _activeChoiceViewModels.Count > 0,
                node.IsEnd);

            View?.ShowLine(lineVm);
            OnLineShown?.Invoke(lineVm);

            if (_activeChoiceViewModels.Count > 0)
            {
                View?.ShowChoices(_activeChoiceViewModels);
                OnChoicesShown?.Invoke(_activeChoiceViewModels);
            }
        }

        private void EndDialog()
        {
            DialogGraph endedGraph = CurrentGraph;
            IsRunning = false;
            CurrentNode = null;
            CurrentGraph = null;
            _activeChoices.Clear();
            _activeChoiceViewModels.Clear();

            View?.OnDialogEnded(endedGraph);
            OnEnded?.Invoke(endedGraph);
        }

        private void ApplyMutations(IReadOnlyList<DialogMutation> mutations)
        {
            for (int i = 0; i < mutations.Count; i++)
                mutations[i].Apply(Variables);
        }

        private void ExecuteCommands(IReadOnlyList<DialogCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                DialogCommand cmd = commands[i];
                if (_commandHandlers.TryGetValue(cmd.Name, out Action<DialogCommand> handler))
                    handler.Invoke(cmd);

                OnCommandExecuted?.Invoke(cmd);
            }
        }

        private string InterpolateVariables(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
                return text ?? string.Empty;

            var sb = new StringBuilder(text.Length + 16);
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '{')
                {
                    int close = text.IndexOf('}', i + 1);
                    if (close > i + 1)
                    {
                        string varName = text.Substring(i + 1, close - i - 1).Trim();
                        if (varName.StartsWith("$", StringComparison.Ordinal))
                            varName = varName.Substring(1);
                        if (Variables.TryGet(varName, out DialogValue value))
                        {
                            sb.Append(value.ToDisplayString());
                            i = close + 1;
                            continue;
                        }
                    }
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }
    }
}

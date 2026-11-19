using System.Collections.Generic;

namespace ContentContent.Dialog
{
    public readonly struct DialogLineViewModel
    {
        public string NodeId { get; }
        public string Speaker { get; }
        public string Text { get; }
        public bool HasChoices { get; }
        public bool IsEnd { get; }

        public DialogLineViewModel(string nodeId, string speaker, string text, bool hasChoices, bool isEnd)
        {
            NodeId = nodeId ?? string.Empty;
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            HasChoices = hasChoices;
            IsEnd = isEnd;
        }
    }

    public readonly struct DialogChoiceViewModel
    {
        public int Index { get; }
        public string Text { get; }
        public string TargetNodeId { get; }

        public DialogChoiceViewModel(int index, string text, string targetNodeId)
        {
            Index = index;
            Text = text ?? string.Empty;
            TargetNodeId = targetNodeId ?? string.Empty;
        }
    }

    /// <summary>
    /// UI-agnostic presentation interface implemented by uGUI, UI Toolkit, or headless test views.
    /// </summary>
    public interface IDialogView
    {
        void OnDialogStarted(DialogGraph graph);
        void ShowLine(DialogLineViewModel line);
        void ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices);
        void OnDialogEnded(DialogGraph graph);
    }
}

using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Single node in a <see cref="DialogGraph"/> holding a speaker, line key, optional choices or next target.
    /// </summary>
    public sealed class DialogNode
    {
        private static readonly DialogChoice[] EmptyChoices = Array.Empty<DialogChoice>();
        private static readonly DialogMutation[] EmptyMutations = Array.Empty<DialogMutation>();
        private static readonly DialogCommand[] EmptyCommands = Array.Empty<DialogCommand>();

        public string Id { get; }
        public string Speaker { get; }
        public string LineKey { get; }
        public string NextNodeId { get; }
        public bool IsEnd { get; }
        public IReadOnlyList<DialogChoice> Choices { get; }
        public IReadOnlyList<DialogMutation> OnEnterMutations { get; }
        public IReadOnlyList<DialogCommand> OnEnterCommands { get; }

        public bool HasChoices => Choices.Count > 0;

        public DialogNode(
            string id,
            string speaker = "",
            string lineKey = "",
            string nextNodeId = "",
            bool isEnd = false,
            IReadOnlyList<DialogChoice> choices = null,
            IReadOnlyList<DialogMutation> onEnterMutations = null,
            IReadOnlyList<DialogCommand> onEnterCommands = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Node id cannot be null or empty.", nameof(id));

            Id = id;
            Speaker = speaker ?? string.Empty;
            LineKey = lineKey ?? string.Empty;
            NextNodeId = nextNodeId ?? string.Empty;
            IsEnd = isEnd;
            Choices = choices ?? EmptyChoices;
            OnEnterMutations = onEnterMutations ?? EmptyMutations;
            OnEnterCommands = onEnterCommands ?? EmptyCommands;
        }
    }
}

using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Branching option on a <see cref="DialogNode"/>.
    /// Costs/mutations placed here execute on the transition (revisit-safe).
    /// </summary>
    public sealed class DialogChoice
    {
        private static readonly DialogCondition[] EmptyConditions = Array.Empty<DialogCondition>();
        private static readonly DialogMutation[] EmptyMutations = Array.Empty<DialogMutation>();
        private static readonly DialogCommand[] EmptyCommands = Array.Empty<DialogCommand>();

        public string TextKey { get; }
        public string TargetNodeId { get; }
        public IReadOnlyList<DialogCondition> Conditions { get; }
        public IReadOnlyList<DialogMutation> Mutations { get; }
        public IReadOnlyList<DialogCommand> Commands { get; }

        public DialogChoice(
            string textKey,
            string targetNodeId,
            IReadOnlyList<DialogCondition> conditions = null,
            IReadOnlyList<DialogMutation> mutations = null,
            IReadOnlyList<DialogCommand> commands = null)
        {
            TextKey = textKey ?? string.Empty;
            TargetNodeId = targetNodeId ?? string.Empty;
            Conditions = conditions ?? EmptyConditions;
            Mutations = mutations ?? EmptyMutations;
            Commands = commands ?? EmptyCommands;
        }

        public bool IsAvailable(IDialogVariableStore store)
        {
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (!Conditions[i].Evaluate(store))
                    return false;
            }

            return true;
        }
    }
}

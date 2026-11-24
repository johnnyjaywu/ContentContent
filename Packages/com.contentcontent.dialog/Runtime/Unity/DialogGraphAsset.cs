using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Designer-authored ScriptableObject defining a branching <see cref="DialogGraph"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogGraph", menuName = "ContentContent/Dialog/Dialog Graph")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/dialog/quickstart")]
    public class DialogGraphAsset : ScriptableObject
    {
        [Serializable]
        public struct ValueData
        {
            public DialogValueType type;
            public bool boolValue;
            public int intValue;
            public float floatValue;
            public string stringValue;

            public DialogValue ToRuntime()
            {
                switch (type)
                {
                    case DialogValueType.Bool: return DialogValue.FromBool(boolValue);
                    case DialogValueType.Int: return DialogValue.FromInt(intValue);
                    case DialogValueType.Float: return DialogValue.FromFloat(floatValue);
                    case DialogValueType.String: return DialogValue.FromString(stringValue);
                    default: return DialogValue.FromBool(false);
                }
            }
        }

        [Serializable]
        public struct VariableEntry
        {
            public string name;
            public ValueData value;
        }

        [Serializable]
        public struct ConditionData
        {
            public string variableName;
            public DialogComparisonOperator comparison;
            public ValueData compareValue;

            public DialogCondition ToRuntime() =>
                new DialogCondition(variableName, comparison, compareValue.ToRuntime());
        }

        [Serializable]
        public struct MutationData
        {
            public string variableName;
            public DialogMutationOperator mutation;
            public ValueData value;

            public DialogMutation ToRuntime() =>
                new DialogMutation(variableName, mutation, value.ToRuntime());
        }

        [Serializable]
        public struct CommandData
        {
            public string name;
            public string[] arguments;

            public DialogCommand ToRuntime() => new DialogCommand(name, arguments);
        }

        [Serializable]
        public struct ChoiceData
        {
            public string textKey;
            public string targetNodeId;
            public List<ConditionData> conditions;
            public List<MutationData> mutations;
            public List<CommandData> commands;
        }

        [Serializable]
        public struct NodeData
        {
            public string id;
            public string speaker;
            [TextArea(2, 5)]
            public string lineKey;
            public string nextNodeId;
            public bool isEnd;
            public List<ChoiceData> choices;
            public List<MutationData> onEnterMutations;
            public List<CommandData> onEnterCommands;
        }

        [Tooltip("Optional graph identifier passed to views and events. Defaults to the asset name when empty.")]
        [SerializeField] private string _graphId;

        [Tooltip("Entry node id started by default when running this graph.")]
        [SerializeField] private string _startNodeId;

        [Tooltip("Initial variable defaults applied when starting the dialog if not already set.")]
        [SerializeField] private List<VariableEntry> _defaultVariables = new List<VariableEntry>();

        [Tooltip("Authored conversation nodes, choices, conditions, mutations, and commands.")]
        [SerializeField] private List<NodeData> _nodes = new List<NodeData>();

        public string GraphId => string.IsNullOrEmpty(_graphId) ? name : _graphId;
        public string StartNodeId => _startNodeId;
        public int NodeCount => _nodes != null ? _nodes.Count : 0;

        /// <summary>
        /// Validates authored nodes and returns structural issues without throwing on duplicate IDs.
        /// </summary>
        public IReadOnlyList<DialogValidationIssue> ValidateGraph()
        {
            var issues = new List<DialogValidationIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool hasDuplicates = false;

            if (_nodes != null)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    string id = _nodes[i].id;
                    if (string.IsNullOrEmpty(id))
                    {
                        issues.Add(new DialogValidationIssue(string.Empty, $"Node at index {i} has an empty id and will be skipped."));
                    }
                    else if (!seen.Add(id))
                    {
                        hasDuplicates = true;
                        issues.Add(new DialogValidationIssue(id, $"Duplicate dialog node id '{id}'. Rename one of the duplicate nodes."));
                    }
                }
            }

            if (!hasDuplicates)
            {
                issues.AddRange(BuildGraph().Validate());
            }

            return issues;
        }

        public DialogGraph BuildGraph()
        {
            var defaults = new Dictionary<string, DialogValue>(StringComparer.Ordinal);
            for (int i = 0; i < _defaultVariables.Count; i++)
            {
                VariableEntry v = _defaultVariables[i];
                if (!string.IsNullOrEmpty(v.name))
                    defaults[v.name] = v.value.ToRuntime();
            }

            var runtimeNodes = new List<DialogNode>(_nodes.Count);
            for (int i = 0; i < _nodes.Count; i++)
            {
                NodeData n = _nodes[i];
                if (string.IsNullOrEmpty(n.id))
                    continue;

                var choices = BuildChoices(n.choices);
                var mutations = BuildMutations(n.onEnterMutations);
                var commands = BuildCommands(n.onEnterCommands);

                runtimeNodes.Add(new DialogNode(
                    n.id,
                    n.speaker,
                    n.lineKey,
                    n.nextNodeId,
                    n.isEnd,
                    choices,
                    mutations,
                    commands));
            }

            return new DialogGraph(GraphId, _startNodeId, runtimeNodes, defaults);
        }

        private static List<DialogChoice> BuildChoices(List<ChoiceData> source)
        {
            if (source == null || source.Count == 0)
                return null;

            var result = new List<DialogChoice>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                ChoiceData c = source[i];
                result.Add(new DialogChoice(
                    c.textKey,
                    c.targetNodeId,
                    BuildConditions(c.conditions),
                    BuildMutations(c.mutations),
                    BuildCommands(c.commands)));
            }

            return result;
        }

        private static List<DialogCondition> BuildConditions(List<ConditionData> source)
        {
            if (source == null || source.Count == 0)
                return null;

            var result = new List<DialogCondition>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrEmpty(source[i].variableName))
                    result.Add(source[i].ToRuntime());
            }

            return result;
        }

        private static List<DialogMutation> BuildMutations(List<MutationData> source)
        {
            if (source == null || source.Count == 0)
                return null;

            var result = new List<DialogMutation>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrEmpty(source[i].variableName))
                    result.Add(source[i].ToRuntime());
            }

            return result;
        }

        private static List<DialogCommand> BuildCommands(List<CommandData> source)
        {
            if (source == null || source.Count == 0)
                return null;

            var result = new List<DialogCommand>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrEmpty(source[i].name))
                    result.Add(source[i].ToRuntime());
            }

            return result;
        }
    }
}

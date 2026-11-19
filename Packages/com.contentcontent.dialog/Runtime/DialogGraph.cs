using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    public sealed class DialogValidationIssue
    {
        public string NodeId { get; }
        public string Message { get; }

        public DialogValidationIssue(string nodeId, string message)
        {
            NodeId = nodeId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(NodeId) ? Message : $"[{NodeId}] {Message}";
    }

    /// <summary>
    /// Immutable dialog graph containing nodes, a start node id, and optional default variables.
    /// </summary>
    public sealed class DialogGraph
    {
        private readonly Dictionary<string, DialogNode> _nodes;

        public string Id { get; }
        public string StartNodeId { get; }
        public IReadOnlyDictionary<string, DialogNode> Nodes => _nodes;
        public IReadOnlyDictionary<string, DialogValue> DefaultVariables { get; }

        public DialogGraph(
            string id,
            string startNodeId,
            IEnumerable<DialogNode> nodes,
            IReadOnlyDictionary<string, DialogValue> defaultVariables = null)
        {
            Id = id ?? string.Empty;
            StartNodeId = startNodeId ?? string.Empty;
            _nodes = new Dictionary<string, DialogNode>(StringComparer.Ordinal);

            if (nodes != null)
            {
                foreach (DialogNode node in nodes)
                {
                    if (node == null)
                        continue;

                    if (_nodes.ContainsKey(node.Id))
                        throw new ArgumentException($"Duplicate dialog node id '{node.Id}'.", nameof(nodes));

                    _nodes.Add(node.Id, node);
                }
            }

            DefaultVariables = defaultVariables != null
                ? new Dictionary<string, DialogValue>(defaultVariables, StringComparer.Ordinal)
                : new Dictionary<string, DialogValue>(StringComparer.Ordinal);
        }

        public bool TryGetNode(string nodeId, out DialogNode node)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                node = null;
                return false;
            }

            return _nodes.TryGetValue(nodeId, out node);
        }

        /// <summary>
        /// Validates start node, exit targets, dead ends, and reachability.
        /// </summary>
        public List<DialogValidationIssue> Validate()
        {
            var issues = new List<DialogValidationIssue>();

            if (string.IsNullOrEmpty(StartNodeId) || !_nodes.ContainsKey(StartNodeId))
                issues.Add(new DialogValidationIssue(string.Empty, $"Start node '{StartNodeId}' does not exist."));

            foreach (KeyValuePair<string, DialogNode> pair in _nodes)
            {
                DialogNode node = pair.Value;
                bool hasChoices = node.Choices.Count > 0;
                bool hasNext = !string.IsNullOrEmpty(node.NextNodeId);

                int exitCount = (hasChoices ? 1 : 0) + (hasNext ? 1 : 0) + (node.IsEnd ? 1 : 0);
                if (exitCount == 0)
                    issues.Add(new DialogValidationIssue(node.Id, "Dead end: node has no choices, next node, or end flag."));
                else if (exitCount > 1)
                    issues.Add(new DialogValidationIssue(node.Id, "Ambiguous exit: node should define exactly one of choices, next node, or end flag."));

                if (hasNext && !_nodes.ContainsKey(node.NextNodeId))
                    issues.Add(new DialogValidationIssue(node.Id, $"Next node '{node.NextNodeId}' does not exist."));

                for (int i = 0; i < node.Choices.Count; i++)
                {
                    DialogChoice choice = node.Choices[i];
                    if (string.IsNullOrEmpty(choice.TargetNodeId) || !_nodes.ContainsKey(choice.TargetNodeId))
                        issues.Add(new DialogValidationIssue(node.Id, $"Choice {i} target '{choice.TargetNodeId}' does not exist."));
                }
            }

            if (!string.IsNullOrEmpty(StartNodeId) && _nodes.ContainsKey(StartNodeId))
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var stack = new Stack<string>();
                stack.Push(StartNodeId);

                while (stack.Count > 0)
                {
                    string currentId = stack.Pop();
                    if (!visited.Add(currentId) || !_nodes.TryGetValue(currentId, out DialogNode current))
                        continue;

                    if (!string.IsNullOrEmpty(current.NextNodeId))
                        stack.Push(current.NextNodeId);

                    for (int i = 0; i < current.Choices.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(current.Choices[i].TargetNodeId))
                            stack.Push(current.Choices[i].TargetNodeId);
                    }
                }

                foreach (string nodeId in _nodes.Keys)
                {
                    if (!visited.Contains(nodeId))
                        issues.Add(new DialogValidationIssue(nodeId, "Node is unreachable from start."));
                }
            }

            return issues;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ContentContent.Dialog
{
    public sealed class DialogScriptParseException : Exception
    {
        public int LineNumber { get; }

        public DialogScriptParseException(int lineNumber, string message)
            : base($"Line {lineNumber}: {message}")
        {
            LineNumber = lineNumber;
        }
    }

    /// <summary>
    /// Parses Yarn-Spinner-style plain text dialog scripts into a <see cref="DialogGraph"/>.
    /// Supports nodes (title/---/===), // comments, Speaker: lines, #line:KEY tags,
    /// &lt;&lt;declare&gt;&gt;, &lt;&lt;set&gt;&gt;, &lt;&lt;jump&gt;&gt;, &lt;&lt;stop&gt;&gt;,
    /// custom &lt;&lt;command args...&gt;&gt;, and -&gt; choices with &lt;&lt;if ...&gt;&gt; conditions
    /// and indented transition mutations/commands/inline replies.
    /// </summary>
    public static class DialogScriptParser
    {
        private sealed class PendingLine
        {
            public string Speaker = string.Empty;
            public string LineKey = string.Empty;
            public readonly List<DialogMutation> Mutations = new List<DialogMutation>();
            public readonly List<DialogCommand> Commands = new List<DialogCommand>();
        }

        private sealed class PendingChoice
        {
            public int LineNumber;
            public string TextKey = string.Empty;
            public string TargetNodeId = string.Empty;
            public bool ExplicitlyEnded;
            public readonly List<DialogCondition> Conditions = new List<DialogCondition>();
            public readonly List<DialogMutation> TransitionMutations = new List<DialogMutation>();
            public readonly List<DialogCommand> TransitionCommands = new List<DialogCommand>();
            public readonly List<PendingLine> InlineLines = new List<PendingLine>();
        }

        public static DialogGraph Parse(string scriptText, string graphId = "script_dialog", string startNodeIdOverride = null)
        {
            if (scriptText == null)
                throw new ArgumentNullException(nameof(scriptText));

            string[] rawLines = scriptText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var defaults = new Dictionary<string, DialogValue>(StringComparer.Ordinal);
            var nodes = new List<DialogNode>();
            string firstNodeId = null;

            bool inHeader = false;
            bool inBody = false;
            string currentTitle = null;
            int nodeStartLine = 0;

            var pendingLines = new List<PendingLine>();
            var pendingMutations = new List<DialogMutation>();
            var pendingCommands = new List<DialogCommand>();
            var pendingChoices = new List<PendingChoice>();
            PendingChoice activeChoice = null;
            string nodeJumpTarget = string.Empty;
            bool nodeExplicitEnd = false;

            void ResetNodeState()
            {
                inHeader = false;
                inBody = false;
                currentTitle = null;
                nodeStartLine = 0;
                pendingLines.Clear();
                pendingMutations.Clear();
                pendingCommands.Clear();
                pendingChoices.Clear();
                activeChoice = null;
                nodeJumpTarget = string.Empty;
                nodeExplicitEnd = false;
            }

            void FlushNode()
            {
                if (string.IsNullOrEmpty(currentTitle))
                    throw new DialogScriptParseException(nodeStartLine, "Node is missing a 'title:' header.");

                if (firstNodeId == null)
                    firstNodeId = currentTitle;

                // Ensure at least one PendingLine exists so mutations/commands/choices have a host node.
                if (pendingLines.Count == 0)
                {
                    pendingLines.Add(new PendingLine());
                }

                // Attach trailing node-level mutations/commands to the last line.
                if (pendingMutations.Count > 0 || pendingCommands.Count > 0)
                {
                    PendingLine last = pendingLines[pendingLines.Count - 1];
                    last.Mutations.AddRange(pendingMutations);
                    last.Commands.AddRange(pendingCommands);
                    pendingMutations.Clear();
                    pendingCommands.Clear();
                }

                // Build choices (synthesizing inline reply nodes when a choice contains spoken lines).
                List<DialogChoice> builtChoices = null;
                var synthesizedNodes = new List<DialogNode>();

                if (pendingChoices.Count > 0)
                {
                    builtChoices = new List<DialogChoice>(pendingChoices.Count);
                    for (int c = 0; c < pendingChoices.Count; c++)
                    {
                        PendingChoice pc = pendingChoices[c];
                        string targetId = pc.TargetNodeId;

                        if (pc.InlineLines.Count > 0)
                        {
                            string baseInlineId = $"{currentTitle}__choice_{c}";
                            EmitLineChain(
                                baseInlineId,
                                pc.InlineLines,
                                choices: null,
                                jumpTarget: pc.ExplicitlyEnded ? string.Empty : pc.TargetNodeId,
                                explicitEnd: pc.ExplicitlyEnded || string.IsNullOrEmpty(pc.TargetNodeId),
                                output: synthesizedNodes);
                            targetId = baseInlineId;
                        }
                        else if (pc.ExplicitlyEnded)
                        {
                            targetId = string.Empty;
                        }

                        builtChoices.Add(new DialogChoice(
                            pc.TextKey,
                            targetId,
                            pc.Conditions.Count > 0 ? new List<DialogCondition>(pc.Conditions) : null,
                            pc.TransitionMutations.Count > 0 ? new List<DialogMutation>(pc.TransitionMutations) : null,
                            pc.TransitionCommands.Count > 0 ? new List<DialogCommand>(pc.TransitionCommands) : null));
                    }
                }

                bool isEnd = builtChoices == null && (nodeExplicitEnd || string.IsNullOrEmpty(nodeJumpTarget));
                EmitLineChain(
                    currentTitle,
                    pendingLines,
                    builtChoices,
                    isEnd ? string.Empty : nodeJumpTarget,
                    isEnd,
                    nodes);

                for (int i = 0; i < synthesizedNodes.Count; i++)
                    nodes.Add(synthesizedNodes[i]);

                ResetNodeState();
            }

            for (int i = 0; i < rawLines.Length; i++)
            {
                int lineNumber = i + 1;
                string raw = rawLines[i];
                string trimmed = raw.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                    continue;

                if (!inHeader && !inBody)
                {
                    if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                    {
                        inHeader = true;
                        nodeStartLine = lineNumber;
                        currentTitle = trimmed.Substring("title:".Length).Trim();
                        if (string.IsNullOrEmpty(currentTitle))
                            throw new DialogScriptParseException(lineNumber, "Node 'title:' cannot be empty.");
                        continue;
                    }

                    if (trimmed.StartsWith("<<", StringComparison.Ordinal) && trimmed.EndsWith(">>", StringComparison.Ordinal))
                    {
                        string inner = trimmed.Substring(2, trimmed.Length - 4).Trim();
                        if (inner.StartsWith("declare ", StringComparison.OrdinalIgnoreCase))
                        {
                            ParseDeclare(inner.Substring("declare ".Length).Trim(), lineNumber, defaults);
                            continue;
                        }
                    }

                    throw new DialogScriptParseException(lineNumber, $"Unexpected content outside node header: '{trimmed}'. Expected 'title: <node_id>'.");
                }

                if (inHeader)
                {
                    if (trimmed == "---")
                    {
                        inHeader = false;
                        inBody = true;
                        continue;
                    }

                    if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentTitle = trimmed.Substring("title:".Length).Trim();
                        continue;
                    }

                    // Ignore other optional header metadata lines (e.g., tags:, position:).
                    continue;
                }

                // Inside node body.
                if (trimmed == "===")
                {
                    FlushNode();
                    continue;
                }

                bool isIndented = raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t');

                // Choice line: "-> Choice text <<if ...>>"
                if (trimmed.StartsWith("->", StringComparison.Ordinal))
                {
                    if (isIndented && activeChoice != null)
                        throw new DialogScriptParseException(lineNumber, "Nested choices inside choices are not supported; use <<jump NodeId>> to branch.");

                    activeChoice = ParseChoiceHeader(trimmed.Substring(2).Trim(), lineNumber);
                    pendingChoices.Add(activeChoice);
                    continue;
                }

                // If unindented content appears after a choice, close the active choice block.
                if (!isIndented && activeChoice != null)
                    activeChoice = null;

                // Command / directive: <<...>>
                if (trimmed.StartsWith("<<", StringComparison.Ordinal) && trimmed.EndsWith(">>", StringComparison.Ordinal))
                {
                    string inner = trimmed.Substring(2, trimmed.Length - 4).Trim();
                    if (inner.StartsWith("declare ", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseDeclare(inner.Substring("declare ".Length).Trim(), lineNumber, defaults);
                    }
                    else if (inner.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                    {
                        DialogMutation mutation = ParseSet(inner.Substring("set ".Length).Trim(), lineNumber);
                        if (activeChoice != null)
                        {
                            if (activeChoice.InlineLines.Count > 0)
                                activeChoice.InlineLines[activeChoice.InlineLines.Count - 1].Mutations.Add(mutation);
                            else
                                activeChoice.TransitionMutations.Add(mutation);
                        }
                        else
                        {
                            pendingMutations.Add(mutation);
                        }
                    }
                    else if (inner.StartsWith("jump ", StringComparison.OrdinalIgnoreCase))
                    {
                        string target = inner.Substring("jump ".Length).Trim();
                        if (string.IsNullOrEmpty(target))
                            throw new DialogScriptParseException(lineNumber, "<<jump>> requires a target node id.");

                        if (activeChoice != null)
                            activeChoice.TargetNodeId = target;
                        else
                            nodeJumpTarget = target;
                    }
                    else if (string.Equals(inner, "stop", StringComparison.OrdinalIgnoreCase))
                    {
                        if (activeChoice != null)
                            activeChoice.ExplicitlyEnded = true;
                        else
                            nodeExplicitEnd = true;
                    }
                    else
                    {
                        DialogCommand command = ParseCommand(inner, lineNumber);
                        if (activeChoice != null)
                        {
                            if (activeChoice.InlineLines.Count > 0)
                                activeChoice.InlineLines[activeChoice.InlineLines.Count - 1].Commands.Add(command);
                            else
                                activeChoice.TransitionCommands.Add(command);
                        }
                        else
                        {
                            pendingCommands.Add(command);
                        }
                    }

                    continue;
                }

                // Spoken dialogue line
                (string speaker, string lineKey) = ParseSpokenLine(trimmed);
                var lineEntry = new PendingLine
                {
                    Speaker = speaker,
                    LineKey = lineKey
                };

                if (activeChoice != null)
                {
                    activeChoice.InlineLines.Add(lineEntry);
                }
                else
                {
                    if (pendingMutations.Count > 0)
                    {
                        lineEntry.Mutations.AddRange(pendingMutations);
                        pendingMutations.Clear();
                    }

                    if (pendingCommands.Count > 0)
                    {
                        lineEntry.Commands.AddRange(pendingCommands);
                        pendingCommands.Clear();
                    }

                    pendingLines.Add(lineEntry);
                }
            }

            if (inHeader || inBody)
                throw new DialogScriptParseException(nodeStartLine, $"Unclosed node '{currentTitle}'. Missing '===' terminator.");

            string startId = !string.IsNullOrEmpty(startNodeIdOverride) ? startNodeIdOverride : (firstNodeId ?? string.Empty);
            return new DialogGraph(graphId, startId, nodes, defaults);
        }

        private static void EmitLineChain(
            string baseId,
            List<PendingLine> lines,
            List<DialogChoice> choices,
            string jumpTarget,
            bool explicitEnd,
            List<DialogNode> output)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                PendingLine line = lines[i];
                bool isLast = i == lines.Count - 1;
                string id = i == 0 ? baseId : $"{baseId}__{i}";
                string nextId = !isLast ? $"{baseId}__{i + 1}" : (choices == null && !explicitEnd ? jumpTarget : string.Empty);
                bool isEnd = isLast && choices == null && explicitEnd;

                output.Add(new DialogNode(
                    id: id,
                    speaker: line.Speaker,
                    lineKey: line.LineKey,
                    nextNodeId: nextId,
                    isEnd: isEnd,
                    choices: isLast ? choices : null,
                    onEnterMutations: line.Mutations.Count > 0 ? new List<DialogMutation>(line.Mutations) : null,
                    onEnterCommands: line.Commands.Count > 0 ? new List<DialogCommand>(line.Commands) : null));
            }
        }

        private static PendingChoice ParseChoiceHeader(string text, int lineNumber)
        {
            var choice = new PendingChoice { LineNumber = lineNumber };

            int ifStart = text.IndexOf("<<if ", StringComparison.OrdinalIgnoreCase);
            if (ifStart >= 0)
            {
                int ifEnd = text.IndexOf(">>", ifStart + 5, StringComparison.Ordinal);
                if (ifEnd < 0)
                    throw new DialogScriptParseException(lineNumber, "Unclosed '<<if ...>>' condition on choice.");

                string expr = text.Substring(ifStart + 5, ifEnd - (ifStart + 5)).Trim();
                ParseConditions(expr, lineNumber, choice.Conditions);

                string before = text.Substring(0, ifStart).Trim();
                string after = text.Substring(ifEnd + 2).Trim();
                text = string.IsNullOrEmpty(after) ? before : $"{before} {after}".Trim();
            }

            choice.TextKey = ExtractLineKeyOrText(text);
            return choice;
        }

        private static (string speaker, string lineKey) ParseSpokenLine(string line)
        {
            string explicitKey = ExtractExplicitLineTag(line, out string contentWithoutTag);

            string speaker = string.Empty;
            string body = contentWithoutTag;

            int colonIndex = contentWithoutTag.IndexOf(':');
            if (colonIndex > 0)
            {
                string candidateSpeaker = contentWithoutTag.Substring(0, colonIndex).Trim();
                if (candidateSpeaker.Length > 0 &&
                    candidateSpeaker.IndexOf('{') < 0 &&
                    !candidateSpeaker.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                    !candidateSpeaker.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    speaker = candidateSpeaker;
                    body = contentWithoutTag.Substring(colonIndex + 1).Trim();
                }
            }

            return (speaker, !string.IsNullOrEmpty(explicitKey) ? explicitKey : body);
        }

        private static string ExtractLineKeyOrText(string text)
        {
            string explicitKey = ExtractExplicitLineTag(text, out string contentWithoutTag);
            return !string.IsNullOrEmpty(explicitKey) ? explicitKey : contentWithoutTag;
        }

        private static string ExtractExplicitLineTag(string text, out string contentWithoutTag)
        {
            int tagIndex = text.LastIndexOf("#line:", StringComparison.OrdinalIgnoreCase);
            if (tagIndex >= 0)
            {
                string key = text.Substring(tagIndex + "#line:".Length).Trim();
                contentWithoutTag = text.Substring(0, tagIndex).Trim();
                return key;
            }

            contentWithoutTag = text.Trim();
            return string.Empty;
        }

        private static void ParseDeclare(string body, int lineNumber, Dictionary<string, DialogValue> defaults)
        {
            SplitAssignment(body, lineNumber, out string varName, out string rhs);
            defaults[varName] = ParseLiteral(rhs, lineNumber);
        }

        private static DialogMutation ParseSet(string body, int lineNumber)
        {
            // Compound operators: +=, -=
            int plusEq = body.IndexOf("+=", StringComparison.Ordinal);
            if (plusEq > 0)
            {
                string varName = NormalizeVarName(body.Substring(0, plusEq).Trim(), lineNumber);
                DialogValue val = ParseLiteral(body.Substring(plusEq + 2).Trim(), lineNumber);
                return new DialogMutation(varName, DialogMutationOperator.Add, val);
            }

            int minusEq = body.IndexOf("-=", StringComparison.Ordinal);
            if (minusEq > 0)
            {
                string varName = NormalizeVarName(body.Substring(0, minusEq).Trim(), lineNumber);
                DialogValue val = ParseLiteral(body.Substring(minusEq + 2).Trim(), lineNumber);
                return new DialogMutation(varName, DialogMutationOperator.Subtract, val);
            }

            SplitAssignment(body, lineNumber, out string targetVar, out string rhs);

            // Toggle: !$var or not $var
            if (rhs.StartsWith("!", StringComparison.Ordinal) || rhs.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
            {
                string inner = rhs.StartsWith("!", StringComparison.Ordinal)
                    ? rhs.Substring(1).Trim()
                    : rhs.Substring(4).Trim();
                string toggledVar = NormalizeVarName(inner, lineNumber);
                if (string.Equals(toggledVar, targetVar, StringComparison.Ordinal))
                    return new DialogMutation(targetVar, DialogMutationOperator.Toggle);
            }

            // Arithmetic self-update: $var + N / $var - N
            if (TryParseSelfArithmetic(targetVar, rhs, lineNumber, out DialogMutationOperator arithOp, out DialogValue arithVal))
                return new DialogMutation(targetVar, arithOp, arithVal);

            return new DialogMutation(targetVar, DialogMutationOperator.Set, ParseLiteral(rhs, lineNumber));
        }

        private static bool TryParseSelfArithmetic(
            string targetVar,
            string rhs,
            int lineNumber,
            out DialogMutationOperator op,
            out DialogValue operand)
        {
            op = DialogMutationOperator.Set;
            operand = default;

            int plusIdx = rhs.IndexOf('+');
            int minusIdx = rhs.IndexOf('-', 1); // ignore leading minus on negative literal
            int opIdx = plusIdx > 0 ? plusIdx : minusIdx;
            if (opIdx <= 0)
                return false;

            string lhsToken = rhs.Substring(0, opIdx).Trim();
            string rhsToken = rhs.Substring(opIdx + 1).Trim();
            if (!lhsToken.StartsWith("$", StringComparison.Ordinal) && !string.Equals(lhsToken, targetVar, StringComparison.Ordinal))
                return false;

            string lhsVar = NormalizeVarName(lhsToken, lineNumber);
            if (!string.Equals(lhsVar, targetVar, StringComparison.Ordinal))
                return false;

            op = rhs[opIdx] == '+' ? DialogMutationOperator.Add : DialogMutationOperator.Subtract;
            operand = ParseLiteral(rhsToken, lineNumber);
            return true;
        }

        private static void SplitAssignment(string body, int lineNumber, out string varName, out string rhs)
        {
            int eqIndex = body.IndexOf('=');
            if (eqIndex > 0)
            {
                varName = NormalizeVarName(body.Substring(0, eqIndex).Trim(), lineNumber);
                rhs = body.Substring(eqIndex + 1).Trim();
                if (string.IsNullOrEmpty(rhs))
                    throw new DialogScriptParseException(lineNumber, "Assignment value cannot be empty.");
                return;
            }

            int toIndex = body.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
            if (toIndex > 0)
            {
                varName = NormalizeVarName(body.Substring(0, toIndex).Trim(), lineNumber);
                rhs = body.Substring(toIndex + 4).Trim();
                if (string.IsNullOrEmpty(rhs))
                    throw new DialogScriptParseException(lineNumber, "Assignment value cannot be empty.");
                return;
            }

            throw new DialogScriptParseException(lineNumber, $"Expected '=' or 'to' in assignment: '{body}'.");
        }

        private static void ParseConditions(string expression, int lineNumber, List<DialogCondition> output)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new DialogScriptParseException(lineNumber, "Empty <<if>> condition.");

            // Split by " and " or " && "
            List<string> clauses = SplitByAnd(expression);
            for (int i = 0; i < clauses.Count; i++)
            {
                string clause = clauses[i].Trim();
                if (string.IsNullOrEmpty(clause))
                    continue;

                output.Add(ParseSingleCondition(clause, lineNumber));
            }
        }

        private static List<string> SplitByAnd(string expression)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                    continue;
                }

                if (!inQuotes)
                {
                    if (i + 1 < expression.Length && expression[i] == '&' && expression[i + 1] == '&')
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        i++;
                        continue;
                    }

                    if (i + 4 < expression.Length &&
                        expression.Substring(i, 5).Equals(" and ", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        i += 4;
                        continue;
                    }
                }

                current.Append(c);
            }

            if (current.Length > 0)
                result.Add(current.ToString());

            return result;
        }

        private static DialogCondition ParseSingleCondition(string clause, int lineNumber)
        {
            string[] operators = { ">=", "<=", "==", "!=", " is ", ">", "<" };
            for (int i = 0; i < operators.Length; i++)
            {
                string token = operators[i];
                int idx = clause.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx <= 0)
                    continue;

                string varName = NormalizeVarName(clause.Substring(0, idx).Trim(), lineNumber);
                string rhs = clause.Substring(idx + token.Length).Trim();
                DialogValue val = ParseLiteral(rhs, lineNumber);

                DialogComparisonOperator op;
                switch (token)
                {
                    case ">=": op = DialogComparisonOperator.GreaterOrEqual; break;
                    case "<=": op = DialogComparisonOperator.LessOrEqual; break;
                    case "==":
                    case " is ": op = DialogComparisonOperator.Equal; break;
                    case "!=": op = DialogComparisonOperator.NotEqual; break;
                    case ">": op = DialogComparisonOperator.GreaterThan; break;
                    case "<": op = DialogComparisonOperator.LessThan; break;
                    default: throw new DialogScriptParseException(lineNumber, $"Unsupported operator '{token}'.");
                }

                return new DialogCondition(varName, op, val);
            }

            // Bare boolean check: "$flag", "!$flag", "not $flag"
            if (clause.StartsWith("!", StringComparison.Ordinal))
            {
                string varName = NormalizeVarName(clause.Substring(1).Trim(), lineNumber);
                return new DialogCondition(varName, DialogComparisonOperator.Equal, DialogValue.FromBool(false));
            }

            if (clause.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
            {
                string varName = NormalizeVarName(clause.Substring(4).Trim(), lineNumber);
                return new DialogCondition(varName, DialogComparisonOperator.Equal, DialogValue.FromBool(false));
            }

            string truthyVar = NormalizeVarName(clause, lineNumber);
            return new DialogCondition(truthyVar, DialogComparisonOperator.Equal, DialogValue.FromBool(true));
        }

        private static DialogCommand ParseCommand(string inner, int lineNumber)
        {
            List<string> tokens = TokenizeArguments(inner, lineNumber);
            if (tokens.Count == 0)
                throw new DialogScriptParseException(lineNumber, "Empty command '<<>>'.");

            string name = tokens[0];
            if (tokens.Count == 1)
                return new DialogCommand(name);

            var args = new string[tokens.Count - 1];
            for (int i = 1; i < tokens.Count; i++)
                args[i - 1] = tokens[i];

            return new DialogCommand(name, args);
        }

        private static List<string> TokenizeArguments(string text, int lineNumber)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (inQuotes)
                throw new DialogScriptParseException(lineNumber, "Unterminated quoted string in command.");

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
        }

        private static DialogValue ParseLiteral(string token, int lineNumber)
        {
            if (string.IsNullOrEmpty(token))
                throw new DialogScriptParseException(lineNumber, "Expected a value literal.");

            if (token.Length >= 2 && token[0] == '"' && token[token.Length - 1] == '"')
                return DialogValue.FromString(token.Substring(1, token.Length - 2));

            if (string.Equals(token, "true", StringComparison.OrdinalIgnoreCase))
                return DialogValue.FromBool(true);

            if (string.Equals(token, "false", StringComparison.OrdinalIgnoreCase))
                return DialogValue.FromBool(false);

            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
                return DialogValue.FromInt(intVal);

            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
                return DialogValue.FromFloat(floatVal);

            return DialogValue.FromString(token);
        }

        private static string NormalizeVarName(string rawName, int lineNumber)
        {
            string name = rawName.StartsWith("$", StringComparison.Ordinal) ? rawName.Substring(1) : rawName;
            if (string.IsNullOrWhiteSpace(name))
                throw new DialogScriptParseException(lineNumber, $"Invalid variable name '{rawName}'.");

            return name.Trim();
        }
    }
}

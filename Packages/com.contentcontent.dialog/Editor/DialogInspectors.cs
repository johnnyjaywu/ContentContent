using System;
using System.Collections.Generic;
using ContentContent.Dialog.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Dialog.Editor
{
    [CustomEditor(typeof(DialogGraphAsset))]
    internal sealed class DialogGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (DialogGraphAsset)target;
            IReadOnlyList<DialogValidationIssue> issues = asset.ValidateGraph();

            if (issues.Count == 0)
            {
                string startId = string.IsNullOrEmpty(asset.StartNodeId) ? "(unset)" : asset.StartNodeId;
                EditorGUILayout.HelpBox($"Dialog graph valid ({asset.NodeCount} nodes, Start: '{startId}').", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < issues.Count; i++)
                {
                    EditorGUILayout.HelpBox(issues[i].ToString(), MessageType.Error);
                }
            }

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(DialogRunnerBehaviour))]
    internal sealed class DialogRunnerBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var behaviour = (DialogRunnerBehaviour)target;

            DrawEditModeDiagnostics(behaviour);
            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            DrawPlayModeDebugger(behaviour);
        }

        private static void DrawEditModeDiagnostics(DialogRunnerBehaviour behaviour)
        {
            if (behaviour.InitialGraph == null && behaviour.InitialScript == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign an Initial Graph (ScriptableObject) or Initial Script (.txt), or call StartDialog(...) at runtime.",
                    MessageType.Info);
            }

            if (behaviour.InitialGraph != null)
            {
                IReadOnlyList<DialogValidationIssue> graphIssues = behaviour.InitialGraph.ValidateGraph();
                for (int i = 0; i < graphIssues.Count; i++)
                {
                    EditorGUILayout.HelpBox($"Initial Graph issue: {graphIssues[i]}", MessageType.Warning);
                }
            }

            if (behaviour.InitialScript != null)
            {
                try
                {
                    DialogGraph parsed = DialogScriptParser.Parse(behaviour.InitialScript.text, behaviour.InitialScript.name);
                    IReadOnlyList<DialogValidationIssue> scriptIssues = parsed.Validate();
                    if (scriptIssues.Count == 0)
                    {
                        EditorGUILayout.HelpBox(
                            $"Initial Script '{behaviour.InitialScript.name}' parsed cleanly ({parsed.Nodes.Count} nodes, Start: '{parsed.StartNodeId}').",
                            MessageType.Info);
                    }
                    else
                    {
                        for (int i = 0; i < scriptIssues.Count; i++)
                        {
                            EditorGUILayout.HelpBox($"Script validation: {scriptIssues[i]}", MessageType.Warning);
                        }
                    }
                }
                catch (DialogScriptParseException ex)
                {
                    EditorGUILayout.HelpBox($"Script syntax error in '{behaviour.InitialScript.name}': {ex.Message}", MessageType.Error);
                }
                catch (Exception ex)
                {
                    EditorGUILayout.HelpBox($"Script parse failure: {ex.Message}", MessageType.Error);
                }
            }

            IDialogView childView = behaviour.GetComponentInChildren<IDialogView>(includeInactive: true);
            if (childView == null)
            {
                EditorGUILayout.HelpBox(
                    "No child IDialogView component found. Subscribe to On Line Shown, install a Dialog sample view, or call SetView(...) in code.",
                    MessageType.Info);
            }
        }

        private void DrawPlayModeDebugger(DialogRunnerBehaviour behaviour)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Dialog Debugger", EditorStyles.boldLabel);

            DialogRunner runner = behaviour.Runner;
            EditorGUILayout.LabelField("Status", runner.IsRunning ? "Running" : "Idle");

            if (!runner.IsRunning)
            {
                if ((behaviour.InitialGraph != null || behaviour.InitialScript != null) &&
                    GUILayout.Button("Start Initial Dialog"))
                {
                    behaviour.StartInitialDialog();
                }
            }
            else
            {
                DialogNode node = runner.CurrentNode;
                if (node != null)
                {
                    EditorGUILayout.LabelField("Node Id", node.Id);
                    EditorGUILayout.LabelField("Speaker", runner.FormatText(node.Speaker));
                    EditorGUILayout.LabelField("Line", runner.FormatText(node.LineKey), EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.Space(4f);
                if (runner.CurrentChoices.Count > 0)
                {
                    EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);
                    for (int i = 0; i < runner.CurrentChoices.Count; i++)
                    {
                        DialogChoiceViewModel choice = runner.CurrentChoices[i];
                        if (GUILayout.Button($"[{i}] {choice.Text} -> {choice.TargetNodeId}"))
                        {
                            behaviour.SelectChoice(choice.Index);
                            break;
                        }
                    }
                }
                else if (GUILayout.Button("Continue"))
                {
                    behaviour.Continue();
                }

                if (GUILayout.Button("Stop Dialog"))
                {
                    behaviour.StopDialog();
                }
            }

            IReadOnlyDictionary<string, DialogValue> snapshot = behaviour.Variables.Snapshot();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Variables ({snapshot.Count})", EditorStyles.boldLabel);
            foreach (KeyValuePair<string, DialogValue> kvp in snapshot)
            {
                EditorGUILayout.LabelField(kvp.Key, $"{kvp.Value.ToDisplayString()} ({kvp.Value.Type})");
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }
}

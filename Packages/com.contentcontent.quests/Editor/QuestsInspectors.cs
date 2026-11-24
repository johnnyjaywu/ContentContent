using System.Collections.Generic;
using ContentContent.Quests.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Quests.Editor
{
    [CustomEditor(typeof(QuestDefinitionAsset))]
    internal sealed class QuestDefinitionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (QuestDefinitionAsset)target;
            List<string> issues = asset.ValidateAsset();
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox($"Quest '{asset.QuestId}' is valid.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < issues.Count; i++)
                {
                    EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
                }
            }

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(QuestLogComponent))]
    internal sealed class QuestLogComponentEditor : UnityEditor.Editor
    {
        private readonly List<QuestState> _activeScratch = new List<QuestState>();
        private readonly List<QuestState> _completedScratch = new List<QuestState>();

        public override void OnInspectorGUI()
        {
            var logComp = (QuestLogComponent)target;

            if (logComp.Catalog == null)
            {
                EditorGUILayout.HelpBox("Assign a Quest Catalog or register QuestDefinitionAssets in code or Starting Quests.", MessageType.Info);
            }

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Quest Tracker", EditorStyles.boldLabel);

            QuestJournal journal = logComp.Journal;
            journal.GetActiveQuests(_activeScratch);
            journal.GetCompletedQuests(_completedScratch);

            EditorGUILayout.LabelField($"Active Quests ({_activeScratch.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < _activeScratch.Count; i++)
            {
                QuestState q = _activeScratch[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{q.Definition.Title} [{q.Definition.ObjectiveMode}]", EditorStyles.boldLabel);
                if (GUILayout.Button("Fail", GUILayout.Width(48f)))
                {
                    journal.TryFailQuest(q.Definition.Id);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                for (int o = 0; o < q.Objectives.Count; o++)
                {
                    ObjectiveProgress obj = q.Objectives[o];
                    bool unlocked = q.IsObjectiveUnlocked(o);

                    EditorGUILayout.BeginHorizontal();
                    Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                    string lockTag = unlocked ? string.Empty : " (Locked)";
                    string optTag = obj.Definition.IsOptional ? " [Optional]" : string.Empty;
                    EditorGUI.ProgressBar(
                        rect,
                        obj.NormalizedProgress,
                        $"{obj.Definition.Description}{optTag}{lockTag}: {obj.CurrentCount}/{obj.Definition.TargetCount}");

                    GUI.enabled = unlocked && !obj.IsComplete;
                    if (GUILayout.Button("+1", GUILayout.Width(32f)))
                    {
                        journal.ReportProgress(q.Definition.Id, obj.Definition.Id, 1);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Completed Quests ({_completedScratch.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < _completedScratch.Count; i++)
            {
                QuestState q = _completedScratch[i];
                EditorGUILayout.LabelField(q.Definition.Title, $"Completed x{q.CompletionCount}");
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }
}

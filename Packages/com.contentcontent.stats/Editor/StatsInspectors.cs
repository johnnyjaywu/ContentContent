using System;
using System.Collections.Generic;
using ContentContent.Stats.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Stats.Editor
{
    [CustomEditor(typeof(StatSheetAsset))]
    internal sealed class StatSheetAssetEditor : UnityEditor.Editor
    {
        private readonly HashSet<string> _seenIds = new HashSet<string>(StringComparer.Ordinal);

        public override void OnInspectorGUI()
        {
            var sheet = (StatSheetAsset)target;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Starter RPG Stats (MaxHealth, Attack, Defense, MoveSpeed)"))
            {
                Undo.RecordObject(sheet, "Fill Starter RPG Stats");
                sheet.ApplyStarterRpgPreset();
                EditorUtility.SetDirty(sheet);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            DrawDuplicateWarnings(sheet);

            DrawDefaultInspector();
        }

        private void DrawDuplicateWarnings(StatSheetAsset sheet)
        {
            _seenIds.Clear();
            IReadOnlyList<StatSheetAsset.Entry> entries = sheet.Entries;
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                string id = entries[i].statId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    EditorGUILayout.HelpBox($"Entry {i} has an empty Stat Id and will be skipped.", MessageType.Warning);
                }
                else if (!_seenIds.Add(id.Trim()))
                {
                    EditorGUILayout.HelpBox($"Duplicate Stat Id '{id}' found; later entries overwrite earlier ones.", MessageType.Warning);
                }
            }
        }
    }

    [CustomEditor(typeof(StatsComponent))]
    internal sealed class StatsComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var statsComp = (StatsComponent)target;

            if (statsComp.InitialSheet == null)
            {
                EditorGUILayout.HelpBox("No Initial Sheet assigned. Stats start empty unless registered via script (Create -> ContentContent -> Stats -> Stat Sheet).", MessageType.Info);
            }

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Stats", EditorStyles.boldLabel);

            foreach (KeyValuePair<string, Stat> kvp in statsComp.Stats.Stats)
            {
                Stat s = kvp.Value;
                EditorGUILayout.LabelField(kvp.Key, $"{s.Value:0.##} (Base {s.BaseValue:0.##}, {s.Modifiers.Count} mod)");
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Active Buffs ({statsComp.Buffs.ActiveBuffs.Count})", EditorStyles.boldLabel);

            for (int i = 0; i < statsComp.Buffs.ActiveBuffs.Count; i++)
            {
                ActiveBuff<string> buff = statsComp.Buffs.ActiveBuffs[i];
                if (buff.IsPermanent)
                {
                    EditorGUILayout.LabelField($"{buff.Definition.Id} (x{buff.Stacks})", "Permanent");
                }
                else
                {
                    Rect barRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                    float ratio = buff.Definition.Duration > 0f
                        ? Mathf.Clamp01(buff.RemainingDuration / buff.Definition.Duration)
                        : 1f;
                    EditorGUI.ProgressBar(barRect, ratio, $"{buff.Definition.Id} (x{buff.Stacks}) — {buff.RemainingDuration:0.0}s");
                }
            }

            if (statsComp.Buffs.ActiveBuffs.Count > 0 && GUILayout.Button("Clear All Buffs"))
            {
                statsComp.Buffs.ClearAll();
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(HealthComponent))]
    internal sealed class HealthComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _statsProp;

        private void OnEnable()
        {
            _statsProp = serializedObject.FindProperty("_statsComponent");
        }

        public override void OnInspectorGUI()
        {
            var healthComp = (HealthComponent)target;

            serializedObject.Update();

            if (healthComp.BindToStatsComponent)
            {
                StatsComponent sibling = healthComp.GetComponent<StatsComponent>();
                if (_statsProp.objectReferenceValue == null && sibling != null)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox("Sibling StatsComponent found (will auto-bind on Awake, or assign explicitly now).", MessageType.Info);
                    if (GUILayout.Button("Assign Sibling", GUILayout.Width(120f), GUILayout.Height(34f)))
                    {
                        _statsProp.objectReferenceValue = sibling;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Health", EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(20f, 20f, GUILayout.ExpandWidth(true));
            float ratio = healthComp.Max > 0f ? Mathf.Clamp01(healthComp.Current / healthComp.Max) : 0f;
            EditorGUI.ProgressBar(rect, ratio, $"HP: {healthComp.Current:0.#} / {healthComp.Max:0.#} ({(healthComp.IsAlive ? "Alive" : "Defeated")})");
            EditorGUILayout.LabelField("Stat Binding", healthComp.IsBoundToStat ? $"Bound to '{healthComp.MaxHealthStatId}'" : "Standalone (_maxHealth)");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage 10")) healthComp.ApplyFlatDamage(10f);
            if (GUILayout.Button("Heal 10")) healthComp.Heal(10f);
            if (GUILayout.Button("Revive")) healthComp.Revive();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }
}

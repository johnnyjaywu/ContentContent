using System.Collections.Generic;
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using ContentContent.Core.Fsm.Unity;
using ContentContent.Core.Grid.Unity;
using ContentContent.Core.Timers.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Core.Editor
{
    [CustomPropertyDrawer(typeof(BlackboardBehaviour.InitialEntry))]
    internal sealed class BlackboardInitialEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty keyProp = property.FindPropertyRelative("key");
            SerializedProperty typeProp = property.FindPropertyRelative("type");

            float gap = 4f;
            float typeWidth = 72f;
            float keyWidth = Mathf.Max(60f, (position.width - typeWidth - gap * 2f) * 0.45f);
            float valWidth = Mathf.Max(40f, position.width - keyWidth - typeWidth - gap * 2f);

            var keyRect = new Rect(position.x, position.y, keyWidth, EditorGUIUtility.singleLineHeight);
            var typeRect = new Rect(keyRect.xMax + gap, position.y, typeWidth, EditorGUIUtility.singleLineHeight);
            var valRect = new Rect(typeRect.xMax + gap, position.y, valWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            var valueType = (BlackboardBehaviour.InitialValueType)typeProp.enumValueIndex;
            string fieldName;
            switch (valueType)
            {
                case BlackboardBehaviour.InitialValueType.Int:
                    fieldName = "intValue";
                    break;
                case BlackboardBehaviour.InitialValueType.Float:
                    fieldName = "floatValue";
                    break;
                case BlackboardBehaviour.InitialValueType.String:
                    fieldName = "stringValue";
                    break;
                case BlackboardBehaviour.InitialValueType.Bool:
                default:
                    fieldName = "boolValue";
                    break;
            }

            EditorGUI.PropertyField(valRect, property.FindPropertyRelative(fieldName), GUIContent.none);
            EditorGUI.EndProperty();
        }
    }

    [CustomEditor(typeof(BlackboardBehaviour))]
    internal sealed class BlackboardBehaviourEditor : UnityEditor.Editor
    {
        private readonly List<KeyValuePair<string, string>> _entries = new List<KeyValuePair<string, string>>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var bb = (BlackboardBehaviour)target;
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Blackboard", EditorStyles.boldLabel);

            bb.Blackboard.CopyDebugEntries(_entries);
            if (_entries.Count == 0)
            {
                EditorGUILayout.LabelField("(No local keys set)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    EditorGUILayout.LabelField(_entries[i].Key, _entries[i].Value);
                }
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(StateMachineRunner))]
    internal sealed class StateMachineRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var runner = (StateMachineRunner)target;
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live HFSM", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Initialized", runner.Machine != null ? "Yes" : "No");
            EditorGUILayout.LabelField("Active State", runner.CurrentStateName);
            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(EventBusBehaviour))]
    internal sealed class EventBusBehaviourEditor : UnityEditor.Editor
    {
        private readonly List<KeyValuePair<string, int>> _stats = new List<KeyValuePair<string, int>>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var busBehaviour = (EventBusBehaviour)target;
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Event Channels", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Channel Count", busBehaviour.Bus.ChannelCount.ToString());

            busBehaviour.Bus.CopyChannelStats(_stats);
            for (int i = 0; i < _stats.Count; i++)
            {
                EditorGUILayout.LabelField(_stats[i].Key, $"{_stats[i].Value} subscriber(s)");
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(TimerSchedulerRunner))]
    internal sealed class TimerSchedulerRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var runner = (TimerSchedulerRunner)target;
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Timers", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Timers", runner.Scheduler.ActiveCount.ToString());
            EditorGUILayout.LabelField("Time Scale", runner.Scheduler.TimeScale.ToString("0.00"));
            if (GUILayout.Button("Cancel All Timers"))
            {
                runner.Scheduler.CancelAll();
            }
            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(GridBehaviour))]
    internal sealed class GridBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var grid = (GridBehaviour)target;
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Grid Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Dimensions", $"{grid.Width} x {grid.Height} ({grid.Width * grid.Height} cells)");
            EditorGUILayout.LabelField("World Span", $"{grid.Width * grid.CellSize:0.##} x {grid.Height * grid.CellSize:0.##} ({grid.Plane})");
            EditorGUILayout.EndVertical();
        }
    }
}

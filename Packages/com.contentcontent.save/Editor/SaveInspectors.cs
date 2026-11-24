using System;
using System.Collections.Generic;
using System.IO;
using ContentContent.Save.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Save.Editor
{
    [CustomEditor(typeof(SaveManagerBehaviour))]
    internal sealed class SaveManagerBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var managerBehaviour = (SaveManagerBehaviour)target;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Save Storage & Schema", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Schema Version", $"v{managerBehaviour.SchemaVersion}");
            EditorGUILayout.LabelField("Backend", managerBehaviour.UseInMemoryStorage
                ? "In-Memory (RAM only)"
                : managerBehaviour.ResolvedSaveDirectory);

            if (!managerBehaviour.UseInMemoryStorage && GUILayout.Button("Open Save Folder"))
            {
                string dir = managerBehaviour.ResolvedSaveDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                EditorUtility.RevealInFinder(dir);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Save Slots", EditorStyles.boldLabel);

            SaveManager manager = managerBehaviour.Manager;
            EditorGUILayout.LabelField("Registered Participants", manager.ParticipantCount.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Default"))
                managerBehaviour.SaveDefaultSlot();
            if (GUILayout.Button("Load Default"))
                managerBehaviour.LoadDefaultSlot();
            if (GUILayout.Button("Save Autosave"))
                managerBehaviour.SaveAutosave();
            EditorGUILayout.EndHorizontal();

            List<SaveMetadata> slots = manager.ListSlotMetadata();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Stored Slots ({slots.Count})", EditorStyles.boldLabel);

            for (int i = 0; i < slots.Count; i++)
            {
                SaveMetadata meta = slots[i];
                EditorGUILayout.BeginHorizontal();
                var timestamp = meta.SavedAtUtcTicks > 0
                    ? new DateTime(meta.SavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("HH:mm:ss")
                    : "-";
                EditorGUILayout.LabelField($"{meta.SlotId} (v{meta.SchemaVersion} @ {timestamp}, {meta.PlayTimeSeconds:0.0}s)");
                if (GUILayout.Button("Load", GUILayout.Width(54f)))
                {
                    managerBehaviour.LoadSlot(meta.SlotId);
                }
                if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                {
                    managerBehaviour.DeleteSlot(meta.SlotId);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(SaveableBehaviour), editorForChildClasses: true)]
    internal sealed class SaveableBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _saveIdProp;

        private void OnEnable()
        {
            _saveIdProp = serializedObject.FindProperty("_saveId");
        }

        public override void OnInspectorGUI()
        {
            var self = (SaveableBehaviour)target;

            serializedObject.Update();
            if (_saveIdProp != null)
            {
                string currentId = _saveIdProp.stringValue;
                bool isEmpty = string.IsNullOrWhiteSpace(currentId);
                bool isDuplicate = !isEmpty && HasDuplicateSaveId(self, currentId);

                if (isEmpty || isDuplicate)
                {
                    string message = isEmpty
                        ? "Save Id is empty. This object will not participate in saves until assigned a unique Save Id."
                        : $"Duplicate Save Id '{currentId}' detected in scene! Multiple objects sharing an id overwrite each other.";

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox(message, isEmpty ? MessageType.Warning : MessageType.Error);
                    if (GUILayout.Button("Generate Unique ID", GUILayout.Width(138f), GUILayout.Height(36f)))
                    {
                        _saveIdProp.stringValue = Guid.NewGuid().ToString("N");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();
        }

        private static bool HasDuplicateSaveId(SaveableBehaviour self, string id)
        {
            SaveableBehaviour[] all = UnityEngine.Object.FindObjectsByType<SaveableBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                SaveableBehaviour other = all[i];
                if (other == null || other == self)
                    continue;

                if (string.Equals(other.SaveId, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}

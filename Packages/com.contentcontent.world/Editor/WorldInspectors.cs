using System;
using System.Collections.Generic;
using ContentContent.World.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.World.Editor
{
    [CustomEditor(typeof(WorldGraphAsset))]
    internal sealed class WorldGraphAssetEditor : UnityEditor.Editor
    {
        private string _assumedAbilitiesCsv = string.Empty;

        public override void OnInspectorGUI()
        {
            var asset = (WorldGraphAsset)target;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Static Reachability & Soft-Lock Validator", EditorStyles.boldLabel);
            _assumedAbilitiesCsv = EditorGUILayout.TextField(
                new GUIContent("Assume Abilities (CSV)", "Optional comma-separated starting abilities for testing mid-game progression."),
                _assumedAbilitiesCsv);

            string[] abilities = string.IsNullOrWhiteSpace(_assumedAbilitiesCsv)
                ? null
                : _assumedAbilitiesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (abilities != null)
            {
                for (int i = 0; i < abilities.Length; i++)
                    abilities[i] = abilities[i].Trim();
            }

            WorldValidationReport report = asset.ValidateReachability(abilities);

            if (report.DuplicateRoomIds != null && report.DuplicateRoomIds.Count > 0)
            {
                EditorGUILayout.HelpBox($"Duplicate room IDs: {string.Join(", ", report.DuplicateRoomIds)}", MessageType.Error);
            }

            if (!report.HasStartRoom)
            {
                EditorGUILayout.HelpBox($"Start Room Id '{asset.StartRoomId}' was not found in Rooms.", MessageType.Warning);
            }
            else if (report.IsValid)
            {
                int count = asset.Rooms != null ? asset.Rooms.Count : 0;
                EditorGUILayout.HelpBox($"All {count} rooms are reachable from '{asset.StartRoomId}' with 0 soft-locks.", MessageType.Info);
            }
            else
            {
                if (report.UnreachableRooms != null && report.UnreachableRooms.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Unreachable rooms: {string.Join(", ", report.UnreachableRooms)}", MessageType.Warning);
                }

                if (report.SoftLockRooms != null && report.SoftLockRooms.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Soft-lock rooms (no return path to start or checkpoint): {string.Join(", ", report.SoftLockRooms)}", MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(WorldManagerBehaviour))]
    internal sealed class WorldManagerBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = (WorldManagerBehaviour)target;

            if (manager.WorldGraphAsset == null)
            {
                EditorGUILayout.HelpBox("No WorldGraphAsset assigned. Transitions still work directly by roomId, or assign a WorldGraphAsset for gated exits and soft-lock checks.", MessageType.Info);
            }

            if (manager.PlayerTransform == null)
            {
                EditorGUILayout.HelpBox("Player Transform is empty. RoomSpawnPoints won't automatically teleport the player until assigned.", MessageType.Warning);
            }

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode World Telemetry", EditorStyles.boldLabel);

            RoomLifecycleController ctrl = manager.Controller;
            EditorGUILayout.LabelField("Current Room", string.IsNullOrEmpty(ctrl.CurrentRoomId) ? "(none)" : $"{ctrl.CurrentRoomId} ({ctrl.CurrentSpawnPointId})");
            EditorGUILayout.LabelField("Active Checkpoint", ctrl.Checkpoints.HasActiveCheckpoint ? $"{ctrl.Checkpoints.ActiveCheckpoint.Value.CheckpointId} @ {ctrl.Checkpoints.ActiveCheckpoint.Value.RoomId}" : "None");
            EditorGUILayout.LabelField("Safe Ground", ctrl.Checkpoints.HasSafeGround ? $"{ctrl.Checkpoints.LastSafeGroundRoomId} {ctrl.Checkpoints.LastSafeGroundPosition}" : "None");
            EditorGUILayout.LabelField("Visited Rooms", ctrl.Flags.VisitedRoomCount.ToString());

            Dictionary<string, WorldFlagValue> flags = ctrl.Flags.ExportFlags();
            EditorGUILayout.LabelField($"Flags ({flags.Count})", EditorStyles.boldLabel);
            foreach (KeyValuePair<string, WorldFlagValue> kvp in flags)
            {
                EditorGUILayout.LabelField(kvp.Key, FormatFlagValue(kvp.Value));
            }

            EditorGUILayout.BeginHorizontal();
            if (ctrl.Checkpoints.HasActiveCheckpoint && GUILayout.Button("Respawn at Checkpoint"))
            {
                manager.TriggerDeathRespawn();
            }
            if (!string.IsNullOrEmpty(ctrl.CurrentRoomId) && GUILayout.Button("Reset Current Room"))
            {
                manager.ResetCurrentRoom();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            Repaint();
        }

        private static string FormatFlagValue(WorldFlagValue value)
        {
            switch (value.Type)
            {
                case WorldFlagType.Bool: return value.BoolValue.ToString();
                case WorldFlagType.Int: return value.IntValue.ToString();
                case WorldFlagType.String: return value.StringValue;
                default: return "None";
            }
        }
    }

#if CONTENTCONTENT_PHYSICS2D
    internal static class WorldTriggerDiagnostics
    {
        public static void DrawTriggerChecks(MonoBehaviour component, SerializedObject serializedObject)
        {
            Collider2D col = component.GetComponent<Collider2D>();
            if (col == null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("Missing Collider2D for 2D trigger detection.", MessageType.Warning);
                if (GUILayout.Button("Add BoxCollider2D", GUILayout.Width(130f), GUILayout.Height(34f)))
                {
                    BoxCollider2D added = Undo.AddComponent<BoxCollider2D>(component.gameObject);
                    added.isTrigger = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (!col.isTrigger)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("Collider2D should have Is Trigger enabled for automatic detection.", MessageType.Warning);
                if (GUILayout.Button("Enable Is Trigger", GUILayout.Width(130f), GUILayout.Height(34f)))
                {
                    Undo.RecordObject(col, "Enable Is Trigger");
                    col.isTrigger = true;
                    EditorUtility.SetDirty(col);
                }
                EditorGUILayout.EndHorizontal();
            }

            SerializedProperty mgrProp = serializedObject.FindProperty("_worldManager");
            if (mgrProp != null && mgrProp.objectReferenceValue == null)
            {
                WorldManagerBehaviour sceneMgr = UnityEngine.Object.FindAnyObjectByType<WorldManagerBehaviour>();
                if (sceneMgr == null)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox("No WorldManagerBehaviour found in scene.", MessageType.Warning);
                    if (GUILayout.Button("Create WorldManager", GUILayout.Width(140f), GUILayout.Height(34f)))
                    {
                        var go = new GameObject("WorldManager");
                        WorldManagerBehaviour created = go.AddComponent<WorldManagerBehaviour>();
                        Undo.RegisterCreatedObjectUndo(go, "Create WorldManager");
                        mgrProp.objectReferenceValue = created;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    [CustomEditor(typeof(CheckpointBehaviour2D))]
    internal sealed class CheckpointBehaviour2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            WorldTriggerDiagnostics.DrawTriggerChecks((MonoBehaviour)target, serializedObject);
            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(HazardZone2D))]
    internal sealed class HazardZone2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            WorldTriggerDiagnostics.DrawTriggerChecks((MonoBehaviour)target, serializedObject);
            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(RoomTransitionTrigger2D))]
    internal sealed class RoomTransitionTrigger2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            WorldTriggerDiagnostics.DrawTriggerChecks((MonoBehaviour)target, serializedObject);
            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }
    }
#endif
}

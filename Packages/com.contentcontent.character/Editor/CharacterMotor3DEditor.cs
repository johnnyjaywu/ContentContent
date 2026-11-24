#if CONTENTCONTENT_PHYSICS
using System;
using ContentContent.Character.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Character.Editor
{
    [CustomEditor(typeof(CharacterMotor3D))]
    internal sealed class CharacterMotor3DEditor : UnityEditor.Editor
    {
        private const string Input3DAssemblyQualifiedName = "ContentContent.Character.Unity.Input.CharacterInput3D, ContentContent.Character.Input";

        private SerializedProperty _configAssetProp;
        private SerializedProperty _inlineConfigProp;
        private SerializedProperty _liveSyncProp;
        private SerializedProperty _groundMaskProp;

        private bool _showSensors = true;
        private bool _showIntegrations;
        private bool _showActions = true;
        private bool _showEvents;

        private void OnEnable()
        {
            _configAssetProp = serializedObject.FindProperty("_configAsset");
            _inlineConfigProp = serializedObject.FindProperty("_inlineConfig");
            _liveSyncProp = serializedObject.FindProperty("_liveSyncConfigInEditor");
            _groundMaskProp = serializedObject.FindProperty("_groundMask");
        }

        public override void OnInspectorGUI()
        {
            var motor = (CharacterMotor3D)target;

            serializedObject.Update();

            DrawDiagnostics(motor);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_configAssetProp);
            EditorGUILayout.PropertyField(_liveSyncProp);

            if (_configAssetProp.objectReferenceValue == null)
            {
                EditorGUILayout.Space(4f);
                CharacterConfigInspectorUI.DrawConfig(
                    _inlineConfigProp,
                    motor.InlineConfig,
                    motor,
                    preset => motor.ApplyPreset(preset),
                    allow2DPresets: false,
                    allow3DPreset: true);
            }
            else
            {
                EditorGUILayout.HelpBox("Using shared CharacterConfigAsset. Select the asset to edit shared values across characters.", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Config Asset"))
                {
                    Selection.activeObject = _configAssetProp.objectReferenceValue;
                }
                if (GUILayout.Button("Copy Asset to Inline"))
                {
                    var asset = (CharacterConfigAsset)_configAssetProp.objectReferenceValue;
                    Undo.RecordObject(motor, "Copy Config Asset to Inline");
                    motor.InlineConfig.CopyFrom(asset.Config);
                    _configAssetProp.objectReferenceValue = null;
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            _showSensors = EditorGUILayout.Foldout(_showSensors, "3D Physics Sensors", true);
            if (_showSensors)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (GUILayout.Button("Auto-Fit Sensors to CharacterController"))
                {
                    Undo.RecordObject(motor, "Auto-Fit 3D Sensors");
                    motor.AutoFitSensorsFromController();
                    EditorUtility.SetDirty(motor);
                    serializedObject.Update();
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_groundMaskProp);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_groundCheckOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_groundCheckRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_ceilingCheckOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_ceilingCheckRadius"));
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            _showActions = EditorGUILayout.Foldout(_showActions, "Actions (Code-Free)", true);
            if (_showActions)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_actions"), true);
            }

            _showIntegrations = EditorGUILayout.Foldout(_showIntegrations, "Optional Core Integrations", true);
            if (_showIntegrations)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_blackboardBehaviour"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_eventBusBehaviour"));
                EditorGUI.indentLevel--;
            }

            _showEvents = EditorGUILayout.Foldout(_showEvents, "UnityEvents (Audio / VFX / State)", true);
            if (_showEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onJumped"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onLanded"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onDashed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onFacingChanged"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onStateChanged"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying && motor.Controller != null)
            {
                CharacterConfigInspectorUI.DrawTelemetry(motor.Controller);
                Repaint();
            }
        }

        private void DrawDiagnostics(CharacterMotor3D motor)
        {
            CharacterConfig active = motor.ActiveConfigSource;
            if (active != null && active.Mode != MovementMode.ThirdPerson3D)
            {
                DrawFixBox("CharacterMotor3D is intended for ThirdPerson3D mode.", MessageType.Warning, "Switch to ThirdPerson3D", () =>
                {
                    Undo.RecordObject(motor, "Switch to ThirdPerson3D");
                    motor.ApplyPreset(CharacterPresetType.ThirdPerson);
                    EditorUtility.SetDirty(motor);
                    serializedObject.Update();
                });
            }

            if (_groundMaskProp.intValue == 0)
            {
                DrawFixBox("Ground Mask is Nothing (0), so sphere ground/ceiling probes will ignore all surfaces.", MessageType.Warning, "Include Everything", () =>
                {
                    _groundMaskProp.intValue = ~0;
                });
            }

            Type input3DType = CharacterConfigInspectorUI.ResolveOptionalType(Input3DAssemblyQualifiedName);
            if (input3DType != null && motor.GetComponent(input3DType) == null)
            {
                DrawFixBox("No CharacterInput3D attached. Add one for instant camera-relative Keyboard/Gamepad controls, or drive this motor from your own script.", MessageType.Info, "Add CharacterInput3D", () =>
                {
                    Undo.AddComponent(motor.gameObject, input3DType);
                });
            }
        }

        private static void DrawFixBox(string message, MessageType type, string buttonLabel, Action onFix)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(message, type);
            if (GUILayout.Button(buttonLabel, GUILayout.Width(160f), GUILayout.Height(36f)))
            {
                onFix?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

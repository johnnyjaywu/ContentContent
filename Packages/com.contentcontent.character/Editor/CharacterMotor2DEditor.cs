#if CONTENTCONTENT_PHYSICS2D
using System;
using ContentContent.Character.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Character.Editor
{
    [CustomEditor(typeof(CharacterMotor2D))]
    internal sealed class CharacterMotor2DEditor : UnityEditor.Editor
    {
        private const string Input2DAssemblyQualifiedName = "ContentContent.Character.Unity.Input.CharacterInput2D, ContentContent.Character.Input";

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
            var motor = (CharacterMotor2D)target;

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
                    allow2DPresets: true,
                    allow3DPreset: false);
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

            bool isSideScroller = motor.ActiveConfigSource == null || motor.ActiveConfigSource.Mode == MovementMode.SideScroller2D;
            if (isSideScroller)
            {
                DrawSensorsSection(motor);
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_onWallJumped"));
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

        private void DrawSensorsSection(CharacterMotor2D motor)
        {
            _showSensors = EditorGUILayout.Foldout(_showSensors, "2D Physics Sensors & Corner Correction", true);
            if (!_showSensors)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button("Auto-Fit Sensors to Collider"))
            {
                Undo.RecordObject(motor, "Auto-Fit 2D Sensors");
                if (!motor.AutoFitSensorsFromCollider())
                {
                    Debug.LogWarning("Add a Collider2D first before auto-fitting sensors.", motor);
                }
                else
                {
                    EditorUtility.SetDirty(motor);
                    serializedObject.Update();
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_groundMaskProp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_oneWayPlatformMask"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_groundCheckOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_groundCheckSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_ceilingCheckOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_ceilingCheckSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_wallMask"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_wallCheckOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_wallCheckSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableCornerCorrection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cornerCorrectionDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cornerCorrectionProbeHeight"));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics(CharacterMotor2D motor)
        {
            Rigidbody2D rb = motor.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (!Mathf.Approximately(rb.gravityScale, 0f))
                {
                    DrawFixBox("Rigidbody2D Gravity Scale should be 0 because CharacterMotor2D computes custom gravity & fall curves.", MessageType.Warning, "Set Gravity Scale = 0", () =>
                    {
                        Undo.RecordObject(rb, "Fix Rigidbody2D Gravity Scale");
                        rb.gravityScale = 0f;
                    });
                }

                if (!rb.freezeRotation)
                {
                    DrawFixBox("Rigidbody2D Freeze Rotation Z should be enabled so collisions do not tip the character over.", MessageType.Warning, "Freeze Rotation Z", () =>
                    {
                        Undo.RecordObject(rb, "Freeze Rigidbody2D Rotation");
                        rb.freezeRotation = true;
                    });
                }
            }

            if (motor.GetComponent<Collider2D>() == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("No Collider2D attached. Add a BoxCollider2D or CapsuleCollider2D so the character collides with terrain.", MessageType.Error);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add BoxCollider2D"))
                {
                    Undo.AddComponent<BoxCollider2D>(motor.gameObject);
                    Undo.RecordObject(motor, "Auto-Fit 2D Sensors");
                    motor.AutoFitSensorsFromCollider();
                }
                if (GUILayout.Button("Add CapsuleCollider2D"))
                {
                    Undo.AddComponent<CapsuleCollider2D>(motor.gameObject);
                    Undo.RecordObject(motor, "Auto-Fit 2D Sensors");
                    motor.AutoFitSensorsFromCollider();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            CharacterConfig active = motor.ActiveConfigSource;
            if (active != null && active.Mode == MovementMode.ThirdPerson3D)
            {
                DrawFixBox("CharacterMotor2D does not run in ThirdPerson3D mode. Switch to SideScroller2D or TopDown2D.", MessageType.Error, "Switch to SideScroller2D", () =>
                {
                    Undo.RecordObject(motor, "Switch to SideScroller2D");
                    motor.ApplyPreset(CharacterPresetType.SideScroller);
                    EditorUtility.SetDirty(motor);
                    serializedObject.Update();
                });
            }

            if (active != null && active.Mode == MovementMode.SideScroller2D && _groundMaskProp.intValue == 0)
            {
                DrawFixBox("Ground Mask is Nothing (0), so the character will never detect ground.", MessageType.Warning, "Include Everything", () =>
                {
                    _groundMaskProp.intValue = ~0;
                });
            }

            Type input2DType = CharacterConfigInspectorUI.ResolveOptionalType(Input2DAssemblyQualifiedName);
            if (input2DType != null && motor.GetComponent(input2DType) == null)
            {
                DrawFixBox("No CharacterInput2D attached. Add one for instant Keyboard/Gamepad controls, or drive this motor from your own script.", MessageType.Info, "Add CharacterInput2D", () =>
                {
                    Undo.AddComponent(motor.gameObject, input2DType);
                });
            }
        }

        private static void DrawFixBox(string message, MessageType type, string buttonLabel, Action onFix)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(message, type);
            if (GUILayout.Button(buttonLabel, GUILayout.Width(155f), GUILayout.Height(36f)))
            {
                onFix?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

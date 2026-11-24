using ContentContent.Interaction.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Interaction.Editor
{
    [CustomEditor(typeof(InteractorBehaviour))]
    internal sealed class InteractorBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _maskProp;

        private void OnEnable()
        {
            _maskProp = serializedObject.FindProperty("_interactableMask");
        }

        public override void OnInspectorGUI()
        {
            var interactor = (InteractorBehaviour)target;

            serializedObject.Update();
            if (_maskProp != null && _maskProp.intValue == 0)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("Interactable Mask is Nothing. No interactables can be detected.", MessageType.Warning);
                if (GUILayout.Button("Set Everything (~0)", GUILayout.Width(138f), GUILayout.Height(36f)))
                {
                    _maskProp.intValue = ~0;
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Focus Telemetry", EditorStyles.boldLabel);

            InteractionSelector sel = interactor.Selector;
            EditorGUILayout.LabelField("Focused", sel.HasFocus ? sel.FocusedInteractable.InteractableId : "(none)");
            if (sel.HasFocus)
            {
                EditorGUILayout.LabelField("Prompt", sel.CurrentPrompt.FormatDisplayText());
                EditorGUILayout.LabelField("Can Interact", sel.CurrentPrompt.CanInteract ? "Yes" : $"No ({sel.CurrentPrompt.BlockedReason})");

                if (sel.CurrentPrompt.HoldDuration > 0f)
                {
                    Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(rect, sel.HoldProgress, $"Hold: {sel.HoldProgress * 100f:0}%");
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Focus"))
                interactor.RefreshFocusNow();
            if (GUILayout.Button("Trigger Interact"))
                interactor.TryInteractNow();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(InteractableBehaviour))]
    internal sealed class InteractableBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var interactable = (InteractableBehaviour)target;

            bool hasCollider = false;
#if CONTENTCONTENT_PHYSICS2D
            if (interactable.GetComponentInChildren<Collider2D>(true) != null)
                hasCollider = true;
#endif
#if CONTENTCONTENT_PHYSICS
            if (interactable.GetComponentInChildren<Collider>(true) != null)
                hasCollider = true;
#endif

            if (!hasCollider)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("No Collider or Collider2D found in hierarchy. Add one so InteractorBehaviour can detect this object.", MessageType.Warning);
                EditorGUILayout.BeginHorizontal();
#if CONTENTCONTENT_PHYSICS2D
                if (GUILayout.Button("Add BoxCollider2D (Trigger)"))
                {
                    BoxCollider2D c = Undo.AddComponent<BoxCollider2D>(interactable.gameObject);
                    c.isTrigger = true;
                }
#endif
#if CONTENTCONTENT_PHYSICS
                if (GUILayout.Button("Add BoxCollider (Trigger)"))
                {
                    BoxCollider c = Undo.AddComponent<BoxCollider>(interactable.gameObject);
                    c.isTrigger = true;
                }
#endif
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Interactable State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Locked", interactable.IsLocked ? "Yes" : "No");
            EditorGUILayout.LabelField("Used (Single-Use)", interactable.HasUsed ? "Yes" : "No");
            EditorGUILayout.LabelField("Cooldown", $"{interactable.CooldownRemaining:0.0}s");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Interact"))
                interactable.TryInteract(new InteractionContext(interactable.WorldPosition, System.Numerics.Vector3.UnitZ));
            if (GUILayout.Button(interactable.IsLocked ? "Unlock" : "Lock"))
                interactable.SetLocked(!interactable.IsLocked);
            if (GUILayout.Button("Reset"))
                interactable.ResetInteractable();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }
}

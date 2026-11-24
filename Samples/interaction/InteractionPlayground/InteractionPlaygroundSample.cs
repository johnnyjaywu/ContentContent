using System.Collections.Generic;
using ContentContent.Interaction.Unity;
using UnityEngine;

namespace ContentContent.Interaction.Samples
{
    /// <summary>
    /// Interactive 2D playground demonstrating <see cref="InteractorBehaviour"/> focus scoring,
    /// hold-to-interact chest opening, locked lever unlocking, and zero-code prompt hooks.
    /// </summary>
    public sealed class InteractionPlaygroundSample : MonoBehaviour
    {
        private InteractorBehaviour _interactor;
        private InteractableBehaviour _lockedDoor;
        private Transform _playerTransform;
        private string _currentPrompt = "(Walk near an object)";
        private float _holdProgress;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(-2f, 0f, 0f);
            _playerTransform = playerGo.transform;

            _interactor = playerGo.AddComponent<InteractorBehaviour>();
            _interactor.DetectionMode = InteractionDetectionMode.Physics2D;
            _interactor.OnPromptChanged.AddListener(p => _currentPrompt = p);
            _interactor.OnPromptCleared.AddListener(() => _currentPrompt = "(Nothing in range)");
            _interactor.OnHoldProgress.AddListener(p => _holdProgress = p);
            _interactor.OnInteracted.AddListener(id => AppendLog($"Interacted with '{id}'"));

            CreateInteractable("Chest (Hold 1s)", new Vector3(0f, 0f, 0f), holdDuration: 1f, locked: false);
            _lockedDoor = CreateInteractable("Security Gate", new Vector3(2.5f, 0f, 0f), holdDuration: 0f, locked: true);
        }

        private InteractableBehaviour CreateInteractable(string label, Vector3 pos, float holdDuration, bool locked)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.position = pos;
            Collider c3d = go.GetComponent<Collider>();
            if (c3d != null)
                Destroy(c3d);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var comp = go.AddComponent<InteractableBehaviour>();
            if (locked)
                comp.SetLocked(true, "Press Unlock Gate button");

            var holdField = typeof(InteractableBehaviour).GetField("_holdDuration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            holdField?.SetValue(comp, holdDuration);

            var nameField = typeof(InteractableBehaviour).GetField("_displayName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            nameField?.SetValue(comp, label);

            return comp;
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 6)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 400, 320), "Interaction Playground", GUI.skin.window);
            GUILayout.Label($"Prompt: {_currentPrompt}");
            GUILayout.Label($"Hold Progress: {_holdProgress * 100f:0}%");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Left") && _playerTransform != null)
                _playerTransform.position += Vector3.left * 0.75f;
            if (GUILayout.Button("Move Right") && _playerTransform != null)
                _playerTransform.position += Vector3.right * 0.75f;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tap / Begin Interact")) _interactor.BeginInteract();
            if (GUILayout.Button("Cancel Hold")) _interactor.CancelInteract();
            if (_lockedDoor != null && GUILayout.Button(_lockedDoor.IsLocked ? "Unlock Gate" : "Lock Gate"))
                _lockedDoor.SetLocked(!_lockedDoor.IsLocked);
            GUILayout.EndHorizontal();

            GUILayout.Label("Log:");
            for (int i = 0; i < _log.Count; i++)
                GUILayout.Label("- " + _log[i]);
            GUILayout.EndArea();
        }
    }
}

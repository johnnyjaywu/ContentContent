using System.Collections.Generic;
using ContentContent.Save.Unity;
using UnityEngine;

namespace ContentContent.Save.Samples
{
    /// <summary>
    /// Interactive playground demonstrating <see cref="SaveManagerBehaviour"/> and <see cref="SaveableTransform"/>.
    /// </summary>
    [RequireComponent(typeof(SaveManagerBehaviour))]
    public sealed class SavePlaygroundSample : MonoBehaviour
    {
        private SaveManagerBehaviour _saveManager;
        private Transform _cube;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            _saveManager = GetComponent<SaveManagerBehaviour>();
            _saveManager.OnSlotSaved.AddListener(s => AppendLog($"Saved '{s}'"));
            _saveManager.OnSlotLoaded.AddListener(s => AppendLog($"Loaded '{s}'"));
            _saveManager.OnSlotDeleted.AddListener(s => AppendLog($"Deleted '{s}'"));

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SaveableDemoCube";
            cube.transform.position = new Vector3(0f, 1f, 0f);
            var saveable = cube.AddComponent<SaveableTransform>();
            var soField = typeof(SaveableBehaviour).GetField(
                "_saveId",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            soField?.SetValue(saveable, "demo_cube_01");
            saveable.RegisterWithManager();
            _cube = cube.transform;

            AppendLog("Press Move Cube, Save Slot, then move away and Load Slot.");
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 7)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 400, 320), "Save System Playground", GUI.skin.window);
            if (_cube != null)
            {
                GUILayout.Label($"Cube Pos: {_cube.position:F2}  Active: {_cube.gameObject.activeSelf}");
                if (GUILayout.Button("Move & Rotate Cube"))
                {
                    _cube.position = new Vector3(Random.Range(-3f, 3f), Random.Range(0f, 2f), 0f);
                    _cube.Rotate(0f, 45f, 0f);
                }
                if (GUILayout.Button(_cube.gameObject.activeSelf ? "Deactivate Cube" : "Activate Cube"))
                {
                    _cube.gameObject.SetActive(!_cube.gameObject.activeSelf);
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Default")) _saveManager.SaveDefaultSlot();
            if (GUILayout.Button("Load Default")) _saveManager.LoadDefaultSlot();
            if (GUILayout.Button("Save Autosave")) _saveManager.SaveAutosave();
            GUILayout.EndHorizontal();

            GUILayout.Label("Log:");
            for (int i = 0; i < _log.Count; i++)
                GUILayout.Label("- " + _log[i]);
            GUILayout.EndArea();
        }
    }
}

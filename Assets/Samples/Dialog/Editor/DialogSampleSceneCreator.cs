using ContentContent.Dialog.Samples.UGUI;
using ContentContent.Dialog.Samples.UIToolkit;
using ContentContent.Dialog.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Samples.Editor
{
    public static class DialogSampleSceneCreator
    {
        private const string UguiScenePath = "Assets/Samples/Dialog/UGUI/UGUIDialogSample.unity";
        private const string UitkScenePath = "Assets/Samples/Dialog/UIToolkit/UIToolkitDialogSample.unity";
        private const string UitkUxmlPath = "Assets/Samples/Dialog/UIToolkit/DialogView.uxml";
        private const string UitkPanelSettingsPath = "Assets/Samples/Dialog/UIToolkit/DialogPanelSettings.asset";

        [MenuItem("ContentContent/Dialog/Create uGUI Sample Scene")]
        public static void CreateUguiSampleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var managerGo = new GameObject("DialogSampleManager");
            var runner = managerGo.AddComponent<DialogRunnerBehaviour>();
            var view = UGUIDialogSampleBootstrap.BuildRuntimeUI(runner);
            var bootstrap = managerGo.AddComponent<UGUIDialogSampleBootstrap>();

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_dialogView").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, UguiScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[DialogSampleSceneCreator] Created uGUI sample scene at {UguiScenePath}");
        }

        [MenuItem("ContentContent/Dialog/Create UI Toolkit Sample Scene")]
        public static void CreateUitkSampleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UitkPanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                panelSettings.match = 0.5f;
                AssetDatabase.CreateAsset(panelSettings, UitkPanelSettingsPath);
                AssetDatabase.SaveAssets();
            }

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UitkUxmlPath);

            var managerGo = new GameObject("DialogSampleManager");
            var runner = managerGo.AddComponent<DialogRunnerBehaviour>();
            var uiDoc = managerGo.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var view = managerGo.AddComponent<UIToolkitDialogView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_runner").objectReferenceValue = runner;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            managerGo.AddComponent<UIToolkitDialogSampleBootstrap>();

            EditorSceneManager.SaveScene(scene, UitkScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[DialogSampleSceneCreator] Created UI Toolkit sample scene at {UitkScenePath}");
        }
    }
}

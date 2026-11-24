using ContentContent.Save.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Save.Editor
{
    internal static class SaveMenuItems
    {
        [MenuItem("GameObject/ContentContent/Save/Save Manager", false, 10)]
        private static void CreateSaveManager(MenuCommand command)
        {
            var go = new GameObject("SaveManager");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<SaveManagerBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create Save Manager");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/Save/Saveable Transform", false, 11)]
        private static void CreateSaveableTransform(MenuCommand command)
        {
            var go = new GameObject("SaveableObject");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var saveable = go.AddComponent<SaveableTransform>();
            saveable.GenerateUniqueId();
            Undo.RegisterCreatedObjectUndo(go, "Create Saveable Transform");
            Selection.activeGameObject = go;
        }
    }
}

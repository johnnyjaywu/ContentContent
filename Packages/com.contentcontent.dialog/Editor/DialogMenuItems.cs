using ContentContent.Dialog.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Dialog.Editor
{
    internal static class DialogMenuItems
    {
        [MenuItem("GameObject/ContentContent/Dialog/Dialog Runner", false, 10)]
        private static void CreateDialogRunner(MenuCommand command)
        {
            var go = new GameObject("DialogRunner");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<DialogRunnerBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create Dialog Runner");
            Selection.activeGameObject = go;
        }
    }
}

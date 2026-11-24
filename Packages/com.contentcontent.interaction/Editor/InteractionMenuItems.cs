using ContentContent.Interaction.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Interaction.Editor
{
    internal static class InteractionMenuItems
    {
        [MenuItem("GameObject/ContentContent/Interaction/Interactor", false, 10)]
        private static void CreateInteractor(MenuCommand command)
        {
            var go = new GameObject("Interactor");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<InteractorBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create Interactor");
            Selection.activeGameObject = go;
        }

#if CONTENTCONTENT_PHYSICS2D
        [MenuItem("GameObject/ContentContent/Interaction/Interactable (2D)", false, 20)]
        private static void CreateInteractable2D(MenuCommand command)
        {
            var go = new GameObject("Interactable2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<InteractableBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create Interactable (2D)");
            Selection.activeGameObject = go;
        }
#endif

#if CONTENTCONTENT_PHYSICS
        [MenuItem("GameObject/ContentContent/Interaction/Interactable (3D)", false, 21)]
        private static void CreateInteractable3D(MenuCommand command)
        {
            var go = new GameObject("Interactable3D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            go.AddComponent<InteractableBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create Interactable (3D)");
            Selection.activeGameObject = go;
        }
#endif
    }
}

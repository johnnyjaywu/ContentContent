using ContentContent.Quests.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Quests.Editor
{
    internal static class QuestsMenuItems
    {
        [MenuItem("GameObject/ContentContent/Quests/Quest Log", false, 10)]
        private static void CreateQuestLog(MenuCommand command)
        {
            var go = new GameObject("QuestLog");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<QuestLogComponent>();
            Undo.RegisterCreatedObjectUndo(go, "Create Quest Log");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/Quests/Quest Signal Emitter", false, 11)]
        private static void CreateSignalEmitter(MenuCommand command)
        {
            var go = new GameObject("QuestSignalEmitter");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<QuestSignalEmitter>();
            Undo.RegisterCreatedObjectUndo(go, "Create Quest Signal Emitter");
            Selection.activeGameObject = go;
        }
    }
}

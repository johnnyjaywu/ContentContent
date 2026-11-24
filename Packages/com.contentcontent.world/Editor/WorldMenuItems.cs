using ContentContent.World.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.World.Editor
{
    internal static class WorldMenuItems
    {
        [MenuItem("GameObject/ContentContent/World/World Manager", false, 10)]
        private static void CreateWorldManager(MenuCommand command)
        {
            var go = new GameObject("WorldManager");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<WorldManagerBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Create World Manager");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/World/Room Spawn Point", false, 11)]
        private static void CreateSpawnPoint(MenuCommand command)
        {
            var go = new GameObject("RoomSpawnPoint");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<RoomSpawnPoint>();
            Undo.RegisterCreatedObjectUndo(go, "Create Room Spawn Point");
            Selection.activeGameObject = go;
        }

#if CONTENTCONTENT_PHYSICS2D
        [MenuItem("GameObject/ContentContent/World/Checkpoint 2D", false, 20)]
        private static void CreateCheckpoint2D(MenuCommand command)
        {
            var go = new GameObject("Checkpoint2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<CheckpointBehaviour2D>();
            Undo.RegisterCreatedObjectUndo(go, "Create Checkpoint 2D");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/World/Hazard Zone 2D", false, 21)]
        private static void CreateHazardZone2D(MenuCommand command)
        {
            var go = new GameObject("HazardZone2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<HazardZone2D>();
            Undo.RegisterCreatedObjectUndo(go, "Create Hazard Zone 2D");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/World/Room Transition Trigger 2D", false, 22)]
        private static void CreateTransitionTrigger2D(MenuCommand command)
        {
            var go = new GameObject("RoomTransitionTrigger2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<RoomTransitionTrigger2D>();
            Undo.RegisterCreatedObjectUndo(go, "Create Room Transition Trigger 2D");
            Selection.activeGameObject = go;
        }
#endif
    }
}

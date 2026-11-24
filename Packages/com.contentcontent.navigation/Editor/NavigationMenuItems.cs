using ContentContent.Navigation.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Navigation.Editor
{
    internal static class NavigationMenuItems
    {
        [MenuItem("GameObject/ContentContent/Navigation/Nav Surface (2D XY)", false, 10)]
        private static void CreateSurfaceXY(MenuCommand command)
        {
            CreateSurface(command, "NavSurface_2D_XY", NavMode.PlanarXY, new Vector3(20f, 12f, 1f));
        }

        [MenuItem("GameObject/ContentContent/Navigation/Nav Surface (2D XZ)", false, 11)]
        private static void CreateSurfaceXZ(MenuCommand command)
        {
            CreateSurface(command, "NavSurface_2D_XZ", NavMode.PlanarXZ, new Vector3(20f, 1f, 20f));
        }

        [MenuItem("GameObject/ContentContent/Navigation/Nav Surface (Volumetric 3D)", false, 12)]
        private static void CreateSurface3D(MenuCommand command)
        {
            CreateSurface(command, "NavSurface_3D", NavMode.Volumetric3D, new Vector3(16f, 12f, 16f));
        }

        [MenuItem("GameObject/ContentContent/Navigation/Nav Agent", false, 20)]
        private static void CreateAgent(MenuCommand command)
        {
            var go = new GameObject("NavAgent");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<NavAgent>();
            Undo.RegisterCreatedObjectUndo(go, "Create Nav Agent");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/Navigation/Patrolling Agent", false, 21)]
        private static void CreatePatrollingAgent(MenuCommand command)
        {
            var root = new GameObject("PatrolGroup");
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);

            var agentGo = new GameObject("PatrollingAgent");
            agentGo.transform.SetParent(root.transform, false);
            agentGo.AddComponent<NavAgent>();
            var patrol = agentGo.AddComponent<NavPatrol>();

            var wp0 = new GameObject("Waypoint_0");
            wp0.transform.SetParent(root.transform, false);
            wp0.transform.localPosition = new Vector3(-3f, 0f, 0f);

            var wp1 = new GameObject("Waypoint_1");
            wp1.transform.SetParent(root.transform, false);
            wp1.transform.localPosition = new Vector3(3f, 0f, 0f);

            patrol.Waypoints.Add(wp0.transform);
            patrol.Waypoints.Add(wp1.transform);

            Undo.RegisterCreatedObjectUndo(root, "Create Patrolling Agent");
            Selection.activeGameObject = agentGo;
        }

        private static void CreateSurface(MenuCommand command, string name, NavMode mode, Vector3 size)
        {
            var go = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var surface = go.AddComponent<NavSurface>();

            var so = new SerializedObject(surface);
            so.FindProperty("_mode").intValue = (int)mode;
            so.FindProperty("_size").vector3Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeGameObject = go;
        }
    }
}

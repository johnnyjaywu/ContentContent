using ContentContent.Stats.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Stats.Editor
{
    internal static class StatsMenuItems
    {
        [MenuItem("GameObject/ContentContent/Stats/Combat Entity (Stats + Health)", false, 10)]
        private static void CreateCombatEntity(MenuCommand cmd)
        {
            var go = new GameObject("CombatEntity");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            go.AddComponent<StatsComponent>();
            go.AddComponent<HealthComponent>();
            Undo.RegisterCreatedObjectUndo(go, "Create CombatEntity");
            Selection.activeGameObject = go;
        }
    }
}

using ContentContent.Inventory.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Inventory.Editor
{
    internal static class InventoryMenuItems
    {
        [MenuItem("GameObject/ContentContent/Inventory/Inventory Holder", false, 10)]
        private static void CreateInventoryHolder(MenuCommand command)
        {
            var go = new GameObject("InventoryHolder");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<InventoryComponent>();
            Undo.RegisterCreatedObjectUndo(go, "Create Inventory Holder");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/ContentContent/Inventory/Equipped Character", false, 11)]
        private static void CreateEquippedCharacter(MenuCommand command)
        {
            var go = new GameObject("EquippedCharacter");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<InventoryComponent>();
            go.AddComponent<EquipmentComponent>();
            Undo.RegisterCreatedObjectUndo(go, "Create Equipped Character");
            Selection.activeGameObject = go;
        }
    }
}

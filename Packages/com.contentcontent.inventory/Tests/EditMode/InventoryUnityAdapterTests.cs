#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Inventory.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Inventory.Tests
{
    public class InventoryUnityAdapterTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        [Test]
        public void InventoryAndEquipmentComponents_AutoWireAndSwapWithCatalog()
        {
            var swordAsset = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            _created.Add(swordAsset);
            var swordSo = new SerializedObject(swordAsset);
            swordSo.FindProperty("_itemId").stringValue = "iron_blade";
            swordSo.FindProperty("_displayName").stringValue = "Iron Blade";
            swordSo.FindProperty("_category").stringValue = "Weapon";
            swordSo.FindProperty("_maxStack").intValue = 1;
            swordSo.FindProperty("_unitWeight").floatValue = 4f;
            swordSo.ApplyModifiedPropertiesWithoutUndo();

            var catalog = ScriptableObject.CreateInstance<ItemCatalogAsset>();
            _created.Add(catalog);
            var catSo = new SerializedObject(catalog);
            SerializedProperty itemsProp = catSo.FindProperty("_items");
            itemsProp.InsertArrayElementAtIndex(0);
            itemsProp.GetArrayElementAtIndex(0).objectReferenceValue = swordAsset;
            catSo.ApplyModifiedPropertiesWithoutUndo();

            var hero = new GameObject("HeroInventory");
            _created.Add(hero);
            var invComp = hero.AddComponent<InventoryComponent>();
            var invSo = new SerializedObject(invComp);
            invSo.FindProperty("_catalog").objectReferenceValue = catalog;
            invSo.FindProperty("_slotCount").intValue = 4;
            invSo.FindProperty("_maxWeight").floatValue = 10f;
            invSo.ApplyModifiedPropertiesWithoutUndo();

            var equipComp = hero.AddComponent<EquipmentComponent>();
            string equippedSlot = null;
            string equippedItem = null;
            equipComp.OnEquipped.AddListener((s, item) =>
            {
                equippedSlot = s;
                equippedItem = item;
            });

            invComp.AddItem(swordAsset);
            Assert.IsTrue(invComp.HasItem("iron_blade", 1));
            Assert.AreEqual(4f, invComp.Container.CurrentWeight, 0.001f);

            // Sibling EquipmentComponent auto-wires InventoryComponent and equips from slot 0
            Assert.AreSame(invComp, equipComp.Inventory);
            Assert.IsTrue(equipComp.TryEquipFromInventory("Weapon", 0));
            Assert.AreEqual("Weapon", equippedSlot);
            Assert.AreEqual("iron_blade", equippedItem);
            Assert.IsFalse(invComp.HasItem("iron_blade", 1));

            // Unequip back to inventory
            Assert.IsTrue(equipComp.TryUnequipToInventory("Weapon"));
            Assert.IsTrue(invComp.HasItem("iron_blade", 1));
        }
    }
}
#endif

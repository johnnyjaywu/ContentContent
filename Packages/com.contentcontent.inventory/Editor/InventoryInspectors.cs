using System;
using System.Collections.Generic;
using ContentContent.Inventory.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Inventory.Editor
{
    [CustomEditor(typeof(ItemCatalogAsset))]
    internal sealed class ItemCatalogAssetEditor : UnityEditor.Editor
    {
        private readonly HashSet<string> _seenIds = new HashSet<string>(StringComparer.Ordinal);

        public override void OnInspectorGUI()
        {
            var catalog = (ItemCatalogAsset)target;
            _seenIds.Clear();

            IReadOnlyList<ItemDefinitionAsset> items = catalog.Items;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ItemDefinitionAsset item = items[i];
                    if (item == null)
                    {
                        EditorGUILayout.HelpBox($"Entry {i} is null and will be skipped.", MessageType.Warning);
                    }
                    else if (!_seenIds.Add(item.ItemId))
                    {
                        EditorGUILayout.HelpBox($"Duplicate Item Id '{item.ItemId}' in catalog; later entries overwrite earlier ones.", MessageType.Warning);
                    }
                }
            }

            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(InventoryComponent))]
    internal sealed class InventoryComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var invComp = (InventoryComponent)target;

            if (invComp.Catalog == null)
            {
                EditorGUILayout.HelpBox("No Item Catalog assigned. Unregistered items default to MaxStack=99 and UnitWeight=0.", MessageType.Info);
            }

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Live Inventory", EditorStyles.boldLabel);

            InventoryContainer c = invComp.Container;
            EditorGUILayout.LabelField("Slots Used", $"{c.UsedSlotCount} / {c.SlotCount}");

            if (c.MaxWeight > 0f)
            {
                Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                float ratio = Mathf.Clamp01(c.CurrentWeight / c.MaxWeight);
                EditorGUI.ProgressBar(rect, ratio, $"Weight: {c.CurrentWeight:0.#} / {c.MaxWeight:0.#}");
            }

            for (int i = 0; i < c.SlotCount; i++)
            {
                ItemStack slot = c.Slots[i];
                if (!slot.IsEmpty)
                {
                    EditorGUILayout.LabelField($"Slot {i}", $"{slot.ItemId} x{slot.Quantity}");
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort"))
                c.SortById();
            if (GUILayout.Button("Clear"))
                c.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(EquipmentComponent))]
    internal sealed class EquipmentComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _invProp;

        private void OnEnable()
        {
            _invProp = serializedObject.FindProperty("_inventory");
        }

        public override void OnInspectorGUI()
        {
            var equipComp = (EquipmentComponent)target;

            serializedObject.Update();
            if (_invProp != null && _invProp.objectReferenceValue == null)
            {
                InventoryComponent sibling = equipComp.GetComponent<InventoryComponent>();
                if (sibling == null)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox("No InventoryComponent assigned or attached on this GameObject.", MessageType.Warning);
                    if (GUILayout.Button("Add Inventory", GUILayout.Width(120f), GUILayout.Height(34f)))
                    {
                        InventoryComponent added = Undo.AddComponent<InventoryComponent>(equipComp.gameObject);
                        _invProp.objectReferenceValue = added;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox($"Sibling InventoryComponent '{sibling.name}' will auto-bind on Awake.", MessageType.Info);
                }
            }
            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Equipped Slots", EditorStyles.boldLabel);

            EquipmentSet set = equipComp.Equipment;
            for (int i = 0; i < set.SlotNames.Count; i++)
            {
                string slotName = set.SlotNames[i];
                ItemStack equipped = set.GetEquipped(slotName);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(slotName, equipped.IsEmpty ? "(Empty)" : equipped.ItemId);
                if (!equipped.IsEmpty && GUILayout.Button("Unequip", GUILayout.Width(72f)))
                {
                    equipComp.TryUnequipToInventory(slotName);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            Repaint();
        }
    }
}

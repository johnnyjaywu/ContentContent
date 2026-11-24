using System.Collections.Generic;
using ContentContent.Inventory.Unity;
using UnityEngine;

namespace ContentContent.Inventory.Samples
{
    /// <summary>
    /// Interactive playground demonstrating <see cref="InventoryComponent"/> and <see cref="EquipmentComponent"/>.
    /// </summary>
    [RequireComponent(typeof(InventoryComponent))]
    [RequireComponent(typeof(EquipmentComponent))]
    public sealed class InventoryPlaygroundSample : MonoBehaviour
    {
        private InventoryComponent _inv;
        private EquipmentComponent _equip;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            _inv = GetComponent<InventoryComponent>();
            _equip = GetComponent<EquipmentComponent>();

            _inv.Container.RegisterDefinition(new ItemDefinition("potion", "Health Potion", "Consumable", maxStack: 5, unitWeight: 1f));
            _inv.Container.RegisterDefinition(new ItemDefinition("sword", "Iron Sword", "Weapon", maxStack: 1, unitWeight: 6f));
            _inv.Container.RegisterDefinition(new ItemDefinition("plate", "Steel Armor", "Armor", maxStack: 1, unitWeight: 10f));

            _inv.OnItemAdded.AddListener((id, q) => AppendLog($"+{q} {id}"));
            _inv.OnItemRemoved.AddListener((id, q) => AppendLog($"-{q} {id}"));
            _equip.OnEquipped.AddListener((s, id) => AppendLog($"Equipped {id} -> {s}"));
            _equip.OnUnequipped.AddListener((s, id) => AppendLog($"Unequipped {id} <- {s}"));

            _inv.AddItemById("potion", 3);
            _inv.AddItemById("sword", 1);
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 6)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 430, 360), "Inventory & Equipment Playground", GUI.skin.window);
            GUILayout.Label($"Weight: {_inv.Container.CurrentWeight:0.#} / {(_inv.Container.MaxWeight > 0f ? _inv.Container.MaxWeight.ToString("0.#") : "Unlimited")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+2 Potion")) _inv.AddItemById("potion", 2);
            if (GUILayout.Button("+1 Sword")) _inv.AddItemById("sword", 1);
            if (GUILayout.Button("+1 Armor")) _inv.AddItemById("plate", 1);
            if (GUILayout.Button("Sort")) _inv.Container.SortById();
            GUILayout.EndHorizontal();

            GUILayout.Label("Equipped:");
            for (int i = 0; i < _equip.Equipment.SlotNames.Count; i++)
            {
                string slotName = _equip.Equipment.SlotNames[i];
                ItemStack eq = _equip.Equipment.GetEquipped(slotName);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{slotName}: {(eq.IsEmpty ? "(Empty)" : eq.ItemId)}");
                if (!eq.IsEmpty && GUILayout.Button("Unequip", GUILayout.Width(72f)))
                    _equip.TryUnequipToInventory(slotName);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Slots:");
            for (int i = 0; i < _inv.Container.SlotCount; i++)
            {
                ItemStack s = _inv.Container.Slots[i];
                if (s.IsEmpty) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}] {s.ItemId} x{s.Quantity}");
                if (GUILayout.Button("Equip Weapon", GUILayout.Width(100f))) _equip.TryEquipFromInventory("Weapon", i);
                if (GUILayout.Button("Equip Armor", GUILayout.Width(96f))) _equip.TryEquipFromInventory("Armor", i);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Log:");
            for (int i = 0; i < _log.Count; i++)
                GUILayout.Label("- " + _log[i]);
            GUILayout.EndArea();
        }
    }
}

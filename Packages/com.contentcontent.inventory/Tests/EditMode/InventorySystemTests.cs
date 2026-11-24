using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Inventory.Tests
{
    public class InventorySystemTests
    {
        private static InventoryContainer CreateTestInventory(int slots = 4, float maxWeight = 0f)
        {
            var inv = new InventoryContainer(new InventoryConfig
            {
                SlotCount = slots,
                MaxWeight = maxWeight,
                AllowAutoStack = true
            });
            inv.RegisterDefinition(new ItemDefinition("potion", "Health Potion", "Consumable", maxStack: 5, unitWeight: 1f));
            inv.RegisterDefinition(new ItemDefinition("sword", "Iron Sword", "Weapon", maxStack: 1, unitWeight: 8f, tags: new[] { "OneHanded" }));
            inv.RegisterDefinition(new ItemDefinition("greatsword", "Greatsword", "Weapon", maxStack: 1, unitWeight: 15f, tags: new[] { "TwoHanded" }));
            inv.RegisterDefinition(new ItemDefinition("shield", "Oak Shield", "Armor", maxStack: 1, unitWeight: 6f));
            return inv;
        }

        [Test]
        public void InventoryContainer_StacksAcrossSlotsAndEnforcesWeightAndAtomicity()
        {
            var inv = CreateTestInventory(slots: 3, maxWeight: 12f);

            // Stack cap is 5; weight 12 allows at most 12 potions, 3 slots allow at most 15
            Assert.IsTrue(inv.TryAdd("potion", 7, out int added, out int remainder));
            Assert.AreEqual(7, added);
            Assert.AreEqual(0, remainder);
            Assert.AreEqual(5, inv.GetSlot(0).Quantity);
            Assert.AreEqual(2, inv.GetSlot(1).Quantity);

            // Atomic TryAddAll rejects if weight or slot capacity would overflow (only 5kg left, sword is 8kg)
            Assert.IsFalse(inv.TryAddAll("sword", 1));
            Assert.AreEqual(7, inv.CountOf("potion"));

            // Best-effort TryAdd fills remaining 5kg weight limit
            Assert.IsTrue(inv.TryAdd("potion", 10, out int added2, out int rem2));
            Assert.AreEqual(5, added2);
            Assert.AreEqual(5, rem2);
            Assert.AreEqual(12, inv.CountOf("potion"));
            Assert.AreEqual(12f, inv.CurrentWeight, 0.001f);

            // Atomic TryRemove leaves inventory untouched when count is insufficient
            Assert.IsFalse(inv.TryRemove("potion", 20));
            Assert.AreEqual(12, inv.CountOf("potion"));
            Assert.IsTrue(inv.TryRemove("potion", 9));
            Assert.AreEqual(3, inv.CountOf("potion"));
        }

        [Test]
        public void InventoryContainer_MoveMergeSplitTransferSortAndSnapshot()
        {
            var source = CreateTestInventory(slots: 4);
            var chest = CreateTestInventory(slots: 2);

            source.TryAdd("potion", 5, out _, out _);
            Assert.IsTrue(source.TrySplitStack(0, 2, 2));
            Assert.AreEqual(3, source.GetSlot(0).Quantity);
            Assert.AreEqual(2, source.GetSlot(2).Quantity);

            // Merge back cleanly
            Assert.IsTrue(source.TryMoveOrMerge(2, 0));
            Assert.AreEqual(5, source.GetSlot(0).Quantity);
            Assert.IsTrue(source.GetSlot(2).IsEmpty);

            // Transfer between containers
            Assert.IsTrue(source.TryTransferTo(chest, 0, 3));
            Assert.AreEqual(2, source.CountOf("potion"));
            Assert.AreEqual(3, chest.CountOf("potion"));

            // Snapshot export/import restores exact slot layout
            source.TryAdd("sword", 1, out _, out _);
            source.SortById();
            List<ItemStack> snap = source.ExportSnapshot();
            source.Clear();
            Assert.AreEqual(0, source.UsedSlotCount);
            source.ImportSnapshot(snap);
            Assert.AreEqual(2, source.CountOf("potion"));
            Assert.AreEqual(1, source.CountOf("sword"));
        }

        [Test]
        public void EquipmentSet_EnforcesCategoryTagRulesAndRollsBackFailedSwap()
        {
            var inv = CreateTestInventory(slots: 2, maxWeight: 16f);
            inv.TryAddAll("sword", 1);      // Slot 0: 8kg
            inv.TryAddAll("greatsword", 1); // Wait: 8 + 15 = 23 > 16 -> let's test atomic rollback via 1-slot inventory!

            var tightInv = CreateTestInventory(slots: 1, maxWeight: 20f);
            tightInv.TryAddAll("sword", 1); // 8kg
            tightInv.TryGetDefinition("sword", out ItemDefinition swordDef);
            tightInv.TryGetDefinition("greatsword", out ItemDefinition greatDef);
            tightInv.TryGetDefinition("shield", out ItemDefinition shieldDef);

            var equip = new EquipmentSet();
            equip.DefineSlot("MainHand", requiredCategory: "Weapon", requiredTag: "OneHanded");
            equip.DefineSlot("OffHand", requiredCategory: "Armor");

            // Category / tag filtering
            Assert.IsFalse(equip.CanEquip("MainHand", "greatsword", greatDef));
            Assert.IsFalse(equip.CanEquip("MainHand", "shield", shieldDef));
            Assert.IsTrue(equip.CanEquip("MainHand", "sword", swordDef));

            Assert.IsTrue(equip.TryEquipFromInventory("MainHand", tightInv, 0));
            Assert.AreEqual("sword", equip.GetEquipped("MainHand").ItemId);
            Assert.AreEqual(1, tightInv.FreeSlotCount);

            // Fill tightInv with another sword; unequip fails cleanly when inventory is full + unstackable
            tightInv.TryAddAll("sword", 1);
            Assert.IsFalse(equip.TryUnequipToInventory("MainHand", tightInv));

            // Make tightInv maxWeight 10kg and try swapping heavy item if we also add a heavy sword
            var weightInv = CreateTestInventory(slots: 1, maxWeight: 10f);
            var equipAny = new EquipmentSet();
            equipAny.DefineSlot("Weapon", requiredCategory: "Weapon");
            equipAny.TryEquip("Weapon", "greatsword", out _, greatDef); // 15kg equipped
            weightInv.TryAddAll("sword", 1); // 8kg in 10kg max inventory

            // Swapping greatsword (15kg) back into 10kg bag must fail and roll back cleanly!
            Assert.IsFalse(equipAny.TryEquipFromInventory("Weapon", weightInv, 0));
            Assert.AreEqual("greatsword", equipAny.GetEquipped("Weapon").ItemId);
            Assert.AreEqual("sword", weightInv.GetSlot(0).ItemId);
        }
    }
}

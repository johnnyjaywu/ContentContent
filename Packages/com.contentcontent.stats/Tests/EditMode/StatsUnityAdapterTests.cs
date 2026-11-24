#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Stats.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Stats.Tests
{
    public class StatsUnityAdapterTests
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
        public void StatSheetAsset_StarterRpgPresetPopulatesExpectedStats()
        {
            var sheet = ScriptableObject.CreateInstance<StatSheetAsset>();
            _created.Add(sheet);

            sheet.ApplyStarterRpgPreset();
            var collection = new StatCollection<string>();
            sheet.Populate(collection);

            Assert.AreEqual(100f, collection.GetValue("MaxHealth"), 0.001f);
            Assert.AreEqual(20f, collection.GetValue("Attack"), 0.001f);
            Assert.AreEqual(5f, collection.GetValue("Defense"), 0.001f);
            Assert.AreEqual(6f, collection.GetValue("MoveSpeed"), 0.001f);
        }

        [Test]
        public void HealthComponent_AutoBindsToSiblingStatsComponent_AndScalesWithBuffAsset()
        {
            var sheet = ScriptableObject.CreateInstance<StatSheetAsset>();
            _created.Add(sheet);
            sheet.ApplyStarterRpgPreset();

            var buff = ScriptableObject.CreateInstance<BuffAsset>();
            _created.Add(buff);
            var buffSo = new SerializedObject(buff);
            buffSo.FindProperty("_buffId").stringValue = "Vitality";
            buffSo.FindProperty("_duration").floatValue = 10f;
            SerializedProperty mods = buffSo.FindProperty("_modifiers");
            mods.InsertArrayElementAtIndex(0);
            SerializedProperty entry = mods.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("statId").stringValue = "MaxHealth";
            entry.FindPropertyRelative("value").floatValue = 50f;
            entry.FindPropertyRelative("type").intValue = (int)ModifierType.Flat;
            buffSo.ApplyModifiedPropertiesWithoutUndo();

            var go = new GameObject("Combatant");
            _created.Add(go);

            var statsComp = go.AddComponent<StatsComponent>();
            statsComp.InitialSheet = sheet;

            var healthComp = go.AddComponent<HealthComponent>();

            Assert.IsTrue(healthComp.IsBoundToStat);
            Assert.AreEqual(100f, healthComp.Max, 0.001f);
            Assert.AreEqual(100f, healthComp.Current, 0.001f);

            statsComp.ApplyBuff(buff);

            Assert.AreEqual(150f, healthComp.Max, 0.001f);
            Assert.AreEqual(150f, healthComp.Current, 0.001f);
        }

        [Test]
        public void BuffAsset_DefaultInvalidModifierType_SanitizesToFlat()
        {
            var buff = ScriptableObject.CreateInstance<BuffAsset>();
            _created.Add(buff);

            var buffSo = new SerializedObject(buff);
            buffSo.FindProperty("_buffId").stringValue = "ZeroEnumFallback";
            SerializedProperty mods = buffSo.FindProperty("_modifiers");
            mods.InsertArrayElementAtIndex(0);
            SerializedProperty entry = mods.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("statId").stringValue = "Attack";
            entry.FindPropertyRelative("value").floatValue = 15f;
            entry.FindPropertyRelative("type").intValue = 0;
            buffSo.ApplyModifiedPropertiesWithoutUndo();

            BuffDefinition<string> def = buff.BuildDefinition();
            Assert.AreEqual(1, def.Modifiers.Count);
            Assert.AreEqual(ModifierType.Flat, def.Modifiers[0].Type);
            Assert.AreEqual(15f, def.Modifiers[0].Value, 0.001f);
        }
    }
}
#endif

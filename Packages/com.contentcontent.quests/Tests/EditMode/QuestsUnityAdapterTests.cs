#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Quests.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Quests.Tests
{
    public class QuestsUnityAdapterTests
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
        public void QuestLogComponent_AndSignalEmitter_CompleteQuestAndFireEvents()
        {
            var questAsset = ScriptableObject.CreateInstance<QuestDefinitionAsset>();
            _created.Add(questAsset);
            var qSo = new SerializedObject(questAsset);
            qSo.FindProperty("_questId").stringValue = "find_relic";
            qSo.FindProperty("_title").stringValue = "Find the Relic";
            SerializedProperty objs = qSo.FindProperty("_objectives");
            objs.InsertArrayElementAtIndex(0);
            SerializedProperty obj0 = objs.GetArrayElementAtIndex(0);
            obj0.FindPropertyRelative("objectiveId").stringValue = "relic";
            obj0.FindPropertyRelative("description").stringValue = "Collect Relic Shards";
            obj0.FindPropertyRelative("targetCount").intValue = 2;
            obj0.FindPropertyRelative("signalKey").stringValue = "shard_picked";
            qSo.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(0, questAsset.ValidateAsset().Count);

            var catalog = ScriptableObject.CreateInstance<QuestCatalogAsset>();
            _created.Add(catalog);
            var catSo = new SerializedObject(catalog);
            SerializedProperty list = catSo.FindProperty("_quests");
            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = questAsset;
            catSo.ApplyModifiedPropertiesWithoutUndo();

            var logGo = new GameObject("QuestLog");
            _created.Add(logGo);
            var logComp = logGo.AddComponent<QuestLogComponent>();
            var logSo = new SerializedObject(logComp);
            logSo.FindProperty("_catalog").objectReferenceValue = catalog;
            logSo.ApplyModifiedPropertiesWithoutUndo();

            string completedQuest = null;
            int progressCalls = 0;
            logComp.OnObjectiveProgressed.AddListener((_, _) => progressCalls++);
            logComp.OnQuestCompleted.AddListener(qId => completedQuest = qId);

            Assert.IsTrue(logComp.StartQuestById("find_relic"));

            var emitterGo = new GameObject("ShardEmitter");
            _created.Add(emitterGo);
            var emitter = emitterGo.AddComponent<QuestSignalEmitter>();
            var emSo = new SerializedObject(emitter);
            emSo.FindProperty("_questLog").objectReferenceValue = logComp;
            emSo.FindProperty("_signalKey").stringValue = "shard_picked";
            emSo.FindProperty("_amount").intValue = 1;
            emSo.ApplyModifiedPropertiesWithoutUndo();

            emitter.Emit();
            Assert.AreEqual(1, progressCalls);
            Assert.IsNull(completedQuest);

            emitter.Emit();
            Assert.AreEqual(2, progressCalls);
            Assert.AreEqual("find_relic", completedQuest);
            Assert.AreEqual(QuestStatus.Completed, logComp.Journal.GetStatus("find_relic"));
        }
    }
}
#endif

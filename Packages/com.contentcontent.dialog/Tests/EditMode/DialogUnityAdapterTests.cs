#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Dialog.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Dialog.Tests
{
    public class DialogUnityAdapterTests
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
        public void DialogRunnerBehaviour_StartInitialDialog_WithTextAsset_FiresLineShownAndCommandBinding()
        {
            var scriptAsset = new TextAsset(
                "<<declare $gold = 100>>\n" +
                "title: start\n" +
                "---\n" +
                "<<open_gate north_gate 2>>\n" +
                "Guard: You carry {$gold} gold.\n" +
                "===");
            _created.Add(scriptAsset);

            var go = new GameObject("DialogRunnerGO");
            _created.Add(go);
            var runnerBehaviour = go.AddComponent<DialogRunnerBehaviour>();

            var so = new SerializedObject(runnerBehaviour);
            so.FindProperty("_initialScript").objectReferenceValue = scriptAsset;
            SerializedProperty bindings = so.FindProperty("_commandBindings");
            bindings.InsertArrayElementAtIndex(0);
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("commandName").stringValue = "OPEN_GATE";
            so.ApplyModifiedPropertiesWithoutUndo();

            string receivedArgs = null;
            runnerBehaviour.CommandBindings[0].onInvoked.AddListener(args => receivedArgs = args);

            string shownSpeaker = null;
            string shownLine = null;
            runnerBehaviour.OnLineShown.AddListener((speaker, line) =>
            {
                shownSpeaker = speaker;
                shownLine = line;
            });

            runnerBehaviour.StartInitialDialog();

            Assert.IsTrue(runnerBehaviour.IsRunning);
            Assert.AreEqual("north_gate 2", receivedArgs);
            Assert.AreEqual("Guard", shownSpeaker);
            Assert.AreEqual("You carry 100 gold.", shownLine);
        }

        [Test]
        public void DialogGraphAsset_ValidateGraph_ReportsValidAndDuplicateIdsSafely()
        {
            var graphAsset = ScriptableObject.CreateInstance<DialogGraphAsset>();
            _created.Add(graphAsset);

            var so = new SerializedObject(graphAsset);
            so.FindProperty("_startNodeId").stringValue = "intro";
            SerializedProperty nodes = so.FindProperty("_nodes");
            nodes.InsertArrayElementAtIndex(0);
            SerializedProperty n0 = nodes.GetArrayElementAtIndex(0);
            n0.FindPropertyRelative("id").stringValue = "intro";
            n0.FindPropertyRelative("lineKey").stringValue = "Hello";
            n0.FindPropertyRelative("isEnd").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(0, graphAsset.ValidateGraph().Count);

            // Introducing a duplicate node id should return a validation error instead of throwing.
            nodes.InsertArrayElementAtIndex(1);
            SerializedProperty n1 = nodes.GetArrayElementAtIndex(1);
            n1.FindPropertyRelative("id").stringValue = "intro";
            so.ApplyModifiedPropertiesWithoutUndo();

            IReadOnlyList<DialogValidationIssue> issues = graphAsset.ValidateGraph();
            Assert.Greater(issues.Count, 0);
        }
    }
}
#endif

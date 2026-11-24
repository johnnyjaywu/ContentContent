#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Fsm;
using ContentContent.Core.Fsm.Unity;
using ContentContent.Core.Grid;
using ContentContent.Core.Grid.Unity;
using ContentContent.Core.Timers.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Core.Tests
{
    public class CoreUnityAdapterTests
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
        public void BlackboardBehaviour_SeedsInitialEntries_AndPreventsSelfParenting()
        {
            var go = new GameObject("TestBlackboard");
            _created.Add(go);

            var bb = go.AddComponent<BlackboardBehaviour>();
            bb.ParentBlackboard = bb;
            Assert.IsNull(bb.ParentBlackboard);

            var so = new SerializedObject(bb);
            SerializedProperty list = so.FindProperty("_initialEntries");
            list.InsertArrayElementAtIndex(0);
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("key").stringValue = "Score";
            entry.FindPropertyRelative("type").enumValueIndex = (int)BlackboardBehaviour.InitialValueType.Int;
            entry.FindPropertyRelative("intValue").intValue = 42;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(42, bb.Blackboard.Get(BlackboardKey.Create<int>("Score")));
        }

        [Test]
        public void StateMachineRunner_EmitsUnityEventOnTransition()
        {
            var go = new GameObject("TestRunner");
            _created.Add(go);

            var runner = go.AddComponent<StateMachineRunner>();
            string observedState = null;
            runner.OnStateChanged.AddListener(name => observedState = name);

            var idle = new ActionState("Idle");
            var run = new ActionState("Run");
            var fsm = new StateMachine(idle);
            fsm.AddTransition(idle, run, () => true);

            runner.Initialize(fsm, enterImmediately: true);
            fsm.OnTick(0.016f);

            Assert.AreEqual("Run", observedState);
            Assert.AreEqual("Run", runner.CurrentStateName);
        }

        [Test]
        public void TimerSchedulerRunner_AppliesTimeScale()
        {
            var go = new GameObject("TestTimerRunner");
            _created.Add(go);

            var runner = go.AddComponent<TimerSchedulerRunner>();
            runner.TimeScale = 0.5f;

            Assert.AreEqual(0.5f, runner.Scheduler.TimeScale, 0.001f);
        }

        [Test]
        public void GridBehaviour_CreatesSizedGridAndMapsWorld()
        {
            var go = new GameObject("TestGrid");
            _created.Add(go);

            var gridBehaviour = go.AddComponent<GridBehaviour>();
            Grid2D<int> grid = gridBehaviour.CreateGrid(_ => 7);

            Assert.AreEqual(gridBehaviour.Width, grid.Width);
            Assert.AreEqual(gridBehaviour.Height, grid.Height);
            Assert.AreEqual(7, grid[0, 0]);
        }
    }
}
#endif

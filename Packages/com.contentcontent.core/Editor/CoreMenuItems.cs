using ContentContent.Core.Blackboards.Unity;
using ContentContent.Core.Events.Unity;
using ContentContent.Core.Fsm.Unity;
using ContentContent.Core.Grid.Unity;
using ContentContent.Core.Timers.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Core.Editor
{
    internal static class CoreMenuItems
    {
        [MenuItem("GameObject/ContentContent/Core/Blackboard", false, 10)]
        private static void CreateBlackboard(MenuCommand cmd) => CreateComponent<BlackboardBehaviour>("Blackboard", cmd);

        [MenuItem("GameObject/ContentContent/Core/Event Bus", false, 11)]
        private static void CreateEventBus(MenuCommand cmd) => CreateComponent<EventBusBehaviour>("EventBus", cmd);

        [MenuItem("GameObject/ContentContent/Core/State Machine Runner", false, 12)]
        private static void CreateStateMachineRunner(MenuCommand cmd) => CreateComponent<StateMachineRunner>("StateMachineRunner", cmd);

        [MenuItem("GameObject/ContentContent/Core/Timer Scheduler Runner", false, 13)]
        private static void CreateTimerSchedulerRunner(MenuCommand cmd) => CreateComponent<TimerSchedulerRunner>("TimerScheduler", cmd);

        [MenuItem("GameObject/ContentContent/Core/Grid", false, 14)]
        private static void CreateGrid(MenuCommand cmd) => CreateComponent<GridBehaviour>("Grid", cmd);

        private static void CreateComponent<T>(string name, MenuCommand cmd) where T : Component
        {
            var go = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            go.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeGameObject = go;
        }
    }
}

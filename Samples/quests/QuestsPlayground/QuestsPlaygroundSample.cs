using System.Collections.Generic;
using ContentContent.Quests.Unity;
using UnityEngine;

namespace ContentContent.Quests.Samples
{
    /// <summary>
    /// Interactive playground demonstrating <see cref="QuestLogComponent"/> sequential/parallel
    /// quests and signal-driven completion.
    /// </summary>
    [RequireComponent(typeof(QuestLogComponent))]
    public sealed class QuestsPlaygroundSample : MonoBehaviour
    {
        private QuestLogComponent _questLog;
        private readonly List<string> _log = new List<string>();
        private readonly List<QuestState> _activeScratch = new List<QuestState>();

        private void Start()
        {
            _questLog = GetComponent<QuestLogComponent>();
            QuestJournal j = _questLog.Journal;

            j.RegisterQuest(new QuestDefinition(
                id: "beacon",
                title: "Light the Signal Beacon",
                objectiveMode: ObjectiveMode.Sequential,
                objectives: new[]
                {
                    new ObjectiveDefinition("talk_warden", "Speak with Warden", 1, signalKey: "talk_warden"),
                    new ObjectiveDefinition("gather_oil", "Gather Beacon Fuel", 2, signalKey: "collect_fuel")
                }));

            j.RegisterQuest(new QuestDefinition(
                id: "warden_thanks",
                title: "A Reward for Bravery",
                prerequisiteQuestIds: new[] { "beacon" },
                objectives: new[]
                {
                    new ObjectiveDefinition("claim_reward", "Claim Reward", 1, signalKey: "claim_reward")
                },
                repeatable: true));

            _questLog.OnQuestStarted.AddListener(id => AppendLog($"Started: {id}"));
            _questLog.OnObjectiveProgressed.AddListener((q, o) => AppendLog($"Progress: {q}.{o}"));
            _questLog.OnQuestCompleted.AddListener(id => AppendLog($"Completed: {id}"));
            _questLog.OnQuestFailed.AddListener(id => AppendLog($"Failed: {id}"));

            _questLog.StartQuestById("beacon");
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 6)
                _log.RemoveAt(0);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 420, 350), "Quests Journal Playground", GUI.skin.window);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Signal: Talk Warden")) _questLog.ReportSignal("talk_warden");
            if (GUILayout.Button("Signal: Collect Fuel")) _questLog.ReportSignal("collect_fuel");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _questLog.Journal.CanStart("warden_thanks");
            if (GUILayout.Button("Start Follow-up Quest")) _questLog.StartQuestById("warden_thanks");
            GUI.enabled = true;
            if (GUILayout.Button("Signal: Claim Reward")) _questLog.ReportSignal("claim_reward");
            GUILayout.EndHorizontal();

            _questLog.Journal.GetActiveQuests(_activeScratch);
            GUILayout.Label($"Active Quests ({_activeScratch.Count}):");
            for (int i = 0; i < _activeScratch.Count; i++)
            {
                QuestState q = _activeScratch[i];
                GUILayout.Label($"* {q.Definition.Title} ({q.Definition.ObjectiveMode})");
                for (int o = 0; o < q.Objectives.Count; o++)
                {
                    ObjectiveProgress op = q.Objectives[o];
                    string stateTag = q.IsObjectiveUnlocked(o) ? (op.IsComplete ? "[Done]" : "[Open]") : "[Locked]";
                    GUILayout.Label($"   {stateTag} {op.Definition.Description}: {op.CurrentCount}/{op.Definition.TargetCount}");
                }
            }

            GUILayout.Label("Log:");
            for (int i = 0; i < _log.Count; i++)
                GUILayout.Label("- " + _log[i]);
            GUILayout.EndArea();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Quests.Unity
{
    /// <summary>
    /// Designer-authored <see cref="ScriptableObject"/> defining a quest, its prerequisites,
    /// parallel/sequential objectives, and signal keys.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "ContentContent/Quests/Quest Definition", order = 0)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/quests/quickstart")]
    public class QuestDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct ObjectiveEntry
        {
            [Tooltip("Unique objective identifier within this quest.")]
            public string objectiveId;

            [Tooltip("Player-facing description shown in the quest tracker.")]
            public string description;

            [Tooltip("Target progress count required to complete this objective.")]
            [Min(1)]
            public int targetCount;

            [Tooltip("Optional bonus objective that does not block quest completion.")]
            public bool isOptional;

            [Tooltip("Signal key matched by QuestLogComponent.ReportSignal() / QuestSignalEmitter (defaults to Objective Id).")]
            public string signalKey;
        }

        [Tooltip("Unique quest identifier used by saves and prerequisite links. Defaults to asset name.")]
        [SerializeField] private string _questId = string.Empty;

        [Tooltip("Human-readable quest title shown in UI and trackers.")]
        [SerializeField] private string _title = string.Empty;

        [TextArea(2, 4)]
        [Tooltip("Summary shown in quest logs.")]
        [SerializeField] private string _description = string.Empty;

        [Tooltip("AllParallel unlocks all steps at once; Sequential requires completing steps in order.")]
        [SerializeField] private ObjectiveMode _objectiveMode = ObjectiveMode.AllParallel;

        [Tooltip("Allows restarting this quest after completion or failure.")]
        [SerializeField] private bool _repeatable;

        [Tooltip("Quest IDs that must be completed before this quest can start.")]
        [SerializeField] private List<string> _prerequisiteQuestIds = new List<string>();

        [Tooltip("Ordered objectives tracked for this quest.")]
        [SerializeField] private List<ObjectiveEntry> _objectives = new List<ObjectiveEntry>();

        public string QuestId => string.IsNullOrWhiteSpace(_questId) ? name : _questId.Trim();
        public string Title => string.IsNullOrWhiteSpace(_title) ? QuestId : _title.Trim();

        private void OnValidate()
        {
            if (_objectives != null)
            {
                for (int i = 0; i < _objectives.Count; i++)
                {
                    ObjectiveEntry e = _objectives[i];
                    if (e.targetCount < 1)
                    {
                        e.targetCount = 1;
                        _objectives[i] = e;
                    }
                }
            }
        }

        public QuestDefinition ToDefinition()
        {
            var objs = new List<ObjectiveDefinition>(_objectives != null ? _objectives.Count : 0);
            if (_objectives != null)
            {
                for (int i = 0; i < _objectives.Count; i++)
                {
                    ObjectiveEntry e = _objectives[i];
                    if (string.IsNullOrWhiteSpace(e.objectiveId))
                        continue;

                    objs.Add(new ObjectiveDefinition(
                        e.objectiveId,
                        e.description,
                        Mathf.Max(1, e.targetCount),
                        e.isOptional,
                        e.signalKey));
                }
            }

            return new QuestDefinition(
                QuestId,
                Title,
                _description,
                _objectiveMode,
                _prerequisiteQuestIds,
                objs,
                _repeatable);
        }

        public List<string> ValidateAsset()
        {
            var issues = ToDefinition().Validate();
            if (_objectives != null)
            {
                for (int i = 0; i < _objectives.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(_objectives[i].objectiveId))
                        issues.Add($"Objective entry {i} has an empty Objective Id and will be skipped.");
                }
            }
            return issues;
        }
    }
}

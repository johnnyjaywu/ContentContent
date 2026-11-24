using System;
using System.Collections.Generic;

namespace ContentContent.Quests
{
    /// <summary>
    /// Tracks quest states across parallel and sequential objectives, prerequisite chains,
    /// signal-driven fan-out updates, and save/load snapshots.
    /// </summary>
    public sealed class QuestJournal
    {
        private readonly Dictionary<string, QuestState> _quests = new Dictionary<string, QuestState>(StringComparer.Ordinal);
        private readonly List<string> _registrationOrder = new List<string>();
        private readonly List<string> _signalScratchIds = new List<string>(16);

        public event Action<QuestState> OnQuestStarted;
        public event Action<QuestState, ObjectiveProgress> OnObjectiveProgressed;
        public event Action<QuestState> OnQuestCompleted;
        public event Action<QuestState> OnQuestFailed;

        public void RegisterQuest(QuestDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!_quests.ContainsKey(definition.Id))
            {
                _registrationOrder.Add(definition.Id);
            }
            _quests[definition.Id] = new QuestState(definition);
        }

        public bool TryGetDefinition(string questId, out QuestDefinition definition)
        {
            if (!string.IsNullOrEmpty(questId) && _quests.TryGetValue(questId, out QuestState state))
            {
                definition = state.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        public QuestStatus GetStatus(string questId) =>
            !string.IsNullOrEmpty(questId) && _quests.TryGetValue(questId, out QuestState state)
                ? state.Status
                : QuestStatus.NotStarted;

        public bool TryGetState(string questId, out QuestState state)
        {
            if (string.IsNullOrEmpty(questId))
            {
                state = null;
                return false;
            }
            return _quests.TryGetValue(questId, out state);
        }

        public bool CanStart(string questId)
        {
            if (string.IsNullOrEmpty(questId) || !_quests.TryGetValue(questId, out QuestState state))
                return false;

            if (state.Status == QuestStatus.Active)
                return false;

            if (state.Status != QuestStatus.NotStarted && !state.Definition.Repeatable)
                return false;

            IReadOnlyList<string> prereqs = state.Definition.PrerequisiteQuestIds;
            for (int i = 0; i < prereqs.Count; i++)
            {
                if (!_quests.TryGetValue(prereqs[i], out QuestState prereqState))
                    return false;

                if (prereqState.Status != QuestStatus.Completed && prereqState.CompletionCount <= 0)
                    return false;
            }

            return true;
        }

        public bool TryStartQuest(string questId)
        {
            if (!CanStart(questId))
                return false;

            QuestState state = _quests[questId];
            state.ResetCounts();
            state.Status = QuestStatus.Active;
            OnQuestStarted?.Invoke(state);
            return true;
        }

        public bool ReportProgress(string questId, string objectiveId, int delta = 1)
        {
            if (delta <= 0 || string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(objectiveId))
                return false;

            if (!_quests.TryGetValue(questId, out QuestState state) || state.Status != QuestStatus.Active)
                return false;

            for (int i = 0; i < state.Objectives.Count; i++)
            {
                ObjectiveProgress obj = state.Objectives[i];
                if (!string.Equals(obj.Definition.Id, objectiveId, StringComparison.Ordinal))
                    continue;

                if (!state.IsObjectiveUnlocked(i) || obj.IsComplete)
                    return false;

                ApplyDeltaAndCheckCompletion(state, obj, delta);
                return true;
            }

            return false;
        }

        public int ReportSignal(string signalKey, int delta = 1)
        {
            if (delta <= 0 || string.IsNullOrWhiteSpace(signalKey))
                return 0;

            string trimmedSignal = signalKey.Trim();
            _signalScratchIds.Clear();
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                string id = _registrationOrder[i];
                if (_quests[id].Status == QuestStatus.Active)
                    _signalScratchIds.Add(id);
            }

            int updatedObjectives = 0;
            for (int q = 0; q < _signalScratchIds.Count; q++)
            {
                QuestState state = _quests[_signalScratchIds[q]];
                if (state.Status != QuestStatus.Active)
                    continue;

                // Snapshot unlocked eligibility before advancing so one signal never cascades through two sequential stages.
                var matchingIndices = new List<int>(2);
                for (int i = 0; i < state.Objectives.Count; i++)
                {
                    ObjectiveProgress obj = state.Objectives[i];
                    if (obj.IsComplete || !state.IsObjectiveUnlocked(i))
                        continue;

                    if (string.Equals(obj.Definition.SignalKey, trimmedSignal, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingIndices.Add(i);
                    }
                }

                for (int m = 0; m < matchingIndices.Count; m++)
                {
                    if (state.Status != QuestStatus.Active)
                        break;

                    ObjectiveProgress obj = state.Objectives[matchingIndices[m]];
                    ApplyDeltaAndCheckCompletion(state, obj, delta);
                    updatedObjectives++;
                }
            }

            return updatedObjectives;
        }

        public bool TryFailQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId) || !_quests.TryGetValue(questId, out QuestState state))
                return false;

            if (state.Status != QuestStatus.Active)
                return false;

            state.Status = QuestStatus.Failed;
            OnQuestFailed?.Invoke(state);
            return true;
        }

        public bool AbandonQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId) || !_quests.TryGetValue(questId, out QuestState state))
                return false;

            if (state.Status != QuestStatus.Active)
                return false;

            state.ResetCounts();
            state.Status = QuestStatus.NotStarted;
            return true;
        }

        public List<QuestState> GetActiveQuests()
        {
            var results = new List<QuestState>();
            GetActiveQuests(results);
            return results;
        }

        public void GetActiveQuests(List<QuestState> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                QuestState state = _quests[_registrationOrder[i]];
                if (state.Status == QuestStatus.Active)
                    results.Add(state);
            }
        }

        public List<QuestState> GetCompletedQuests()
        {
            var results = new List<QuestState>();
            GetCompletedQuests(results);
            return results;
        }

        public void GetCompletedQuests(List<QuestState> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                QuestState state = _quests[_registrationOrder[i]];
                if (state.Status == QuestStatus.Completed)
                    results.Add(state);
            }
        }

        public List<QuestSnapshot> ExportSnapshots()
        {
            var list = new List<QuestSnapshot>(_registrationOrder.Count);
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                QuestState s = _quests[_registrationOrder[i]];
                var snap = new QuestSnapshot
                {
                    QuestId = s.Definition.Id,
                    Status = s.Status,
                    CompletionCount = s.CompletionCount
                };

                for (int o = 0; o < s.Objectives.Count; o++)
                {
                    ObjectiveProgress op = s.Objectives[o];
                    snap.ObjectiveCounts[op.Definition.Id] = op.CurrentCount;
                }

                list.Add(snap);
            }
            return list;
        }

        public void ImportSnapshots(IReadOnlyList<QuestSnapshot> snapshots)
        {
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                QuestState s = _quests[_registrationOrder[i]];
                s.ResetCounts();
                s.Status = QuestStatus.NotStarted;
                s.CompletionCount = 0;
            }

            if (snapshots == null)
                return;

            for (int i = 0; i < snapshots.Count; i++)
            {
                QuestSnapshot snap = snapshots[i];
                if (snap == null || string.IsNullOrEmpty(snap.QuestId))
                    continue;

                if (!_quests.TryGetValue(snap.QuestId, out QuestState state))
                    continue;

                state.Status = snap.Status;
                state.CompletionCount = Math.Max(0, snap.CompletionCount);

                if (snap.ObjectiveCounts != null)
                {
                    for (int o = 0; o < state.Objectives.Count; o++)
                    {
                        ObjectiveProgress op = state.Objectives[o];
                        if (snap.ObjectiveCounts.TryGetValue(op.Definition.Id, out int count))
                        {
                            op.CurrentCount = Math.Max(0, Math.Min(op.Definition.TargetCount, count));
                        }
                    }
                }
            }
        }

        private void ApplyDeltaAndCheckCompletion(QuestState state, ObjectiveProgress objective, int delta)
        {
            int next = Math.Min(objective.Definition.TargetCount, objective.CurrentCount + delta);
            if (next != objective.CurrentCount)
            {
                objective.CurrentCount = next;
                OnObjectiveProgressed?.Invoke(state, objective);
            }

            if (state.Status == QuestStatus.Active && state.AreRequiredObjectivesComplete)
            {
                state.Status = QuestStatus.Completed;
                state.CompletionCount++;
                OnQuestCompleted?.Invoke(state);
            }
        }
    }
}

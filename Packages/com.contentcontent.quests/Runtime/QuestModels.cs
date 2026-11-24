using System;
using System.Collections.Generic;

namespace ContentContent.Quests
{
    public enum QuestStatus
    {
        NotStarted = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    public enum ObjectiveMode
    {
        AllParallel = 0,
        Sequential = 1
    }

    public sealed class ObjectiveDefinition
    {
        public string Id { get; }
        public string Description { get; }
        public int TargetCount { get; }
        public bool IsOptional { get; }
        public string SignalKey { get; }

        public ObjectiveDefinition(
            string id,
            string description = null,
            int targetCount = 1,
            bool isOptional = false,
            string signalKey = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ObjectiveDefinition.Id must not be empty.", nameof(id));

            Id = id.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? Id : description.Trim();
            TargetCount = Math.Max(1, targetCount);
            IsOptional = isOptional;
            SignalKey = string.IsNullOrWhiteSpace(signalKey) ? Id : signalKey.Trim();
        }
    }

    public sealed class QuestDefinition
    {
        private static readonly string[] EmptyStrings = Array.Empty<string>();
        private static readonly ObjectiveDefinition[] EmptyObjectives = Array.Empty<ObjectiveDefinition>();

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public ObjectiveMode ObjectiveMode { get; }
        public IReadOnlyList<string> PrerequisiteQuestIds { get; }
        public IReadOnlyList<ObjectiveDefinition> Objectives { get; }
        public bool Repeatable { get; }

        public QuestDefinition(
            string id,
            string title = null,
            string description = null,
            ObjectiveMode objectiveMode = ObjectiveMode.AllParallel,
            IEnumerable<string> prerequisiteQuestIds = null,
            IEnumerable<ObjectiveDefinition> objectives = null,
            bool repeatable = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("QuestDefinition.Id must not be empty.", nameof(id));

            Id = id.Trim();
            Title = string.IsNullOrWhiteSpace(title) ? Id : title.Trim();
            Description = description ?? string.Empty;
            ObjectiveMode = objectiveMode;
            Repeatable = repeatable;

            if (prerequisiteQuestIds == null)
            {
                PrerequisiteQuestIds = EmptyStrings;
            }
            else
            {
                var prereqs = new List<string>();
                foreach (string p in prerequisiteQuestIds)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                        prereqs.Add(p.Trim());
                }
                PrerequisiteQuestIds = prereqs.Count > 0 ? prereqs.ToArray() : EmptyStrings;
            }

            if (objectives == null)
            {
                Objectives = EmptyObjectives;
            }
            else
            {
                var objs = new List<ObjectiveDefinition>();
                foreach (ObjectiveDefinition o in objectives)
                {
                    if (o != null)
                        objs.Add(o);
                }
                Objectives = objs.Count > 0 ? objs.ToArray() : EmptyObjectives;
            }
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            if (Objectives.Count == 0)
            {
                issues.Add($"Quest '{Id}' has no objectives.");
            }
            else
            {
                bool hasRequired = false;
                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < Objectives.Count; i++)
                {
                    ObjectiveDefinition o = Objectives[i];
                    if (!seenIds.Add(o.Id))
                        issues.Add($"Quest '{Id}' has duplicate objective id '{o.Id}'.");
                    if (!o.IsOptional)
                        hasRequired = true;
                }

                if (!hasRequired)
                    issues.Add($"Quest '{Id}' has only optional objectives; add at least one required objective.");
            }

            for (int i = 0; i < PrerequisiteQuestIds.Count; i++)
            {
                if (string.Equals(PrerequisiteQuestIds[i], Id, StringComparison.Ordinal))
                    issues.Add($"Quest '{Id}' lists itself as a prerequisite.");
            }

            return issues;
        }
    }

    public sealed class ObjectiveProgress
    {
        public ObjectiveDefinition Definition { get; }
        public int CurrentCount { get; internal set; }
        public bool IsComplete => CurrentCount >= Definition.TargetCount;
        public float NormalizedProgress => Definition.TargetCount > 0
            ? Math.Min(1f, (float)CurrentCount / Definition.TargetCount)
            : 1f;

        public ObjectiveProgress(ObjectiveDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }

    public sealed class QuestState
    {
        private readonly ObjectiveProgress[] _objectives;

        public QuestDefinition Definition { get; }
        public QuestStatus Status { get; internal set; }
        public IReadOnlyList<ObjectiveProgress> Objectives => _objectives;
        public int CompletionCount { get; internal set; }

        public bool AreRequiredObjectivesComplete
        {
            get
            {
                for (int i = 0; i < _objectives.Length; i++)
                {
                    if (!_objectives[i].Definition.IsOptional && !_objectives[i].IsComplete)
                        return false;
                }
                return true;
            }
        }

        public float NormalizedProgress
        {
            get
            {
                if (Status == QuestStatus.Completed)
                    return 1f;

                float sum = 0f;
                int count = 0;
                for (int i = 0; i < _objectives.Length; i++)
                {
                    if (_objectives[i].Definition.IsOptional)
                        continue;

                    sum += _objectives[i].NormalizedProgress;
                    count++;
                }

                return count > 0 ? sum / count : 1f;
            }
        }

        public QuestState(QuestDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _objectives = new ObjectiveProgress[definition.Objectives.Count];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = new ObjectiveProgress(definition.Objectives[i]);
            }
            Status = QuestStatus.NotStarted;
        }

        public bool IsObjectiveUnlocked(int index)
        {
            if (index < 0 || index >= _objectives.Length)
                return false;

            if (Definition.ObjectiveMode == ObjectiveMode.AllParallel)
                return true;

            for (int i = 0; i < index; i++)
            {
                if (!_objectives[i].Definition.IsOptional && !_objectives[i].IsComplete)
                    return false;
            }
            return true;
        }

        internal void ResetCounts()
        {
            for (int i = 0; i < _objectives.Length; i++)
                _objectives[i].CurrentCount = 0;
        }
    }

    public sealed class QuestSnapshot
    {
        public string QuestId;
        public QuestStatus Status;
        public int CompletionCount;
        public Dictionary<string, int> ObjectiveCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    }
}

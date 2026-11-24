using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Quests.Unity
{
    /// <summary>
    /// Code-free scene quest log hosting a <see cref="QuestJournal"/> with catalog registration,
    /// starting quests, signal reporting, and Inspector UnityEvents.
    /// </summary>
    [AddComponentMenu("ContentContent/Quests/Quest Log")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/quests/quickstart")]
    public class QuestLogComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional QuestCatalogAsset defining available quests.")]
        [SerializeField] private QuestCatalogAsset _catalog;

        [Tooltip("Quests automatically started when the log initializes.")]
        [SerializeField] private List<QuestDefinitionAsset> _startingQuests = new List<QuestDefinitionAsset>();

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onQuestStarted = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string, string> _onObjectiveProgressed = new UnityEvent<string, string>();
        [SerializeField] private UnityEvent<string> _onQuestCompleted = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onQuestFailed = new UnityEvent<string>();

        private QuestJournal _journal;

        public QuestJournal Journal
        {
            get
            {
                EnsureInitialized();
                return _journal;
            }
        }

        public QuestCatalogAsset Catalog => _catalog;
        public UnityEvent<string> OnQuestStarted => _onQuestStarted ?? (_onQuestStarted = new UnityEvent<string>());
        public UnityEvent<string, string> OnObjectiveProgressed => _onObjectiveProgressed ?? (_onObjectiveProgressed = new UnityEvent<string, string>());
        public UnityEvent<string> OnQuestCompleted => _onQuestCompleted ?? (_onQuestCompleted = new UnityEvent<string>());
        public UnityEvent<string> OnQuestFailed => _onQuestFailed ?? (_onQuestFailed = new UnityEvent<string>());

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (_journal == null)
                return;

            _journal.OnQuestStarted -= HandleStarted;
            _journal.OnObjectiveProgressed -= HandleProgressed;
            _journal.OnQuestCompleted -= HandleCompleted;
            _journal.OnQuestFailed -= HandleFailed;
        }

        public bool StartQuest(QuestDefinitionAsset questAsset)
        {
            if (questAsset == null)
                return false;

            if (!Journal.TryGetDefinition(questAsset.QuestId, out _))
                Journal.RegisterQuest(questAsset.ToDefinition());

            return Journal.TryStartQuest(questAsset.QuestId);
        }

        public bool StartQuestById(string questId) => Journal.TryStartQuest(questId);

        public void ReportSignal(string signalKey) => Journal.ReportSignal(signalKey, 1);

        public int ReportSignalAmount(string signalKey, int amount) => Journal.ReportSignal(signalKey, amount);

        public bool FailQuestById(string questId) => Journal.TryFailQuest(questId);

        private void EnsureInitialized()
        {
            if (_journal != null)
                return;

            _journal = new QuestJournal();
            if (_catalog != null)
                _catalog.RegisterAll(_journal);

            _journal.OnQuestStarted += HandleStarted;
            _journal.OnObjectiveProgressed += HandleProgressed;
            _journal.OnQuestCompleted += HandleCompleted;
            _journal.OnQuestFailed += HandleFailed;

            if (_startingQuests != null)
            {
                for (int i = 0; i < _startingQuests.Count; i++)
                {
                    QuestDefinitionAsset q = _startingQuests[i];
                    if (q != null)
                        StartQuest(q);
                }
            }
        }

        private void HandleStarted(QuestState state) => _onQuestStarted?.Invoke(state.Definition.Id);

        private void HandleProgressed(QuestState state, ObjectiveProgress obj) =>
            _onObjectiveProgressed?.Invoke(state.Definition.Id, obj.Definition.Id);

        private void HandleCompleted(QuestState state) => _onQuestCompleted?.Invoke(state.Definition.Id);

        private void HandleFailed(QuestState state) => _onQuestFailed?.Invoke(state.Definition.Id);
    }
}

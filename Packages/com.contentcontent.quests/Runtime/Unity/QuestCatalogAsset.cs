using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Quests.Unity
{
    /// <summary>
    /// Shared <see cref="ScriptableObject"/> catalog registering multiple <see cref="QuestDefinitionAsset"/>
    /// definitions into a <see cref="QuestLogComponent"/> at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "ContentContent/Quests/Quest Catalog", order = 1)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/quests/quickstart")]
    public class QuestCatalogAsset : ScriptableObject
    {
        [Tooltip("All quests registered automatically when a QuestLogComponent initializes.")]
        [SerializeField] private List<QuestDefinitionAsset> _quests = new List<QuestDefinitionAsset>();

        public IReadOnlyList<QuestDefinitionAsset> Quests => _quests;

        public void RegisterAll(QuestJournal journal)
        {
            if (journal == null || _quests == null)
                return;

            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i] != null)
                    journal.RegisterQuest(_quests[i].ToDefinition());
            }
        }

        public QuestDefinitionAsset FindById(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) || _quests == null)
                return null;

            string trimmed = questId.Trim();
            for (int i = 0; i < _quests.Count; i++)
            {
                QuestDefinitionAsset q = _quests[i];
                if (q != null && string.Equals(q.QuestId, trimmed, StringComparison.Ordinal))
                    return q;
            }
            return null;
        }
    }
}

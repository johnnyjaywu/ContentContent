using UnityEngine;

namespace ContentContent.Quests.Unity
{
    /// <summary>
    /// Parameterless bridge allowing zero-code UnityEvents (such as <c>HealthComponent.OnDied</c>,
    /// <c>InteractableBehaviour.OnInteracted</c>, or <c>DialogRunnerBehaviour.CommandBindings</c>)
    /// to emit quest progress signals into a scene <see cref="QuestLogComponent"/>.
    /// </summary>
    [AddComponentMenu("ContentContent/Quests/Quest Signal Emitter")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/quests/quickstart")]
    public class QuestSignalEmitter : MonoBehaviour
    {
        [Tooltip("Target QuestLogComponent. Auto-discovered in the scene if omitted.")]
        [SerializeField] private QuestLogComponent _questLog;

        [Tooltip("Signal key matching one or more quest ObjectiveDefinition.SignalKey values.")]
        [SerializeField] private string _signalKey = string.Empty;

        [Tooltip("Default progress delta emitted when calling Emit().")]
        [SerializeField, Min(1)] private int _amount = 1;

        private void Reset()
        {
            if (_questLog == null)
                _questLog = FindAnyObjectByType<QuestLogComponent>();
        }

        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
        }

        private void Awake()
        {
            if (_questLog == null)
                _questLog = FindAnyObjectByType<QuestLogComponent>();
        }

        /// <summary>Emits the configured signal key and amount to <see cref="QuestLogComponent"/>.</summary>
        public void Emit() => EmitAmount(_amount);

        /// <summary>Emits the configured signal key with an explicit delta.</summary>
        public void EmitAmount(int delta)
        {
            if (delta <= 0 || string.IsNullOrWhiteSpace(_signalKey))
                return;

            if (_questLog == null)
                _questLog = FindAnyObjectByType<QuestLogComponent>();

            if (_questLog != null)
                _questLog.ReportSignalAmount(_signalKey, delta);
        }
    }
}

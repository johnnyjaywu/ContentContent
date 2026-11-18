using UnityEngine;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// MonoBehaviour adapter hosting a <see cref="StatCollection{TStatId}"/> and <see cref="BuffController{TStatId}"/>.
    /// </summary>
    public class StatsComponent : MonoBehaviour
    {
        [SerializeField] private StatSheetAsset _initialSheet;
        [SerializeField] private bool _useUnscaledTime;

        public StatCollection<string> Stats { get; private set; }
        public BuffController<string> Buffs { get; private set; }

        private void Awake()
        {
            Stats = new StatCollection<string>();
            if (_initialSheet != null)
            {
                _initialSheet.Populate(Stats);
            }
            Buffs = new BuffController<string>(Stats);
        }

        private void Update()
        {
            if (Buffs == null)
                return;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Buffs.Tick(dt);
        }
    }
}

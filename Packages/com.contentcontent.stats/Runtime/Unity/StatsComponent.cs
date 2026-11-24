using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// Code-free MonoBehaviour hosting a <see cref="StatCollection{TStatId}"/> and <see cref="BuffController{TStatId}"/>.
    /// Initializes lazily so sibling components such as <see cref="HealthComponent"/> can bind safely in any Awake order.
    /// </summary>
    [AddComponentMenu("ContentContent/Stats/Stats Component")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/stats/quickstart")]
    public class StatsComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional ScriptableObject sheet defining initial stats and min/max clamps.")]
        [SerializeField] private StatSheetAsset _initialSheet;

        [Tooltip("Ticks timed buff durations using Time.unscaledDeltaTime instead of Time.deltaTime.")]
        [SerializeField] private bool _useUnscaledTime;

        [Tooltip("Optional BuffAssets applied automatically when the entity starts.")]
        [SerializeField] private List<BuffAsset> _startingBuffs = new List<BuffAsset>();

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onBuffApplied = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onBuffExpired = new UnityEvent<string>();

        private StatCollection<string> _stats;
        private BuffController<string> _buffs;

        public StatSheetAsset InitialSheet
        {
            get => _initialSheet;
            set => _initialSheet = value;
        }

        public StatCollection<string> Stats
        {
            get
            {
                EnsureInitialized();
                return _stats;
            }
        }

        public BuffController<string> Buffs
        {
            get
            {
                EnsureInitialized();
                return _buffs;
            }
        }

        public UnityEvent<string> OnBuffApplied => _onBuffApplied ?? (_onBuffApplied = new UnityEvent<string>());
        public UnityEvent<string> OnBuffExpired => _onBuffExpired ?? (_onBuffExpired = new UnityEvent<string>());

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (_startingBuffs == null)
                return;

            for (int i = 0; i < _startingBuffs.Count; i++)
            {
                if (_startingBuffs[i] != null)
                {
                    ApplyBuff(_startingBuffs[i]);
                }
            }
        }

        private void Update()
        {
            if (_buffs == null)
                return;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _buffs.Tick(dt);
        }

        private void OnDestroy()
        {
            if (_buffs != null)
            {
                _buffs.OnBuffApplied -= HandleBuffApplied;
                _buffs.OnBuffExpired -= HandleBuffExpired;
                _buffs.ClearAll();
            }
        }

        /// <summary>Applies a <see cref="BuffAsset"/> (safe to wire directly from UnityEvents).</summary>
        public void ApplyBuff(BuffAsset buffAsset)
        {
            if (buffAsset == null)
                return;

            Buffs.ApplyBuff(buffAsset.BuildDefinition());
        }

        /// <summary>Removes an active buff matching <paramref name="buffAsset"/>.</summary>
        public void RemoveBuff(BuffAsset buffAsset)
        {
            if (buffAsset == null)
                return;

            Buffs.RemoveBuff(buffAsset.BuffId);
        }

        /// <summary>Removes an active buff by string ID.</summary>
        public void RemoveBuffById(string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
                return;

            Buffs.RemoveBuff(buffId);
        }

        /// <summary>Returns the current computed value of <paramref name="statId"/> or <paramref name="fallback"/>.</summary>
        public float GetStatValue(string statId, float fallback = 0f)
        {
            return Stats.GetValue(statId, fallback);
        }

        private void EnsureInitialized()
        {
            if (_stats != null)
                return;

            _stats = new StatCollection<string>();
            if (_initialSheet != null)
            {
                _initialSheet.Populate(_stats);
            }

            _buffs = new BuffController<string>(_stats);
            _buffs.OnBuffApplied += HandleBuffApplied;
            _buffs.OnBuffExpired += HandleBuffExpired;
        }

        private void HandleBuffApplied(ActiveBuff<string> buff) => _onBuffApplied?.Invoke(buff.Definition.Id);
        private void HandleBuffExpired(ActiveBuff<string> buff) => _onBuffExpired?.Invoke(buff.Definition.Id);
    }
}

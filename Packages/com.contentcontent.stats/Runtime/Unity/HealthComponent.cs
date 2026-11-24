using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// Code-free MonoBehaviour adapter around <see cref="Health"/> implementing <see cref="IDamageable"/>,
    /// automatically binding to a sibling <see cref="StatsComponent"/>'s MaxHealth stat when present.
    /// </summary>
    [AddComponentMenu("ContentContent/Stats/Health Component")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/stats/quickstart")]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Health Defaults")]
        [Tooltip("Fallback Max Health used when not bound to a StatsComponent stat.")]
        [SerializeField] private float _maxHealth = 100f;

        [Tooltip("Starts at full Max Health instead of Initial Health.")]
        [SerializeField] private bool _startAtMaxHealth = true;

        [SerializeField] private float _initialHealth = 100f;

        [Header("Stats Integration (Auto-Wired)")]
        [Tooltip("Automatically binds Max Health to a Stat on a sibling or assigned StatsComponent.")]
        [SerializeField] private bool _bindToStatsComponent = true;

        [Tooltip("Target StatsComponent. Auto-detected on this GameObject if left empty.")]
        [SerializeField] private StatsComponent _statsComponent;

        [Tooltip("Stat ID in StatsComponent to bind as Max Health.")]
        [SerializeField] private string _maxHealthStatId = "MaxHealth";

        [Tooltip("How current HP adjusts when Max Health changes dynamically from buffs or equipment.")]
        [SerializeField] private MaxHealthChangePolicy _maxHealthPolicy = MaxHealthChangePolicy.KeepPercentage;

        [Header("Events")]
        [SerializeField] private UnityEvent<float, float> _onHealthChanged = new UnityEvent<float, float>();
        [SerializeField] private UnityEvent<float> _onDamaged = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> _onHealed = new UnityEvent<float>();
        [SerializeField] private UnityEvent _onDied = new UnityEvent();
        [SerializeField] private UnityEvent _onRevived = new UnityEvent();

        private Health _health;
        private bool _isBoundToStat;

        public Health Health
        {
            get
            {
                EnsureInitialized();
                return _health;
            }
        }

        public StatsComponent StatsComponent => _statsComponent;
        public bool BindToStatsComponent => _bindToStatsComponent;
        public string MaxHealthStatId => _maxHealthStatId;

        public bool IsBoundToStat
        {
            get
            {
                EnsureInitialized();
                return _isBoundToStat;
            }
            private set => _isBoundToStat = value;
        }

        public float Current => Health.Current;
        public float Max => Health.Max;
        public bool IsAlive => Health.IsAlive;

        public UnityEvent<float, float> OnHealthChanged => _onHealthChanged ?? (_onHealthChanged = new UnityEvent<float, float>());
        public UnityEvent<float> OnDamaged => _onDamaged ?? (_onDamaged = new UnityEvent<float>());
        public UnityEvent<float> OnHealed => _onHealed ?? (_onHealed = new UnityEvent<float>());
        public UnityEvent OnDied => _onDied ?? (_onDied = new UnityEvent());
        public UnityEvent OnRevived => _onRevived ?? (_onRevived = new UnityEvent());

        private void Reset()
        {
            if (_statsComponent == null)
            {
                _statsComponent = GetComponent<StatsComponent>();
            }
        }

        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _initialHealth = Mathf.Clamp(_initialHealth, 0f, _maxHealth);
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public DamageResult ApplyDamage(DamageInfo damage) => Health.ApplyDamage(damage);

        /// <summary>Applies raw numeric damage (convenient for Inspector UnityEvents).</summary>
        public void ApplyFlatDamage(float amount) => Health.ApplyDamage(new DamageInfo(amount, source: this));

        public float Heal(float amount) => Health.Heal(amount);

        /// <summary>Restores health back to Max Health while alive.</summary>
        public void FullHeal() => Health.Heal(Health.Max);

        /// <summary>Applies fatal damage equal to current HP.</summary>
        public void Kill()
        {
            if (Health.IsAlive)
            {
                Health.ApplyDamage(new DamageInfo(Health.Current, source: this));
            }
        }

        public void Revive(float? healthAmount = null) => Health.Revive(healthAmount);

        /// <summary>Explicitly binds this component to <paramref name="statsComp"/> at runtime.</summary>
        public bool TryBindToStats(StatsComponent statsComp)
        {
            _statsComponent = statsComp;
            EnsureInitialized();
            return TryBindInternal();
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnDamaged -= HandleDamaged;
                _health.OnHealed -= HandleHealed;
                _health.OnDied -= HandleDied;
                _health.OnRevived -= HandleRevived;
                _health.Dispose();
                _health = null;
            }

            IsBoundToStat = false;
        }

        private void EnsureInitialized()
        {
            if (_health != null)
                return;

            if (_bindToStatsComponent && _statsComponent == null)
            {
                _statsComponent = GetComponent<StatsComponent>();
            }

            float startingMax = Mathf.Max(1f, _maxHealth);
            Stat boundStat = null;
            if (_bindToStatsComponent &&
                _statsComponent != null &&
                !string.IsNullOrEmpty(_maxHealthStatId) &&
                _statsComponent.Stats.TryGetStat(_maxHealthStatId, out boundStat))
            {
                startingMax = Mathf.Max(1f, boundStat.Value);
            }

            float initial = _startAtMaxHealth ? startingMax : Mathf.Clamp(_initialHealth, 0f, startingMax);
            _health = new Health(startingMax, initial);

            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDamaged += HandleDamaged;
            _health.OnHealed += HandleHealed;
            _health.OnDied += HandleDied;
            _health.OnRevived += HandleRevived;

            if (boundStat != null)
            {
                _health.BindMaxHealthStat(boundStat, _maxHealthPolicy);
                IsBoundToStat = true;
            }
        }

        private bool TryBindInternal()
        {
            if (_health == null || _statsComponent == null || string.IsNullOrEmpty(_maxHealthStatId))
            {
                IsBoundToStat = false;
                return false;
            }

            if (_statsComponent.Stats.TryGetStat(_maxHealthStatId, out Stat stat))
            {
                _health.BindMaxHealthStat(stat, _maxHealthPolicy);
                IsBoundToStat = true;
                return true;
            }

            IsBoundToStat = false;
            return false;
        }

        private void HandleHealthChanged(float current, float max) => _onHealthChanged?.Invoke(current, max);
        private void HandleDamaged(DamageInfo info, DamageResult result) => _onDamaged?.Invoke(result.AppliedDamage);
        private void HandleHealed(float amount) => _onHealed?.Invoke(amount);
        private void HandleDied(DamageInfo info) => _onDied?.Invoke();
        private void HandleRevived() => _onRevived?.Invoke();
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace GameSystem.Stats.Unity
{
    /// <summary>
    /// MonoBehaviour adapter around <see cref="Health"/> implementing <see cref="IDamageable"/>
    /// with Inspector-friendly <see cref="UnityEvent"/> hooks.
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private bool _startAtMaxHealth = true;
        [SerializeField] private float _initialHealth = 100f;

        [Header("Events")]
        [SerializeField] private UnityEvent<float, float> _onHealthChanged;
        [SerializeField] private UnityEvent<float> _onDamaged;
        [SerializeField] private UnityEvent<float> _onHealed;
        [SerializeField] private UnityEvent _onDied;
        [SerializeField] private UnityEvent _onRevived;

        public Health Health { get; private set; }

        public float Current => Health?.Current ?? 0f;
        public float Max => Health?.Max ?? _maxHealth;
        public bool IsAlive => Health != null && Health.IsAlive;

        private void Awake()
        {
            float initial = _startAtMaxHealth ? _maxHealth : _initialHealth;
            Health = new Health(Mathf.Max(1f, _maxHealth), initial);

            Health.OnHealthChanged += HandleHealthChanged;
            Health.OnDamaged += HandleDamaged;
            Health.OnHealed += HandleHealed;
            Health.OnDied += HandleDied;
            Health.OnRevived += HandleRevived;
        }

        public DamageResult ApplyDamage(DamageInfo damage)
        {
            return Health.ApplyDamage(damage);
        }

        public float Heal(float amount)
        {
            return Health.Heal(amount);
        }

        public void Revive(float? healthAmount = null)
        {
            Health.Revive(healthAmount);
        }

        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.OnHealthChanged -= HandleHealthChanged;
                Health.OnDamaged -= HandleDamaged;
                Health.OnHealed -= HandleHealed;
                Health.OnDied -= HandleDied;
                Health.OnRevived -= HandleRevived;
                Health.Dispose();
            }
        }

        private void HandleHealthChanged(float current, float max) => _onHealthChanged?.Invoke(current, max);
        private void HandleDamaged(DamageInfo info, DamageResult result) => _onDamaged?.Invoke(result.AppliedDamage);
        private void HandleHealed(float amount) => _onHealed?.Invoke(amount);
        private void HandleDied(DamageInfo info) => _onDied?.Invoke();
        private void HandleRevived() => _onRevived?.Invoke();
    }
}

using System;

namespace ContentContent.Stats
{
    /// <summary>
    /// Describes an incoming damage event.
    /// </summary>
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public object Source { get; }
        public string DamageType { get; }
        public bool IsCritical { get; }

        public DamageInfo(float amount, object source = null, string damageType = null, bool isCritical = false)
        {
            Amount = amount;
            Source = source;
            DamageType = damageType;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// Outcome returned after applying a <see cref="DamageInfo"/> to an <see cref="IDamageable"/>.
    /// </summary>
    public readonly struct DamageResult
    {
        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public float RemainingHealth { get; }
        public bool WasFatal { get; }
        public bool WasBlocked { get; }

        public DamageResult(float requestedDamage, float appliedDamage, float remainingHealth, bool wasFatal, bool wasBlocked)
        {
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            RemainingHealth = remainingHealth;
            WasFatal = wasFatal;
            WasBlocked = wasBlocked;
        }
    }

    /// <summary>
    /// Contract for any entity capable of receiving damage.
    /// </summary>
    public interface IDamageable
    {
        DamageResult ApplyDamage(DamageInfo damage);
    }

    /// <summary>
    /// Determines how current health reacts when maximum health changes dynamically.
    /// </summary>
    public enum MaxHealthChangePolicy
    {
        /// <summary>Keep current health unchanged (clamped to new max).</summary>
        KeepCurrent,

        /// <summary>Preserve the current health percentage relative to max.</summary>
        KeepPercentage,

        /// <summary>Add positive max-health increases directly to current health.</summary>
        AddDelta
    }

    /// <summary>
    /// Pure C# health pool supporting damage, healing, invulnerability, death/revive lifecycle,
    /// and optional dynamic binding to a <see cref="Stat"/>.
    /// </summary>
    public class Health : IDamageable, IDisposable
    {
        private Stat _boundMaxStat;
        private MaxHealthChangePolicy _maxChangePolicy;
        private float _max;
        private float _current;

        public float Current => _current;
        public float Max => _max;
        public float Normalized => _max > 0f ? _current / _max : 0f;
        public bool IsAlive => _current > 0f;
        public bool IsInvulnerable { get; set; }

        /// <summary>Invoked whenever current or max health changes: (current, max).</summary>
        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo, DamageResult> OnDamaged;
        public event Action<float> OnHealed;
        public event Action<DamageInfo> OnDied;
        public event Action OnRevived;

        public Health(float maxHealth, float? initialHealth = null)
        {
            if (maxHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be greater than 0.");

            _max = maxHealth;
            _current = initialHealth.HasValue
                ? Clamp(initialHealth.Value, 0f, _max)
                : _max;
        }

        /// <summary>
        /// Binds <see cref="Max"/> to a <see cref="Stat"/> so buffs/equipment automatically update max health.
        /// </summary>
        public void BindMaxHealthStat(Stat maxHealthStat, MaxHealthChangePolicy policy = MaxHealthChangePolicy.KeepPercentage)
        {
            UnbindMaxHealthStat();

            _boundMaxStat = maxHealthStat ?? throw new ArgumentNullException(nameof(maxHealthStat));
            _maxChangePolicy = policy;
            _boundMaxStat.OnValueChanged += HandleMaxHealthStatChanged;

            SetMaxHealth(_boundMaxStat.Value, policy);
        }

        public void UnbindMaxHealthStat()
        {
            if (_boundMaxStat != null)
            {
                _boundMaxStat.OnValueChanged -= HandleMaxHealthStatChanged;
                _boundMaxStat = null;
            }
        }

        public void SetMaxHealth(float newMax, MaxHealthChangePolicy policy = MaxHealthChangePolicy.KeepCurrent)
        {
            float clampedMax = Math.Max(1f, newMax);
            if (_max.Equals(clampedMax))
                return;

            float oldMax = _max;
            float oldCurrent = _current;
            _max = clampedMax;

            switch (policy)
            {
                case MaxHealthChangePolicy.KeepCurrent:
                    _current = Clamp(_current, 0f, _max);
                    break;

                case MaxHealthChangePolicy.KeepPercentage:
                    float ratio = oldMax > 0f ? oldCurrent / oldMax : 1f;
                    _current = Clamp(ratio * _max, 0f, _max);
                    break;

                case MaxHealthChangePolicy.AddDelta:
                    float delta = _max - oldMax;
                    _current = Clamp(delta > 0f ? _current + delta : _current, 0f, _max);
                    break;
            }

            OnHealthChanged?.Invoke(_current, _max);
        }

        public DamageResult ApplyDamage(DamageInfo damage)
        {
            float requested = Math.Max(0f, damage.Amount);

            if (!IsAlive || requested <= 0f)
            {
                return new DamageResult(requested, 0f, _current, wasFatal: false, wasBlocked: false);
            }

            if (IsInvulnerable)
            {
                var blocked = new DamageResult(requested, 0f, _current, wasFatal: false, wasBlocked: true);
                OnDamaged?.Invoke(damage, blocked);
                return blocked;
            }

            float previous = _current;
            _current = Math.Max(0f, _current - requested);
            float applied = previous - _current;
            bool wasFatal = _current <= 0f;

            var result = new DamageResult(requested, applied, _current, wasFatal, wasBlocked: false);
            OnDamaged?.Invoke(damage, result);
            OnHealthChanged?.Invoke(_current, _max);

            if (wasFatal)
            {
                OnDied?.Invoke(damage);
            }

            return result;
        }

        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return 0f;

            float previous = _current;
            _current = Math.Min(_max, _current + amount);
            float healed = _current - previous;

            if (healed > 0f)
            {
                OnHealed?.Invoke(healed);
                OnHealthChanged?.Invoke(_current, _max);
            }

            return healed;
        }

        public void Revive(float? healthAmount = null)
        {
            if (IsAlive)
                return;

            float target = healthAmount.HasValue
                ? Clamp(healthAmount.Value, 1f, _max)
                : _max;

            _current = target;
            OnRevived?.Invoke();
            OnHealthChanged?.Invoke(_current, _max);
        }

        public void Dispose()
        {
            UnbindMaxHealthStat();
        }

        private void HandleMaxHealthStatChanged(float oldValue, float newValue)
        {
            SetMaxHealth(newValue, _maxChangePolicy);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

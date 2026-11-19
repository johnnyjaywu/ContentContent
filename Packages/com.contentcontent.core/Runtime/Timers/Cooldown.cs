using System;

namespace ContentContent.Core.Timers
{
    /// <summary>
    /// Allocation-free value-type cooldown tracker for abilities, weapons, or rate-limited actions.
    /// </summary>
    public struct Cooldown
    {
        private float _duration;
        private float _remaining;

        /// <summary>Configured cooldown duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Remaining seconds until ready.</summary>
        public float Remaining => _remaining;

        /// <summary>True when the cooldown has elapsed and is ready to trigger.</summary>
        public bool IsReady => _remaining <= 0f;

        /// <summary>Normalized completion ratio in [0, 1] (1 = ready).</summary>
        public float Progress
        {
            get
            {
                if (_duration <= 0f || _remaining <= 0f)
                    return 1f;

                float ratio = 1f - (_remaining / _duration);
                if (ratio < 0f) return 0f;
                if (ratio > 1f) return 1f;
                return ratio;
            }
        }

        public Cooldown(float duration, bool startReady = true)
        {
            if (duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");

            _duration = duration;
            _remaining = startReady ? 0f : duration;
        }

        /// <summary>Starts the cooldown using its configured <see cref="Duration"/>.</summary>
        public void Trigger()
        {
            _remaining = _duration;
        }

        /// <summary>Updates <see cref="Duration"/> and starts the cooldown.</summary>
        public void Trigger(float duration)
        {
            if (duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");

            _duration = duration;
            _remaining = duration;
        }

        /// <summary>
        /// If ready, triggers the cooldown and returns true; otherwise returns false.
        /// </summary>
        public bool TryConsume()
        {
            if (!IsReady)
                return false;

            _remaining = _duration;
            return true;
        }

        /// <summary>Advances the cooldown by <paramref name="deltaTime"/> and returns <see cref="IsReady"/>.</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime > 0f && _remaining > 0f)
            {
                _remaining -= deltaTime;
                if (_remaining < 0f)
                {
                    _remaining = 0f;
                }
            }

            return IsReady;
        }

        /// <summary>Immediately resets the cooldown to ready.</summary>
        public void Reset()
        {
            _remaining = 0f;
        }
    }
}

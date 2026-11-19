using System;

namespace ContentContent.Core.Timers
{
    /// <summary>
    /// Lightweight generational handle to a timer scheduled on a <see cref="TimerScheduler"/>.
    /// Stale handles (from completed or canceled timers) safely no-op on control calls.
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        private readonly TimerScheduler _scheduler;
        internal readonly int Index;
        internal readonly int Generation;

        internal TimerHandle(TimerScheduler scheduler, int index, int generation)
        {
            _scheduler = scheduler;
            Index = index;
            Generation = generation;
        }

        /// <summary>True if the timer is still active (running or paused) on its scheduler.</summary>
        public bool IsActive => _scheduler != null && _scheduler.IsActive(this);

        /// <summary>True if the timer is active and currently paused.</summary>
        public bool IsPaused => _scheduler != null && _scheduler.IsPaused(this);

        /// <summary>Remaining seconds until the next fire, or 0 if inactive.</summary>
        public float Remaining => _scheduler != null ? _scheduler.GetRemaining(this) : 0f;

        /// <summary>Cancels the timer. Returns true if the timer was active and is now canceled.</summary>
        public bool Cancel() => _scheduler != null && _scheduler.Cancel(this);

        /// <summary>Pauses the timer. Returns true if the timer is active.</summary>
        public bool Pause() => _scheduler != null && _scheduler.Pause(this);

        /// <summary>Resumes a paused timer. Returns true if the timer is active.</summary>
        public bool Resume() => _scheduler != null && _scheduler.Resume(this);

        public bool Equals(TimerHandle other)
        {
            return ReferenceEquals(_scheduler, other._scheduler)
                   && Index == other.Index
                   && Generation == other.Generation;
        }

        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _scheduler != null ? _scheduler.GetHashCode() : 0;
                hash = (hash * 397) ^ Index;
                hash = (hash * 397) ^ Generation;
                return hash;
            }
        }

        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
    }
}

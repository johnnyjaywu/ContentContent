using System;
using System.Collections.Generic;

namespace ContentContent.Core.Timers
{
    /// <summary>
    /// Deterministic, tick-driven timer scheduler supporting one-shot and repeating timers
    /// with generational <see cref="TimerHandle"/> slot reuse and catch-up safety caps.
    /// </summary>
    public class TimerScheduler
    {
        private struct Slot
        {
            public int Generation;
            public bool InUse;
            public bool IsPaused;
            public float Remaining;
            public float Interval;
            public int RemainingRepeats; // -1 = infinite, 0 = final fire
            public int ScheduledTick;
            public Action Callback;
        }

        private Slot[] _slots;
        private int _slotCount;
        private readonly Stack<int> _freeIndices = new Stack<int>();
        private int _currentTick;
        private float _timeScale = 1f;
        private int _maxFiresPerTick = 16;

        /// <summary>Number of currently active (running or paused) timers.</summary>
        public int ActiveCount { get; private set; }

        /// <summary>Multiplier applied to delta time during <see cref="Tick"/>. Must be >= 0.</summary>
        public float TimeScale
        {
            get => _timeScale;
            set
            {
                if (value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "TimeScale cannot be negative.");
                _timeScale = value;
            }
        }

        /// <summary>
        /// Maximum number of times a single repeating timer may fire within one <see cref="Tick"/>
        /// when catching up after a large delta time.
        /// </summary>
        public int MaxFiresPerTick
        {
            get => _maxFiresPerTick;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxFiresPerTick must be greater than 0.");
                _maxFiresPerTick = value;
            }
        }

        public TimerScheduler(int initialCapacity = 16, int maxFiresPerTick = 16)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative.");

            MaxFiresPerTick = maxFiresPerTick;
            _slots = new Slot[Math.Max(initialCapacity, 4)];
        }

        /// <summary>
        /// Schedules a one-shot callback to run after <paramref name="delay"/> seconds.
        /// </summary>
        public TimerHandle After(float delay, Action callback)
        {
            if (delay < 0f)
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return AllocateSlot(delay, 0f, 0, callback);
        }

        /// <summary>
        /// Schedules a repeating callback every <paramref name="interval"/> seconds.
        /// Pass <paramref name="repeatCount"/> = -1 for infinite repetition, or a positive integer
        /// for a finite number of executions.
        /// </summary>
        public TimerHandle Every(float interval, Action callback, int repeatCount = -1)
        {
            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than 0.");
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (repeatCount == 0 || repeatCount < -1)
                throw new ArgumentOutOfRangeException(nameof(repeatCount), "Repeat count must be -1 (infinite) or > 0.");

            int remainingRepeats = repeatCount == -1 ? -1 : repeatCount - 1;
            return AllocateSlot(interval, interval, remainingRepeats, callback);
        }

        /// <summary>
        /// Advances all active, unpaused timers by <paramref name="deltaTime"/> * <see cref="TimeScale"/>.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || _timeScale <= 0f)
                return;

            _currentTick++;
            float scaledDt = deltaTime * _timeScale;
            int count = _slotCount;

            for (int i = 0; i < count; i++)
            {
                if (!_slots[i].InUse || _slots[i].IsPaused || _slots[i].ScheduledTick == _currentTick)
                    continue;

                int generation = _slots[i].Generation;
                _slots[i].Remaining -= scaledDt;

                int fires = 0;
                while (_slots[i].InUse
                       && _slots[i].Generation == generation
                       && !_slots[i].IsPaused
                       && _slots[i].Remaining <= 0f
                       && fires < _maxFiresPerTick)
                {
                    fires++;
                    Action callback = _slots[i].Callback;
                    bool isFinalFire = _slots[i].Interval <= 0f || _slots[i].RemainingRepeats == 0;

                    if (isFinalFire)
                    {
                        ReleaseSlot(i);
                    }
                    else
                    {
                        if (_slots[i].RemainingRepeats > 0)
                        {
                            _slots[i].RemainingRepeats--;
                        }

                        _slots[i].Remaining += _slots[i].Interval;
                    }

                    callback();

                    if (isFinalFire)
                        break;
                }

                // If the timer hit the catch-up cap and still has negative remainder, drop excess backlog.
                if (fires >= _maxFiresPerTick
                    && _slots[i].InUse
                    && _slots[i].Generation == generation
                    && _slots[i].Remaining <= 0f
                    && _slots[i].Interval > 0f)
                {
                    _slots[i].Remaining = _slots[i].Interval;
                }
            }
        }

        public bool IsActive(TimerHandle handle)
        {
            return IsValidSlot(handle);
        }

        public bool IsPaused(TimerHandle handle)
        {
            return IsValidSlot(handle) && _slots[handle.Index].IsPaused;
        }

        public float GetRemaining(TimerHandle handle)
        {
            return IsValidSlot(handle) ? Math.Max(0f, _slots[handle.Index].Remaining) : 0f;
        }

        public bool Cancel(TimerHandle handle)
        {
            if (!IsValidSlot(handle))
                return false;

            ReleaseSlot(handle.Index);
            return true;
        }

        public bool Pause(TimerHandle handle)
        {
            if (!IsValidSlot(handle))
                return false;

            _slots[handle.Index].IsPaused = true;
            return true;
        }

        public bool Resume(TimerHandle handle)
        {
            if (!IsValidSlot(handle))
                return false;

            _slots[handle.Index].IsPaused = false;
            return true;
        }

        /// <summary>Cancels all active timers and invalidates all existing handles.</summary>
        public void CancelAll()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].InUse)
                {
                    ReleaseSlot(i);
                }
            }
        }

        private bool IsValidSlot(TimerHandle handle)
        {
            int index = handle.Index;
            return index >= 0
                   && index < _slotCount
                   && _slots[index].InUse
                   && _slots[index].Generation == handle.Generation;
        }

        private TimerHandle AllocateSlot(float delay, float interval, int remainingRepeats, Action callback)
        {
            int index;
            if (_freeIndices.Count > 0)
            {
                index = _freeIndices.Pop();
            }
            else
            {
                if (_slotCount == _slots.Length)
                {
                    Array.Resize(ref _slots, _slots.Length * 2);
                }

                index = _slotCount++;
            }

            int nextGen = _slots[index].Generation + 1;
            if (nextGen <= 0)
            {
                nextGen = 1;
            }

            _slots[index] = new Slot
            {
                Generation = nextGen,
                InUse = true,
                IsPaused = false,
                Remaining = delay,
                Interval = interval,
                RemainingRepeats = remainingRepeats,
                ScheduledTick = _currentTick,
                Callback = callback
            };

            ActiveCount++;
            return new TimerHandle(this, index, nextGen);
        }

        private void ReleaseSlot(int index)
        {
            _slots[index].InUse = false;
            _slots[index].IsPaused = false;
            _slots[index].Callback = null;
            _slots[index].Generation++;
            if (_slots[index].Generation <= 0)
            {
                _slots[index].Generation = 1;
            }

            ActiveCount--;
            _freeIndices.Push(index);
        }
    }
}

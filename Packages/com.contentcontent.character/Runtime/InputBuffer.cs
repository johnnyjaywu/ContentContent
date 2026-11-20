using System;
using System.Collections.Generic;

namespace ContentContent.Character
{
    /// <summary>
    /// Deterministic, allocation-light input buffer that remembers named button presses
    /// for a configurable expiry window and lets systems consume them when preconditions are met.
    /// </summary>
    public sealed class InputBuffer
    {
        private readonly Dictionary<string, float> _timers = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly List<string> _keysBuffer = new List<string>(8);

        /// <summary>Number of currently buffered (unexpired) inputs.</summary>
        public int Count => _timers.Count;

        /// <summary>
        /// Buffers an input identified by <paramref name="inputId"/> for <paramref name="windowSeconds"/>.
        /// If already buffered, refreshes the remaining time to at least <paramref name="windowSeconds"/>.
        /// </summary>
        public void Buffer(string inputId, float windowSeconds)
        {
            if (string.IsNullOrEmpty(inputId))
            {
                throw new ArgumentException("Input ID must not be null or empty.", nameof(inputId));
            }

            if (windowSeconds <= 0f)
            {
                return;
            }

            if (_timers.TryGetValue(inputId, out float existing))
            {
                if (windowSeconds > existing)
                {
                    _timers[inputId] = windowSeconds;
                }
            }
            else
            {
                _timers.Add(inputId, windowSeconds);
            }
        }

        /// <summary>
        /// Returns true if <paramref name="inputId"/> is currently buffered and unexpired.
        /// </summary>
        public bool HasBuffered(string inputId)
        {
            if (string.IsNullOrEmpty(inputId))
            {
                return false;
            }

            return _timers.TryGetValue(inputId, out float remaining) && remaining > 0f;
        }

        /// <summary>
        /// Returns the remaining buffer window in seconds for <paramref name="inputId"/>, or 0 if not buffered.
        /// </summary>
        public float GetRemaining(string inputId)
        {
            if (string.IsNullOrEmpty(inputId))
            {
                return 0f;
            }

            return _timers.TryGetValue(inputId, out float remaining) && remaining > 0f ? remaining : 0f;
        }

        /// <summary>
        /// Consumes a buffered input if present, returning true on success.
        /// </summary>
        public bool Consume(string inputId)
        {
            if (string.IsNullOrEmpty(inputId))
            {
                return false;
            }

            return _timers.Remove(inputId);
        }

        /// <summary>
        /// Advances all buffered timers by <paramref name="deltaTime"/> and evicts expired entries.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || _timers.Count == 0)
            {
                return;
            }

            _keysBuffer.Clear();
            foreach (string key in _timers.Keys)
            {
                _keysBuffer.Add(key);
            }

            for (int i = 0; i < _keysBuffer.Count; i++)
            {
                string key = _keysBuffer[i];
                float remaining = _timers[key] - deltaTime;
                if (remaining <= 0f)
                {
                    _timers.Remove(key);
                }
                else
                {
                    _timers[key] = remaining;
                }
            }
        }

        /// <summary>
        /// Clears all buffered inputs immediately.
        /// </summary>
        public void Clear()
        {
            _timers.Clear();
        }
    }
}

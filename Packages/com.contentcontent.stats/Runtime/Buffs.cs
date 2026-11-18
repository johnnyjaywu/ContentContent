using System;
using System.Collections.Generic;

namespace ContentContent.Stats
{
    /// <summary>
    /// Defines how re-applying an already-active buff behaves.
    /// </summary>
    public enum BuffStackingPolicy
    {
        /// <summary>Resets remaining duration to the definition's duration without adding stacks.</summary>
        RefreshDuration,

        /// <summary>Increases stack count up to <see cref="BuffDefinition{TStatId}.MaxStacks"/> and refreshes duration.</summary>
        StackValue,

        /// <summary>Ignores the new application if the buff is already active.</summary>
        IgnoreIfActive
    }

    /// <summary>
    /// Associates a target stat ID with a modifier template applied by a buff.
    /// </summary>
    public readonly struct BuffModifierEntry<TStatId>
    {
        public TStatId StatId { get; }
        public float Value { get; }
        public ModifierType Type { get; }

        public BuffModifierEntry(TStatId statId, float value, ModifierType type)
        {
            StatId = statId;
            Value = value;
            Type = type;
        }
    }

    /// <summary>
    /// Immutable blueprint describing a timed status effect / buff.
    /// </summary>
    public sealed class BuffDefinition<TStatId>
    {
        public string Id { get; }
        public float Duration { get; }
        public int MaxStacks { get; }
        public BuffStackingPolicy StackingPolicy { get; }
        public IReadOnlyList<BuffModifierEntry<TStatId>> Modifiers { get; }

        public BuffDefinition(
            string id,
            float duration,
            IReadOnlyList<BuffModifierEntry<TStatId>> modifiers,
            BuffStackingPolicy stackingPolicy = BuffStackingPolicy.RefreshDuration,
            int maxStacks = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Buff id cannot be null or empty.", nameof(id));
            if (duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");
            if (maxStacks < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStacks), "MaxStacks must be at least 1.");

            Id = id;
            Duration = duration;
            Modifiers = modifiers ?? Array.Empty<BuffModifierEntry<TStatId>>();
            StackingPolicy = stackingPolicy;
            MaxStacks = maxStacks;
        }
    }

    /// <summary>
    /// Runtime state for an active buff instance.
    /// </summary>
    public sealed class ActiveBuff<TStatId>
    {
        public BuffDefinition<TStatId> Definition { get; }
        public float RemainingDuration { get; internal set; }
        public int Stacks { get; internal set; }

        public bool IsPermanent => Definition.Duration <= 0f;

        internal ActiveBuff(BuffDefinition<TStatId> definition)
        {
            Definition = definition;
            RemainingDuration = definition.Duration;
            Stacks = 1;
        }
    }

    /// <summary>
    /// Manages timed buffs and automatically synchronizes their <see cref="StatModifier"/>s
    /// with a <see cref="StatCollection{TStatId}"/>.
    /// </summary>
    public class BuffController<TStatId>
    {
        private readonly StatCollection<TStatId> _stats;
        private readonly List<ActiveBuff<TStatId>> _activeBuffs = new List<ActiveBuff<TStatId>>();

        public IReadOnlyList<ActiveBuff<TStatId>> ActiveBuffs => _activeBuffs;

        public event Action<ActiveBuff<TStatId>> OnBuffApplied;
        public event Action<ActiveBuff<TStatId>> OnBuffRefreshed;
        public event Action<ActiveBuff<TStatId>> OnBuffExpired;

        public BuffController(StatCollection<TStatId> stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public ActiveBuff<TStatId> ApplyBuff(BuffDefinition<TStatId> definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ActiveBuff<TStatId> existing = FindBuff(definition.Id);
            if (existing != null)
            {
                switch (definition.StackingPolicy)
                {
                    case BuffStackingPolicy.IgnoreIfActive:
                        return existing;

                    case BuffStackingPolicy.RefreshDuration:
                        existing.RemainingDuration = definition.Duration;
                        OnBuffRefreshed?.Invoke(existing);
                        return existing;

                    case BuffStackingPolicy.StackValue:
                        existing.RemainingDuration = definition.Duration;
                        if (existing.Stacks < definition.MaxStacks)
                        {
                            existing.Stacks++;
                            ReapplyModifiers(existing);
                        }
                        OnBuffRefreshed?.Invoke(existing);
                        return existing;
                }
            }

            var active = new ActiveBuff<TStatId>(definition);
            _activeBuffs.Add(active);
            ReapplyModifiers(active);
            OnBuffApplied?.Invoke(active);
            return active;
        }

        public bool RemoveBuff(string buffId)
        {
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Definition.Id == buffId)
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool HasBuff(string buffId) => FindBuff(buffId) != null;

        /// <summary>
        /// Advances all timed buff durations by <paramref name="deltaTime"/> and expires finished buffs.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                ActiveBuff<TStatId> buff = _activeBuffs[i];
                if (buff.IsPermanent)
                    continue;

                buff.RemainingDuration -= deltaTime;
                if (buff.RemainingDuration <= 0f)
                {
                    RemoveAt(i);
                }
            }
        }

        public void ClearAll()
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }

        private ActiveBuff<TStatId> FindBuff(string buffId)
        {
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Definition.Id == buffId)
                    return _activeBuffs[i];
            }
            return null;
        }

        private void ReapplyModifiers(ActiveBuff<TStatId> buff)
        {
            _stats.RemoveAllModifiersFromSource(buff);
            var entries = buff.Definition.Modifiers;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var modifier = new StatModifier(entry.Value * buff.Stacks, entry.Type, source: buff);
                _stats.AddModifier(entry.StatId, modifier);
            }
        }

        private void RemoveAt(int index)
        {
            ActiveBuff<TStatId> buff = _activeBuffs[index];
            _activeBuffs.RemoveAt(index);
            _stats.RemoveAllModifiersFromSource(buff);
            OnBuffExpired?.Invoke(buff);
        }
    }
}

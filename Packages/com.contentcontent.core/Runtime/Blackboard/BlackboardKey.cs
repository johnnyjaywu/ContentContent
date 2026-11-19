using System;

namespace ContentContent.Core.Blackboards
{
    /// <summary>
    /// Strongly-typed, stateless key for <see cref="Blackboard"/> lookups with precomputed FNV-1a hash.
    /// Safe across Unity Domain Reloads without static registries.
    /// </summary>
    public readonly struct BlackboardKey<T> : IEquatable<BlackboardKey<T>>
    {
        /// <summary>Human-readable key name.</summary>
        public readonly string Name;

        /// <summary>Deterministic 32-bit FNV-1a hash of <see cref="Name"/>.</summary>
        public readonly int Hash;

        public BlackboardKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Blackboard key name cannot be null or whitespace.", nameof(name));

            Name = name;
            Hash = ComputeFnv1a(name);
        }

        public bool Equals(BlackboardKey<T> other)
        {
            return Hash == other.Hash && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is BlackboardKey<T> other && Equals(other);
        public override int GetHashCode() => Hash;
        public override string ToString() => $"{Name}<{typeof(T).Name}>";

        public static bool operator ==(BlackboardKey<T> left, BlackboardKey<T> right) => left.Equals(right);
        public static bool operator !=(BlackboardKey<T> left, BlackboardKey<T> right) => !left.Equals(right);

        internal static int ComputeFnv1a(string text)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;
                uint hash = offsetBasis;

                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= prime;
                }

                return (int)hash;
            }
        }
    }

    /// <summary>
    /// Convenience factory for creating <see cref="BlackboardKey{T}"/> instances.
    /// </summary>
    public static class BlackboardKey
    {
        public static BlackboardKey<T> Create<T>(string name) => new BlackboardKey<T>(name);
    }
}

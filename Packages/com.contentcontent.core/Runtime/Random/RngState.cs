using System;

namespace ContentContent.Core.Random
{
    /// <summary>
    /// Serializable 128-bit state snapshot for <see cref="Xoshiro128StarStar"/> enabling exact save/load and replay.
    /// </summary>
    [Serializable]
    public readonly struct RngState : IEquatable<RngState>
    {
        public readonly uint S0;
        public readonly uint S1;
        public readonly uint S2;
        public readonly uint S3;

        public bool IsAllZero => (S0 | S1 | S2 | S3) == 0u;

        public RngState(uint s0, uint s1, uint s2, uint s3)
        {
            S0 = s0;
            S1 = s1;
            S2 = s2;
            S3 = s3;
        }

        public bool Equals(RngState other)
        {
            return S0 == other.S0 && S1 == other.S1 && S2 == other.S2 && S3 == other.S3;
        }

        public override bool Equals(object obj) => obj is RngState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)S0;
                hash = (hash * 397) ^ (int)S1;
                hash = (hash * 397) ^ (int)S2;
                hash = (hash * 397) ^ (int)S3;
                return hash;
            }
        }

        public static bool operator ==(RngState left, RngState right) => left.Equals(right);
        public static bool operator !=(RngState left, RngState right) => !left.Equals(right);

        public override string ToString() => $"RngState({S0:X8}, {S1:X8}, {S2:X8}, {S3:X8})";
    }
}

using System;

namespace ContentContent.Core.Random
{
    /// <summary>
    /// Cross-platform deterministic Xoshiro128** pseudo-random number generator with
    /// SplitMix64 seeding, serializable <see cref="RngState"/> snapshots, and deterministic <see cref="Fork"/> streams.
    /// </summary>
    public sealed class Xoshiro128StarStar : IRandom
    {
        private uint _s0;
        private uint _s1;
        private uint _s2;
        private uint _s3;

        public Xoshiro128StarStar(ulong seed)
        {
            Seed(seed);
        }

        public Xoshiro128StarStar(int seed)
            : this(unchecked((ulong)seed))
        {
        }

        public Xoshiro128StarStar(RngState state)
        {
            SetState(state);
        }

        /// <summary>Re-seeds the generator deterministically using SplitMix64.</summary>
        public void Seed(ulong seed)
        {
            ulong state = seed;
            ulong a = SplitMix64Next(ref state);
            ulong b = SplitMix64Next(ref state);

            _s0 = (uint)a;
            _s1 = (uint)(a >> 32);
            _s2 = (uint)b;
            _s3 = (uint)(b >> 32);

            if ((_s0 | _s1 | _s2 | _s3) == 0u)
            {
                _s0 = 0x9E3779B9u;
            }
        }

        /// <summary>Captures the exact 128-bit internal state for persistence or replay.</summary>
        public RngState GetState() => new RngState(_s0, _s1, _s2, _s3);

        /// <summary>Restores the exact 128-bit internal state.</summary>
        public void SetState(RngState state)
        {
            if (state.IsAllZero)
                throw new ArgumentException("Xoshiro128** state cannot be all zeroes.", nameof(state));

            _s0 = state.S0;
            _s1 = state.S1;
            _s2 = state.S2;
            _s3 = state.S3;
        }

        /// <summary>
        /// Derives an independent deterministic child generator stream without correlating with subsequent draws.
        /// </summary>
        public Xoshiro128StarStar Fork()
        {
            ulong childSeed = ((ulong)NextUInt() << 32) | NextUInt();
            return new Xoshiro128StarStar(childSeed);
        }

        public uint NextUInt()
        {
            uint result = Rotl(_s1 * 5u, 7) * 9u;
            uint t = _s1 << 9;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;

            _s2 ^= t;
            _s3 = Rotl(_s3, 11);

            return result;
        }

        public int NextInt()
        {
            return (int)(NextUInt() & 0x7FFFFFFFu);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than 0.");

            return (int)NextUIntBounded((uint)maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            if (range <= uint.MaxValue)
            {
                return (int)(minInclusive + NextUIntBounded((uint)range));
            }

            // Full 32-bit signed span (range > uint.MaxValue only when minInclusive < 0 and maxExclusive > 0)
            while (true)
            {
                ulong sample = ((ulong)NextUInt() << 32) | NextUInt();
                ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
                if (sample < limit)
                {
                    return (int)((long)minInclusive + (long)(sample % range));
                }
            }
        }

        public float NextFloat()
        {
            // 24-bit mantissa precision in [0, 1)
            return (NextUInt() >> 8) * (1.0f / 16777216.0f);
        }

        public float NextFloat(float minInclusive, float maxExclusive)
        {
            if (minInclusive > maxExclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be >= minInclusive.");

            return minInclusive + (NextFloat() * (maxExclusive - minInclusive));
        }

        public double NextDouble()
        {
            // 53-bit mantissa precision in [0, 1)
            ulong bits = ((ulong)(NextUInt() >> 5) << 26) | (NextUInt() >> 6);
            return bits * (1.0 / 9007199254740992.0);
        }

        public bool NextBool(float probability = 0.5f)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        private uint NextUIntBounded(uint bound)
        {
            if (bound <= 1u)
                return 0u;

            uint threshold = unchecked((uint)-(int)bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                    return r % bound;
            }
        }

        private static uint Rotl(uint x, int k)
        {
            return (x << k) | (x >> (32 - k));
        }

        private static ulong SplitMix64Next(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}

namespace ContentContent.Core.Random
{
    /// <summary>
    /// Deterministic pseudo-random number generator contract.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns a uniformly distributed 32-bit unsigned integer.</summary>
        uint NextUInt();

        /// <summary>Returns a non-negative 32-bit signed integer in [0, int.MaxValue).</summary>
        int NextInt();

        /// <summary>Returns a 32-bit signed integer in [0, <paramref name="maxExclusive"/>).</summary>
        int NextInt(int maxExclusive);

        /// <summary>Returns a 32-bit signed integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Returns a single-precision float in [0, 1).</summary>
        float NextFloat();

        /// <summary>Returns a single-precision float in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        float NextFloat(float minInclusive, float maxExclusive);

        /// <summary>Returns a double-precision float in [0, 1).</summary>
        double NextDouble();

        /// <summary>Returns true with the given <paramref name="probability"/> in [0, 1].</summary>
        bool NextBool(float probability = 0.5f);
    }
}

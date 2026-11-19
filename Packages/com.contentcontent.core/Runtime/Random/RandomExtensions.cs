using System;
using System.Collections.Generic;

namespace ContentContent.Core.Random
{
    /// <summary>
    /// Common gameplay utility extensions for any <see cref="IRandom"/> implementation.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Shuffles <paramref name="list"/> in-place using the Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IRandom random, IList<T> list)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (list == null) throw new ArgumentNullException(nameof(list));

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>
        /// Picks a uniformly random element from <paramref name="list"/>.
        /// </summary>
        public static T Pick<T>(this IRandom random, IReadOnlyList<T> list)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot pick a random element from an empty list.");

            return list[random.NextInt(list.Count)];
        }

        /// <summary>
        /// Rolls <paramref name="count"/> dice with <paramref name="sides"/> faces (1..sides) and returns the sum.
        /// </summary>
        public static int RollDice(this IRandom random, int count, int sides)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Dice count must be greater than 0.");
            if (sides <= 0) throw new ArgumentOutOfRangeException(nameof(sides), "Dice sides must be greater than 0.");

            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += random.NextInt(1, sides + 1);
            }

            return total;
        }
    }
}

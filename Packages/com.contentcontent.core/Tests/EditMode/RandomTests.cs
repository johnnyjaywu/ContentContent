using System;
using System.Collections.Generic;
using ContentContent.Core.Random;
using NUnit.Framework;

namespace ContentContent.Core.Tests
{
    public class RandomTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var a = new Xoshiro128StarStar(123456UL);
            var b = new Xoshiro128StarStar(123456UL);

            for (int i = 0; i < 64; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
            }
        }

        [Test]
        public void StateSaveAndRestore_ReplaysExactSequence()
        {
            var rng = new Xoshiro128StarStar(98765UL);
            for (int i = 0; i < 10; i++) rng.NextUInt();

            RngState snapshot = rng.GetState();
            var expected = new uint[16];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = rng.NextUInt();
            }

            rng.SetState(snapshot);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], rng.NextUInt());
            }
        }

        [Test]
        public void Fork_ProducesDeterministicIndependentStream()
        {
            var parent1 = new Xoshiro128StarStar(42UL);
            var parent2 = new Xoshiro128StarStar(42UL);

            var child1 = parent1.Fork();
            var child2 = parent2.Fork();

            Assert.AreEqual(child1.NextUInt(), child2.NextUInt());
            Assert.AreNotEqual(parent1.NextUInt(), child1.NextUInt());
        }

        [Test]
        public void RangesAndHelpers_StayWithinBounds()
        {
            var rng = new Xoshiro128StarStar(777UL);

            for (int i = 0; i < 200; i++)
            {
                int v = rng.NextInt(-5, 10);
                Assert.GreaterOrEqual(v, -5);
                Assert.Less(v, 10);

                float f = rng.NextFloat(2.0f, 5.0f);
                Assert.GreaterOrEqual(f, 2.0f);
                Assert.Less(f, 5.0f);

                double d = rng.NextDouble();
                Assert.GreaterOrEqual(d, 0.0);
                Assert.Less(d, 1.0);
            }

            int dice = rng.RollDice(3, 6);
            Assert.GreaterOrEqual(dice, 3);
            Assert.LessOrEqual(dice, 18);

            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            rng.Shuffle(list);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, list);
        }

        [Test]
        public void WeightedTable_RespectsRelativeWeights()
        {
            var rng = new Xoshiro128StarStar(2026UL);
            var table = new WeightedTable<string>();
            table.Add("common", 80f);
            table.Add("rare", 20f);

            int commonCount = 0;
            int rareCount = 0;
            for (int i = 0; i < 2000; i++)
            {
                string pick = table.Pick(rng);
                if (pick == "common") commonCount++;
                else if (pick == "rare") rareCount++;
            }

            Assert.AreEqual(2000, commonCount + rareCount);
            Assert.Greater(commonCount, rareCount * 3);
            Assert.Greater(rareCount, 250);
        }
    }
}

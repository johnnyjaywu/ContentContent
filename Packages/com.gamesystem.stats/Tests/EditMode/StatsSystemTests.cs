using NUnit.Framework;

namespace GameSystem.Stats.Tests
{
    public class StatsSystemTests
    {
        [Test]
        public void Stat_AppliesFlatPercentAddAndPercentMultInOrder()
        {
            var attack = new Stat(100f);
            attack.AddModifier(new StatModifier(20f, ModifierType.Flat));         // 100 + 20 = 120
            attack.AddModifier(new StatModifier(0.25f, ModifierType.PercentAdd)); // +25%
            attack.AddModifier(new StatModifier(0.25f, ModifierType.PercentAdd)); // +25% => * 1.5 = 180
            attack.AddModifier(new StatModifier(0.5f, ModifierType.PercentMult)); // * 1.5 = 270

            Assert.AreEqual(270f, attack.Value, 0.001f);
        }

        [Test]
        public void Stat_ClampsAndRemovesBySource()
        {
            var source = new object();
            var speed = new Stat(10f, minValue: 0f, maxValue: 25f);

            speed.AddModifier(new StatModifier(50f, ModifierType.Flat, source));
            Assert.AreEqual(25f, speed.Value, 0.001f);

            int removed = speed.RemoveAllModifiersFromSource(source);
            Assert.AreEqual(1, removed);
            Assert.AreEqual(10f, speed.Value, 0.001f);
        }

        [Test]
        public void BuffController_AppliesStacksAndExpiresModifiers()
        {
            var stats = new StatCollection<string>();
            stats.RegisterStat("Attack", 10f);

            var buffs = new BuffController<string>(stats);
            var fury = new BuffDefinition<string>(
                id: "fury",
                duration: 5f,
                modifiers: new[] { new BuffModifierEntry<string>("Attack", 5f, ModifierType.Flat) },
                stackingPolicy: BuffStackingPolicy.StackValue,
                maxStacks: 3);

            buffs.ApplyBuff(fury);
            Assert.AreEqual(15f, stats.GetValue("Attack"), 0.001f);

            buffs.Tick(3f);
            buffs.ApplyBuff(fury); // 2 stacks, duration refreshed to 5s
            Assert.AreEqual(20f, stats.GetValue("Attack"), 0.001f);

            buffs.Tick(4f); // still 1s remaining
            Assert.IsTrue(buffs.HasBuff("fury"));

            buffs.Tick(1.5f); // expired
            Assert.IsFalse(buffs.HasBuff("fury"));
            Assert.AreEqual(10f, stats.GetValue("Attack"), 0.001f);
        }

        [Test]
        public void Health_DamageInvulnerabilityDeathHealAndRevive()
        {
            int deathCount = 0;
            var health = new Health(100f);
            health.OnDied += _ => deathCount++;

            health.IsInvulnerable = true;
            var blocked = health.ApplyDamage(new DamageInfo(50f));
            Assert.IsTrue(blocked.WasBlocked);
            Assert.AreEqual(100f, health.Current);

            health.IsInvulnerable = false;
            var hit = health.ApplyDamage(new DamageInfo(40f));
            Assert.AreEqual(40f, hit.AppliedDamage);
            Assert.AreEqual(60f, health.Current);

            float healed = health.Heal(100f);
            Assert.AreEqual(40f, healed);
            Assert.AreEqual(100f, health.Current);

            var lethal = health.ApplyDamage(new DamageInfo(150f));
            Assert.IsTrue(lethal.WasFatal);
            Assert.IsFalse(health.IsAlive);
            Assert.AreEqual(1, deathCount);

            // Further damage while dead does not re-trigger death
            health.ApplyDamage(new DamageInfo(10f));
            Assert.AreEqual(1, deathCount);

            health.Revive(50f);
            Assert.IsTrue(health.IsAlive);
            Assert.AreEqual(50f, health.Current);
        }

        [Test]
        public void Health_BoundMaxHealthStat_UpdatesProportionally()
        {
            var maxHpStat = new Stat(100f);
            using var health = new Health(100f, initialHealth: 50f);
            health.BindMaxHealthStat(maxHpStat, MaxHealthChangePolicy.KeepPercentage);

            maxHpStat.AddModifier(new StatModifier(100f, ModifierType.Flat));
            Assert.AreEqual(200f, health.Max, 0.001f);
            Assert.AreEqual(100f, health.Current, 0.001f);
        }
    }
}

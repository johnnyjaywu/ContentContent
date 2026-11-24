# Stats, Buffs & Health Quickstart

Give any character RPG attributes, stacking timed buffs, equipment bonuses, and dynamic health — completely code-free in the Inspector or directly in C#.

---

## 1. Install

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
```

> **Try the Sample:** In **Package Manager → ContentContent - Stats, Buffs & Health → Downloadable Samples**, download **Combat Stats**, click **Open Scene**, and press **Play**.

---

## 2. Create a Combat Entity (Zero Code)

1. Create a **Stat Sheet** asset via **Right-click → Create → ContentContent → Stats → Stat Sheet**, then click **Fill Starter RPG Stats** in the Inspector (`MaxHealth`, `Attack`, `Defense`, `MoveSpeed`).
2. Create a character via **GameObject → ContentContent → Stats → Combat Entity (Stats + Health)**.
3. Assign your `Stat Sheet` to **Initial Sheet** on **`StatsComponent`**.
4. **`HealthComponent`** automatically discovers the sibling `StatsComponent` and binds its max HP to `MaxHealth` — no bridge script required.
5. Expand **Events** on `HealthComponent` (`OnHealthChanged`, `OnDamaged`, `OnDied`, `OnRevived`) to drive UI health bars, hit flashes, or sound effects directly.

---

## 3. Author Timed & Stacking Buffs (Zero Code)

1. **Create → ContentContent → Stats → Buff** (for example `"BattleCry"`, `Duration = 8s`, `StackingPolicy = StackValue`, `MaxStacks = 3`, `+0.15 PercentAdd Attack`).
2. Trigger buffs in two code-free ways:
   - Add permanent or startup effects to **Starting Buffs** on `StatsComponent`.
   - Call `StatsComponent.ApplyBuff(BuffAsset)` directly from a pickup trigger, button, or `UnityEvent`.
3. Select the entity in **Play Mode** to watch live stat calculations, active buff timers, and HP test buttons (`Damage 10`, `Heal 10`, `Revive`).

---

## 4. Using C# Directly

```csharp
using ContentContent.Stats;

// Pure C# stats, buffs, and health (zero Unity dependencies)
var stats = new StatCollection<string>();
Stat maxHp = stats.RegisterStat("MaxHealth", baseValue: 100f, minValue: 1f);
Stat attack = stats.RegisterStat("Attack", baseValue: 20f, minValue: 0f);

var sword = new object();
stats.AddModifier("Attack", new StatModifier(10f, ModifierType.Flat, source: sword));
stats.AddModifier("Attack", new StatModifier(0.25f, ModifierType.PercentAdd, source: sword));
// (20 + 10) * 1.25 = 37.5

stats.RemoveAllModifiersFromSource(sword);

// Polymorphic combat damage through IDamageable
if (target.TryGetComponent<IDamageable>(out var damageable))
{
    damageable.ApplyDamage(new DamageInfo(25f, source: attacker, damageType: "Slash"));
}
```

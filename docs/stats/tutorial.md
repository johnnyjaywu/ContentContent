# Stats, Buffs & Health Tutorial

This walkthrough covers installing `com.contentcontent.stats`, trying the built-in sample, **pure C#** usage (ideal for simulation code and unit tests), and **Unity Inspector** setup with `StatSheetAsset`, `StatsComponent`, and `HealthComponent`.

---

## Step 1: Install the Package

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
```

If you are working inside the `ContentContent` repository directly, the package is already present at `Packages/com.contentcontent.stats`.

### Try the Built-In Sample

1. Ensure `com.contentcontent.core` is installed, open **Window → Package Manager**, select **ContentContent - Stats, Buffs & Health**, and click **Download** on **Combat Stats** under **Downloadable Samples**.
2. Attach `CombatStatsSample` to an empty `GameObject` (it automatically adds `StatsComponent` and `HealthComponent`) and press **Play** to test live stat modifiers, stacking timed buffs, armor-mitigated damage, healing, invulnerability, and revive.

---

## Part 1: Pure C# Quickstart

### 1. Create a `StatCollection` and Apply Modifiers

```csharp
using ContentContent.Stats;

public enum StatId { MaxHealth, Attack, MoveSpeed }

var stats = new StatCollection<StatId>();
var maxHp = stats.RegisterStat(StatId.MaxHealth, baseValue: 100f, minValue: 1f);
var attack = stats.RegisterStat(StatId.Attack, baseValue: 20f, minValue: 0f);
stats.RegisterStat(StatId.MoveSpeed, baseValue: 5f, minValue: 0f, maxValue: 15f);

// Equip a sword that grants +10 flat attack and +25% additive attack
var ironSword = new object();
stats.AddModifier(StatId.Attack, new StatModifier(10f, ModifierType.Flat, source: ironSword));
stats.AddModifier(StatId.Attack, new StatModifier(0.25f, ModifierType.PercentAdd, source: ironSword));

// (20 + 10) * (1 + 0.25) = 37.5
float currentAttack = attack.Value;

// Unequipping removes every modifier tagged with that source across all stats
stats.RemoveAllModifiersFromSource(ironSword);
```

### 2. Apply Timed & Stacking Buffs

```csharp
var buffs = new BuffController<StatId>(stats);

var battleCry = new BuffDefinition<StatId>(
    id: "battle_cry",
    duration: 8f,
    modifiers: new[]
    {
        new BuffModifierEntry<StatId>(StatId.Attack, 0.15f, ModifierType.PercentAdd),
        new BuffModifierEntry<StatId>(StatId.MoveSpeed, 1f, ModifierType.Flat)
    },
    stackingPolicy: BuffStackingPolicy.StackValue,
    maxStacks: 3);

buffs.OnBuffApplied += b => UnityEngine.Debug.Log($"Applied {b.Definition.Id}");
buffs.OnBuffExpired += b => UnityEngine.Debug.Log($"Expired {b.Definition.Id}");

buffs.ApplyBuff(battleCry); // 1 stack: +15% Attack, +1 MoveSpeed
buffs.ApplyBuff(battleCry); // 2 stacks: +30% Attack, +2 MoveSpeed, duration refreshed

// Advance timers each frame (modifiers are removed automatically on expiry)
buffs.Tick(deltaTime);
```

### 3. Bind `Health` to a `MaxHealth` Stat

```csharp
using var health = new Health(maxHp.Value);
health.BindMaxHealthStat(maxHp, MaxHealthChangePolicy.KeepPercentage);

health.OnDamaged += (info, result) =>
    UnityEngine.Debug.Log($"Took {result.AppliedDamage} {info.DamageType} damage (HP: {result.RemainingHealth})");
health.OnDied += info =>
    UnityEngine.Debug.Log($"Defeated by {info.Source}");

DamageResult hit = health.ApplyDamage(new DamageInfo(35f, source: "Goblin", damageType: "Slash"));
health.Heal(15f);

// Any buff or item that increases maxHp now scales Health.Max and preserves HP%
maxHp.AddModifier(new StatModifier(50f, ModifierType.Flat, source: "VitalityRing"));
```

---

## Part 2: Unity Inspector Setup

### 1. Author a `StatSheetAsset`

1. In the Project window, right-click and choose **Create → ContentContent → Stats → Stat Sheet**.
2. Add entries (e.g., `MaxHealth = 100` with `clampMin = true, minValue = 1`, `Attack = 20`, `MoveSpeed = 5`).

### 2. Attach `StatsComponent` and `HealthComponent`

1. Add **`StatsComponent`** to your character GameObject and assign your `StatSheetAsset` to **Initial Sheet**.
   - `StatsComponent` initializes `Stats` (`StatCollection<string>`) and `Buffs` (`BuffController<string>`) in `Awake()` and calls `Buffs.Tick(dt)` every `Update()`.
2. Add **`HealthComponent`** to the same GameObject and wire UI bars, hit flashes, or death animations to its Inspector `UnityEvent` hooks (`_onHealthChanged`, `_onDamaged`, `_onHealed`, `_onDied`, `_onRevived`).

### 3. Connect `StatsComponent` and `HealthComponent` in Code

```csharp
using ContentContent.Stats;
using ContentContent.Stats.Unity;
using UnityEngine;

[RequireComponent(typeof(StatsComponent), typeof(HealthComponent))]
public class CharacterVitalityBridge : MonoBehaviour
{
    private void Start()
    {
        var statsComp = GetComponent<StatsComponent>();
        var healthComp = GetComponent<HealthComponent>();

        if (statsComp.Stats.TryGetStat("MaxHealth", out Stat maxHpStat))
        {
            healthComp.Health.BindMaxHealthStat(maxHpStat, MaxHealthChangePolicy.KeepPercentage);
        }
    }
}
```

Any combat script can now deal damage polymorphically through `IDamageable`:

```csharp
if (other.TryGetComponent<IDamageable>(out var target))
{
    target.ApplyDamage(new DamageInfo(25f, source: gameObject, damageType: "Fire", isCritical: true));
}
```

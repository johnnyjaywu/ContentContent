# Stats, Buffs & Health Scripting API

## Namespace: `ContentContent.Stats`

### `ModifierType` *(enum)*

| Value | Default Order | Behavior |
| --- | --- | --- |
| `Flat` | `100` | Added directly to the running value (`+ value`). |
| `PercentAdd` | `200` | Consecutive entries are summed and applied once (`× (1 + sum)`). |
| `PercentMult` | `300` | Applied multiplicatively one after another (`× (1 + value)`). |

### `StatModifier` *(readonly struct, `IEquatable<StatModifier>`)*

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `StatModifier(float value, ModifierType type, object source = null)` | Creates a modifier using `(int)type` as `Order`. |
| Constructor | `StatModifier(float value, ModifierType type, int order, object source = null)` | Creates a modifier with an explicit evaluation `Order`. |
| `Value` | `float` | Numeric magnitude. |
| `Type` | `ModifierType` | Calculation mode. |
| `Order` | `int` | Sort key (ascending). |
| `Source` | `object` | Optional owner tag for bulk removal. |

### `Stat` *(class)*

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `Stat(float baseValue, float? minValue = null, float? maxValue = null)` | Initializes stat and optional clamps (`minValue <= maxValue`). |
| `BaseValue` | `float` *(get/set)* | Unmodified base value; setting marks dirty and notifies if final value changes. |
| `Value` | `float` *(get)* | Lazy-evaluated, clamped, 4-decimal rounded final value. |
| `MinValue` / `MaxValue` | `float?` | Optional clamp bounds. |
| `Modifiers` | `IReadOnlyList<StatModifier>` | Active modifiers in evaluation order. |
| `OnValueChanged` | `event Action<float, float>` | Fired when computed `Value` changes: `(oldValue, newValue)`. |
| `AddModifier` | `void AddModifier(StatModifier modifier)` | Inserts modifier by `Order`. |
| `RemoveModifier` | `bool RemoveModifier(StatModifier modifier)` | Removes first matching modifier. |
| `RemoveAllModifiersFromSource` | `int RemoveAllModifiersFromSource(object source)` | Removes all modifiers matching `source`; returns count removed. |
| `ClearModifiers` | `void ClearModifiers()` | Removes all modifiers. |

### `StatCollection<TStatId>` *(class)*

| Member | Signature / Type | Description |
| --- | --- | --- |
| `Stats` | `IReadOnlyDictionary<TStatId, Stat>` | Registered stats keyed by `TStatId`. |
| `RegisterStat` | `Stat RegisterStat(TStatId id, float baseValue, float? minValue = null, float? maxValue = null)` | Creates and registers (or replaces) a `Stat`. |
| `TryGetStat` | `bool TryGetStat(TStatId id, out Stat stat)` | Safely looks up a stat. |
| `GetStat` | `Stat GetStat(TStatId id)` | Returns the stat or throws `KeyNotFoundException`. |
| `GetValue` | `float GetValue(TStatId id, float fallback = 0f)` | Returns `stat.Value` or `fallback` if unregistered. |
| `AddModifier` | `bool AddModifier(TStatId id, StatModifier modifier)` | Adds a modifier to the target stat if registered. |
| `RemoveAllModifiersFromSource` | `int RemoveAllModifiersFromSource(object source)` | Removes all modifiers from `source` across every stat. |

### Buffs (`BuffStackingPolicy`, `BuffModifierEntry<TStatId>`, `BuffDefinition<TStatId>`, `ActiveBuff<TStatId>`, `BuffController<TStatId>`)

- **`BuffStackingPolicy`:** `RefreshDuration`, `StackValue`, `IgnoreIfActive`.
- **`BuffModifierEntry<TStatId>`:** `readonly struct` with `(TStatId statId, float value, ModifierType type)`.
- **`BuffDefinition<TStatId>`:** Immutable blueprint with `Id`, `Duration` (`0` = permanent), `Modifiers`, `StackingPolicy`, and `MaxStacks`.
- **`ActiveBuff<TStatId>`:** Runtime instance exposing `Definition`, `RemainingDuration`, `Stacks`, and `IsPermanent`.
- **`BuffController<TStatId>`:**

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `BuffController(StatCollection<TStatId> stats)` | Creates a controller bound to `stats`. |
| `ActiveBuffs` | `IReadOnlyList<ActiveBuff<TStatId>>` | Currently active buffs. |
| `OnBuffApplied` | `event Action<ActiveBuff<TStatId>>` | Fired when a new buff is added. |
| `OnBuffRefreshed` | `event Action<ActiveBuff<TStatId>>` | Fired when an active buff is refreshed or stacked. |
| `OnBuffExpired` | `event Action<ActiveBuff<TStatId>>` | Fired when a buff expires or is removed. |
| `ApplyBuff` | `ActiveBuff<TStatId> ApplyBuff(BuffDefinition<TStatId> definition)` | Applies, refreshes, stacks, or ignores according to `StackingPolicy`. |
| `RemoveBuff` | `bool RemoveBuff(string buffId)` | Removes buff by ID and cleans up its stat modifiers. |
| `HasBuff` | `bool HasBuff(string buffId)` | Returns whether a buff with `buffId` is active. |
| `Tick` | `void Tick(float deltaTime)` | Advances non-permanent buff timers and expires finished buffs. |
| `ClearAll` | `void ClearAll()` | Removes all active buffs and their modifiers. |

### Health & Damage (`DamageInfo`, `DamageResult`, `IDamageable`, `MaxHealthChangePolicy`, `Health`)

- **`DamageInfo`:** `readonly struct` (`Amount`, `Source`, `DamageType`, `IsCritical`).
- **`DamageResult`:** `readonly struct` (`RequestedDamage`, `AppliedDamage`, `RemainingHealth`, `WasFatal`, `WasBlocked`).
- **`IDamageable`:** `DamageResult ApplyDamage(DamageInfo damage)`.
- **`MaxHealthChangePolicy`:** `KeepCurrent`, `KeepPercentage`, `AddDelta`.
- **`Health` (`IDamageable`, `IDisposable`):**

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `Health(float maxHealth, float? initialHealth = null)` | Initializes health (`maxHealth > 0`, defaults to full). |
| `Current` / `Max` / `Normalized` | `float` | Current HP, max HP, and `Current / Max` ratio (`0..1`). |
| `IsAlive` | `bool` | `true` while `Current > 0`. |
| `IsInvulnerable` | `bool` *(get/set)* | When `true`, `ApplyDamage` returns `WasBlocked = true` with `0` applied damage. |
| `OnHealthChanged` | `event Action<float, float>` | Fired on current/max changes: `(current, max)`. |
| `OnDamaged` | `event Action<DamageInfo, DamageResult>` | Fired on damage attempts while alive (including blocked hits). |
| `OnHealed` | `event Action<float>` | Fired with actual healed amount when `> 0`. |
| `OnDied` | `event Action<DamageInfo>` | Fired when a hit reduces `Current` to `0`. |
| `OnRevived` | `event Action` | Fired when `Revive` restores a dead entity. |
| `BindMaxHealthStat` | `void BindMaxHealthStat(Stat maxHealthStat, MaxHealthChangePolicy policy = MaxHealthChangePolicy.KeepPercentage)` | Subscribes to `maxHealthStat.OnValueChanged` and syncs `Max`. |
| `UnbindMaxHealthStat` | `void UnbindMaxHealthStat()` | Unsubscribes from any bound max-health stat. |
| `SetMaxHealth` | `void SetMaxHealth(float newMax, MaxHealthChangePolicy policy = MaxHealthChangePolicy.KeepCurrent)` | Updates `Max` (clamped to `>= 1`) using `policy`. |
| `ApplyDamage` | `DamageResult ApplyDamage(DamageInfo damage)` | Applies incoming damage and raises lifecycle events. |
| `Heal` | `float Heal(float amount)` | Heals up to `Max` while alive; returns actual amount healed. |
| `Revive` | `void Revive(float? healthAmount = null)` | Revives if dead, restoring `healthAmount` (clamped `[1, Max]`) or `Max`. |
| `Dispose` | `void Dispose()` | Unbinds any subscribed `Stat`. |

---

## Namespace: `ContentContent.Stats.Unity`

### `StatSheetAsset` *(ScriptableObject)*
- Create via **Create → ContentContent → Stats → Stat Sheet**.
- **`Entries`** (`IReadOnlyList<Entry>`): Serialized list of `{ statId, baseValue, clampMin, minValue, clampMax, maxValue }`.
- **`Populate(StatCollection<string> collection)`**: Registers all valid entries into `collection`.

### `StatsComponent` *(MonoBehaviour)*
- Hosts **`Stats`** (`StatCollection<string>`) and **`Buffs`** (`BuffController<string>`).
- Populates `Stats` from `_initialSheet` in `Awake()` and calls `Buffs.Tick(dt)` in `Update()` (respecting `_useUnscaledTime`).

### `HealthComponent` *(MonoBehaviour, `IDamageable`)*
- Wraps **`Health`** with Inspector-configured `_maxHealth`, `_startAtMaxHealth`, `_initialHealth`, and `UnityEvent` hooks (`_onHealthChanged`, `_onDamaged`, `_onHealed`, `_onDied`, `_onRevived`).
- Exposes **`Current`**, **`Max`**, **`IsAlive`**, **`ApplyDamage(DamageInfo)`**, **`Heal(float)`**, and **`Revive(float?)`**.
- Automatically unsubscribes and calls `Health.Dispose()` in `OnDestroy()`.

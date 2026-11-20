# Combat Stats Sample

Drop-in RPG combat sheet showcasing `com.contentcontent.stats`:

- **`StatsComponent` & `StatCollection<string>`** — Dirty-cached `MaxHealth`, `Attack`, and `Armor` stats with `Flat`, `PercentAdd`, and `PercentMult` modifiers.
- **`BuffController<string>`** — Timed buffs with `RefreshDuration` (`BattleShout`, `Vitality`) and `StackValue` (`Frenzy`, up to 5 stacks).
- **`HealthComponent` & `Health`** — Live `BindMaxHealthStat` (`KeepPercentage`), armor-mitigated damage, healing, invulnerability toggling, and death/revive lifecycle events.

## Setup

1. Create an empty `GameObject` in any scene.
2. Attach `CombatStatsSample` (it automatically adds `StatsComponent` and `HealthComponent`).
3. Press **Play** and interact via the on-screen HUD.

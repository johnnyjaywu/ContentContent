# Combat Stats Sample

Drop-in RPG combat sheet showcasing `com.contentcontent.stats`:

- **`StatsComponent` & `StatCollection<string>`** — Dirty-cached `MaxHealth`, `Attack`, and `Armor` stats with `Flat`, `PercentAdd`, and `PercentMult` modifiers.
- **`BuffController<string>`** — Timed buffs with `RefreshDuration` (`BattleShout`, `Vitality`) and `StackValue` (`Frenzy`, up to 5 stacks).
- **`HealthComponent` & `Health`** — Live `BindMaxHealthStat` (`KeepPercentage`), armor-mitigated damage, healing, invulnerability toggling, and death/revive lifecycle events.

## Setup

1. Download **Combat Stats** from **Window → Package Manager → ContentContent - Stats, Buffs & Health → Downloadable Samples** (with `com.contentcontent.core` installed).
2. Click **Open Scene** (or open `CombatStats.unity`) and press **Play** to interact via the on-screen HUD.

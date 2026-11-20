using System.Collections.Generic;
using ContentContent.Stats.Unity;
using UnityEngine;

namespace ContentContent.Stats.Samples
{
    /// <summary>
    /// Drop-in interactive RPG combat stats demo using <see cref="StatsComponent"/> and <see cref="HealthComponent"/>.
    /// Attach to any empty GameObject and press Play.
    /// </summary>
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(HealthComponent))]
    public class CombatStatsSample : MonoBehaviour
    {
        private StatsComponent _statsComponent;
        private HealthComponent _healthComponent;

        private BuffDefinition<string> _battleShoutBuff;
        private BuffDefinition<string> _frenzyStackBuff;
        private BuffDefinition<string> _vitalityBuff;

        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            _statsComponent = GetComponent<StatsComponent>();
            _healthComponent = GetComponent<HealthComponent>();

            var stats = _statsComponent.Stats;
            Stat maxHp = stats.RegisterStat("MaxHealth", 100f);
            stats.RegisterStat("Attack", 20f);
            stats.RegisterStat("Armor", 10f);

            // Bind HealthComponent's Max Health directly to the MaxHealth stat
            _healthComponent.Health.BindMaxHealthStat(maxHp, MaxHealthChangePolicy.KeepPercentage);

            // Subscribe to lifecycle events
            _healthComponent.Health.OnDamaged += (info, res) =>
            {
                string status = res.WasBlocked ? "BLOCKED" : $"{res.AppliedDamage:0.#} dmg";
                AppendLog($"[Health] {status} ({info.DamageType ?? "Physical"}) -> HP {res.RemainingHealth:0.#}/{_healthComponent.Max:0.#}");
            };
            _healthComponent.Health.OnHealed += amt => AppendLog($"[Health] Healed +{amt:0.#}");
            _healthComponent.Health.OnDied += _ => AppendLog("[Health] Character DIED!");
            _healthComponent.Health.OnRevived += () => AppendLog("[Health] Character REVIVED!");

            _statsComponent.Buffs.OnBuffApplied += b => AppendLog($"[Buff] Applied '{b.Definition.Id}'");
            _statsComponent.Buffs.OnBuffRefreshed += b => AppendLog($"[Buff] Refreshed '{b.Definition.Id}' (x{b.Stacks})");
            _statsComponent.Buffs.OnBuffExpired += b => AppendLog($"[Buff] Expired '{b.Definition.Id}'");

            // Define reusable buffs
            _battleShoutBuff = new BuffDefinition<string>(
                id: "BattleShout",
                duration: 6f,
                modifiers: new[]
                {
                    new BuffModifierEntry<string>("Attack", 10f, ModifierType.Flat),
                    new BuffModifierEntry<string>("Attack", 0.25f, ModifierType.PercentAdd)
                },
                stackingPolicy: BuffStackingPolicy.RefreshDuration);

            _frenzyStackBuff = new BuffDefinition<string>(
                id: "Frenzy",
                duration: 8f,
                modifiers: new[]
                {
                    new BuffModifierEntry<string>("Attack", 0.15f, ModifierType.PercentMult)
                },
                stackingPolicy: BuffStackingPolicy.StackValue,
                maxStacks: 5);

            _vitalityBuff = new BuffDefinition<string>(
                id: "Vitality",
                duration: 10f,
                modifiers: new[]
                {
                    new BuffModifierEntry<string>("MaxHealth", 50f, ModifierType.Flat),
                    new BuffModifierEntry<string>("Armor", 15f, ModifierType.Flat)
                },
                stackingPolicy: BuffStackingPolicy.RefreshDuration);

            AppendLog("Combat Stats Sample ready.");
        }

        private void AppendLog(string entry)
        {
            _log.Add(entry);
            if (_log.Count > 9)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (_statsComponent == null || _statsComponent.Stats == null || _healthComponent == null || _healthComponent.Health == null)
                return;

            var stats = _statsComponent.Stats;
            var buffs = _statsComponent.Buffs;
            var health = _healthComponent.Health;

            GUILayout.BeginArea(new Rect(16, 16, 420, 560), "Stats, Buffs & Health Sample", GUI.skin.window);

            GUILayout.Label($"Health: {health.Current:0.#} / {health.Max:0.#} ({health.Normalized * 100f:0}%)  |  {(health.IsAlive ? "ALIVE" : "DEAD")}  |  Invuln: {health.IsInvulnerable}");
            GUILayout.Label($"Attack: {stats.GetValue("Attack"):0.#}  |  Armor: {stats.GetValue("Armor"):0.#}  |  MaxHealth Stat: {stats.GetValue("MaxHealth"):0.#}");

            GUILayout.Space(6);
            GUILayout.Label("Health Actions:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take 30 Raw Dmg"))
            {
                float armor = stats.GetValue("Armor");
                float mitigated = Mathf.Max(1f, 30f * (100f / (100f + armor)));
                health.ApplyDamage(new DamageInfo(mitigated, source: this, damageType: "Slash"));
            }
            if (GUILayout.Button("Heal +25")) health.Heal(25f);
            if (GUILayout.Button("Toggle Invuln")) health.IsInvulnerable = !health.IsInvulnerable;
            if (GUILayout.Button("Revive")) health.Revive();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Timed Buffs:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Battle Shout (6s)")) buffs.ApplyBuff(_battleShoutBuff);
            if (GUILayout.Button("Stack Frenzy (8s)")) buffs.ApplyBuff(_frenzyStackBuff);
            if (GUILayout.Button("Vitality +50 HP (10s)")) buffs.ApplyBuff(_vitalityBuff);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Buffs"))
            {
                buffs.ClearAll();
            }

            GUILayout.Space(6);
            GUILayout.Label($"Active Buffs ({buffs.ActiveBuffs.Count}):");
            for (int i = 0; i < buffs.ActiveBuffs.Count; i++)
            {
                var b = buffs.ActiveBuffs[i];
                GUILayout.Label($"  - {b.Definition.Id} x{b.Stacks} ({b.RemainingDuration:0.0}s left)");
            }

            GUILayout.Space(6);
            GUILayout.Label("Combat Log:");
            for (int i = 0; i < _log.Count; i++)
            {
                GUILayout.Label(_log[i]);
            }

            GUILayout.EndArea();
        }
    }
}

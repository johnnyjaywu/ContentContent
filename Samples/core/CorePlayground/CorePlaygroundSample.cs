using System;
using System.Collections.Generic;
using ContentContent.Core.Blackboards;
using ContentContent.Core.Commands;
using ContentContent.Core.Events;
using ContentContent.Core.Fsm;
using ContentContent.Core.Grid;
using ContentContent.Core.Pooling;
using ContentContent.Core.Random;
using ContentContent.Core.Timers;
using UnityEngine;

namespace ContentContent.Core.Samples
{
    /// <summary>
    /// Drop-in interactive playground demonstrating Core systems:
    /// ObjectPool, HFSM, EventBus, TimerScheduler, Cooldown, Deterministic RNG / WeightedTable,
    /// CommandHistory (Undo/Redo), Blackboard, and Grid2D.
    /// Attach to any empty GameObject and press Play.
    /// </summary>
    public class CorePlaygroundSample : MonoBehaviour
    {
        private readonly struct ScoreEvent
        {
            public readonly int Delta;
            public readonly int Total;

            public ScoreEvent(int delta, int total)
            {
                Delta = delta;
                Total = total;
            }
        }

        private sealed class PooledToken : IPoolable
        {
            public int SpawnCount { get; private set; }
            public void OnSpawned() => SpawnCount++;
            public void OnDespawned() { }
        }

        private static readonly BlackboardKey<bool> AlertKey = BlackboardKey.Create<bool>("IsAlert");
        private static readonly BlackboardKey<int> ScoreKey = BlackboardKey.Create<int>("Score");

        private ObjectPool<PooledToken> _pool;
        private readonly Stack<PooledToken> _leasedTokens = new Stack<PooledToken>();
        private StateMachine _fsm;
        private EventBus _eventBus;
        private EventSubscription _scoreSub;
        private TimerScheduler _timers;
        private Cooldown _abilityCooldown;
        private Xoshiro128StarStar _rng;
        private WeightedTable<string> _lootTable;
        private CommandHistory _history;
        private Blackboard _blackboard;
        private Grid2D<int> _grid;

        private readonly List<string> _log = new List<string>();
        private string _lastLoot = "(none)";
        private int _score;

        private void Awake()
        {
            // 1. ObjectPool
            _pool = new ObjectPool<PooledToken>(() => new PooledToken(), defaultCapacity: 4, maxSize: 16);
            _pool.Prewarm(4);

            // 2. EventBus
            _eventBus = new EventBus();
            _scoreSub = _eventBus.Subscribe<ScoreEvent>((in ScoreEvent e) =>
            {
                AppendLog($"[EventBus] Score {(e.Delta >= 0 ? "+" : "")}{e.Delta} -> {e.Total}");
            });

            // 3. Blackboard + HFSM
            _blackboard = new Blackboard();
            _blackboard.Set(AlertKey, false);
            _blackboard.Set(ScoreKey, 0);

            var patrol = new ActionState("Patrol", onEnter: () => AppendLog("[HFSM] Entered Patrol"));
            var alert = new ActionState("Alert", onEnter: () => AppendLog("[HFSM] Entered Alert"));
            _fsm = new StateMachine(patrol);
            _fsm.AddTransitionWhen(patrol, alert, _blackboard, AlertKey, isAlert => isAlert);
            _fsm.AddTransitionWhen(alert, patrol, _blackboard, AlertKey, isAlert => !isAlert);
            _fsm.OnEnter();

            // 4. Timers & Cooldown
            _timers = new TimerScheduler();
            _abilityCooldown = new Cooldown(2.5f, startReady: true);

            // 5. Deterministic RNG & WeightedTable
            _rng = new Xoshiro128StarStar(12345UL);
            _lootTable = new WeightedTable<string>();
            _lootTable.Add("Common Potion", 60f);
            _lootTable.Add("Rare Gem", 30f);
            _lootTable.Add("Legendary Relic", 10f);

            // 6. CommandHistory
            _history = new CommandHistory(maxCapacity: 32);

            // 7. Grid2D
            _grid = new Grid2D<int>(6, 6, _ => 0);

            AppendLog("Core Playground ready.");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _timers.Tick(dt);
            _abilityCooldown.Tick(dt);
            _fsm.OnTick(dt);
        }

        private void OnDestroy()
        {
            _scoreSub.Dispose();
            _eventBus?.Clear();
            _pool?.Dispose();
            _blackboard?.ClearAll();
        }

        private void ExecuteAddScore(int amount)
        {
            int delta = amount;
            var cmd = new ActionCommand(
                execute: () =>
                {
                    _score += delta;
                    _blackboard.Set(ScoreKey, _score);
                    _eventBus.Publish(new ScoreEvent(delta, _score));
                },
                undo: () =>
                {
                    _score -= delta;
                    _blackboard.Set(ScoreKey, _score);
                    _eventBus.Publish(new ScoreEvent(-delta, _score));
                });
            _history.Execute(cmd);
        }

        private void AppendLog(string message)
        {
            _log.Add(message);
            if (_log.Count > 10)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 420, 560), "Core Playground Sample", GUI.skin.window);

            GUILayout.Label($"HFSM State: {_fsm.CurrentState}  |  Blackboard IsAlert: {_blackboard.Get(AlertKey)}");
            if (GUILayout.Button("Toggle Blackboard Alert (Drives HFSM)"))
            {
                _blackboard.Set(AlertKey, !_blackboard.Get(AlertKey));
            }

            GUILayout.Space(6);
            GUILayout.Label($"ObjectPool: Active={_pool.CountActive}, Inactive={_pool.CountInactive}, Total={_pool.CountAll}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pool Get"))
            {
                var token = _pool.Get();
                _leasedTokens.Push(token);
                AppendLog($"[Pool] Spawned token (reused {token.SpawnCount}x)");
            }
            if (GUILayout.Button("Pool Release") && _leasedTokens.Count > 0)
            {
                _pool.Release(_leasedTokens.Pop());
                AppendLog("[Pool] Released token");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Score (Commands + EventBus): {_score}  |  Undo={_history.UndoCount}, Redo={_history.RedoCount}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 Score")) ExecuteAddScore(10);
            if (GUILayout.Button("Undo") && _history.CanUndo) _history.Undo();
            if (GUILayout.Button("Redo") && _history.CanRedo) _history.Redo();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Cooldown: {(_abilityCooldown.IsReady ? "READY" : $"{_abilityCooldown.Remaining:0.0}s")}  |  Active Timers: {_timers.ActiveCount}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Cooldown (2.5s)"))
            {
                if (_abilityCooldown.TryConsume())
                {
                    AppendLog("[Cooldown] Ability fired!");
                }
                else
                {
                    AppendLog("[Cooldown] Still cooling down...");
                }
            }
            if (GUILayout.Button("Schedule 1.5s Timer"))
            {
                _timers.After(1.5f, () => AppendLog("[Timer] 1.5s delayed callback fired!"));
                AppendLog("[Timer] Scheduled 1.5s timer");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Deterministic RNG Loot: {_lastLoot}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Roll Weighted Loot"))
            {
                _lastLoot = _lootTable.Pick(_rng);
                int x = _rng.NextInt(0, _grid.Width);
                int y = _rng.NextInt(0, _grid.Height);
                _grid[x, y] += 1;
                AppendLog($"[RNG] Rolled '{_lastLoot}' & incremented Grid[{x},{y}]={_grid[x, y]}");
            }
            if (GUILayout.Button("Reseed RNG (12345)"))
            {
                _rng.Seed(12345UL);
                AppendLog("[RNG] Reseeded to 12345");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Event Log:");
            for (int i = 0; i < _log.Count; i++)
            {
                GUILayout.Label(_log[i]);
            }

            GUILayout.EndArea();
        }
    }
}

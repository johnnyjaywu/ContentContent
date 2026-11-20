# World Tutorial

This guide covers installing `com.contentcontent.world`, running edit-time soft-lock checks on your Metroidvania map, setting up Unity room portals, benches, hazards, and `IResettable` enemies, and trying the built-in sample.

---

## Step 1: Install the Package

`com.contentcontent.world` depends on `com.contentcontent.core`. Install via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.world
```

### Try the Built-In Sample

1. Ensure `com.contentcontent.core` is installed, open **Window → Package Manager**, select **ContentContent - World**, and click **Download** on **World Playground** under **Downloadable Samples**.
2. Attach `WorldPlaygroundSample` to an empty `GameObject` and press **Play**.

---

## 1. Validating Your Room Graph in Pure C# Tests

Model your world connectivity and validate reachability in NUnit without launching Unity:

```csharp
using ContentContent.World;
using NUnit.Framework;

var graph = new WorldGraph();
graph.AddRoom("Town", hasCheckpoint: true);
graph.AddRoom("Caverns");
graph.AddRoom("Shrine").GrantAbility("WallJump");
graph.AddRoom("BossArena");

graph.AddBidirectionalConnection("Town", "Caverns");
graph.AddBidirectionalConnection("Caverns", "Shrine");
graph.AddExit("Caverns", new RoomExit("ToBoss", "BossArena", requiredAbilities: new[] { "WallJump" }));

Assert.IsTrue(WorldReachabilityValidator.CanReach(graph, "Town", "BossArena"));
Assert.AreEqual(0, WorldReachabilityValidator.FindUnreachableRooms(graph, "Town").Count);
Assert.AreEqual(0, WorldReachabilityValidator.FindSoftLockRooms(graph, "Town").Count);
```

---

## 2. Setting Up Rooms, Benches & Hazards in Unity

1. Place **`WorldManagerBehaviour`** on a persistent scene manager and assign your Player `Transform` and **Initial Room Id**.
2. Add **`RoomSpawnPoint`** objects at each doorway/bench (`RoomId` + `SpawnPointId`).
3. Add **`RoomTransitionTrigger2D`** portals at room edges, **`HazardZone2D`** triggers on spikes/pits, and **`CheckpointBehaviour2D`** triggers on benches.
4. For interactable benches using Unity's New Input System (`com.unity.inputsystem`):

```csharp
using ContentContent.World.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

public class BenchInteractBridge : MonoBehaviour
{
    [SerializeField] private InputActionReference _interactAction;
    private CheckpointBehaviour2D _nearbyBench;

    private void OnEnable() => _interactAction?.action.Enable();
    private void OnDisable() => _interactAction?.action.Disable();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out CheckpointBehaviour2D bench))
            _nearbyBench = bench;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out CheckpointBehaviour2D bench) && bench == _nearbyBench)
            _nearbyBench = null;
    }

    private void Update()
    {
        if (_nearbyBench != null && _interactAction != null && _interactAction.action.WasPressedThisFrame())
        {
            _nearbyBench.Activate();
        }
    }
}
```

5. Extend **`ResettableBehaviour`** for enemies, breakable walls, or switches that should reset automatically when entering a room or respawning at a bench:

```csharp
using ContentContent.World.Unity;

public class RespawningCrawler : ResettableBehaviour
{
    private UnityEngine.Vector3 _spawnPos;

    private void Awake() => _spawnPos = transform.position;

    public override void ResetState()
    {
        transform.position = _spawnPos;
        gameObject.SetActive(true);
    }
}
```

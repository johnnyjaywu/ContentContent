# Navigation Quickstart

Set up 2D (`XY` or ground `XZ`) and volumetric 3D pathfinding in under a minute — using scene menus, automatic collider scanning (zero baking step), drop-in `NavPatrol` and `NavFollowTarget` behaviors, and live PlayMode telemetry.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.navigation`.
2. Open **Window → Package Manager**, select **ContentContent - Navigation**, and click **Download** on **Navigation Playground** under **Downloadable Samples**.
3. Click **Open Scene** and press **Play** to watch agents navigate around static walls and dynamic doorway obstacles.

---

## 2. Create a Surface & Patrolling Agent (Code-Free)

1. In the Hierarchy menu, click **GameObject → ContentContent → Navigation → Nav Surface (2D XY)** (or **2D XZ** / **Volumetric 3D**).
   - Set **Size** around your level bounds, **Cell Size** to match your character radius, and **Obstacle Mask** to your wall layers (the Inspector highlights if it is set to `Nothing` with a 1-click **Use Default Layer** fix).
   - No manual bake step is required — `NavSurface` scans automatically on `Awake`, and you can preview the scanned cells right inside EditMode via **Scan Surface Now**.
2. Click **GameObject → ContentContent → Navigation → Patrolling Agent** to create an agent pre-wired with **`NavAgent`**, **`NavPatrol`**, and two scene waypoints (`Waypoint_0`, `Waypoint_1`).
3. Move the waypoints in the Scene view, choose **`Loop`** or **`PingPong`** on `NavPatrol`, and press **Play**!
4. Prefer chasing a moving player? Add **`NavFollowTarget`** to any `NavAgent` and drag your player `Transform` into **Target**.

---

## 3. Add Dynamic Obstacles

Attach **`NavObstacle`** to any moving door, crate, or destructible wall:
- When enabled, disabled, or moved, `NavObstacle` refreshes only the `NavSurface` grid cells inside its bounds.
- Active `NavAgent`s detect `Grid.Version` changes and automatically repath cleanly around the obstacle.

---

## 4. Drive `NavAgent` From Code (Full Control)

```csharp
using ContentContent.Navigation.Unity;
using UnityEngine;

public class ClickToMove : MonoBehaviour
{
    [SerializeField] private NavAgent _agent;

    public void MoveTo(Vector3 targetWorldPosition)
    {
        _agent.SetDestination(targetWorldPosition);
    }
}
```

Or use pure C# (`NavGrid`, `NavPathfinder`, `NavPathQueue`) directly without `UnityEngine` — see [Scripting API](scripting-api.md).

---

## Next Steps

- Learn *why* compact voxel/grid pathfinding complements NavMeshes and how the pattern transfers to Godot/Unreal in [Design & Best Practices](design.md).
- Browse every C# class and Unity component in [Scripting API](scripting-api.md).

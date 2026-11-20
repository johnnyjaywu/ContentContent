# Navigation Playground Sample

Drop-in interactive navigation sandbox showcasing `com.contentcontent.navigation`:

- **`NavSurface`** — Automatic obstacle scanning on a 20×20 `PlanarXZ` grid with `Grid.Version` tracking.
- **`NavAgent`** — Budgeted asynchronous pathfinding, waypoint following, and automatic repathing when the surface changes.
- **Dynamic Obstacles** — Toggle the central doorway gate at runtime to watch `NavAgent` immediately repath around or through the opening.

## Setup

1. Create an empty `GameObject` in any scene (ensure a Camera is positioned to look at the origin, e.g. `(0, 18, -14)` looking at `(0, 0, 0)`).
2. Attach `NavPlaygroundSample`.
3. Press **Play** and use the on-screen HUD to toggle the gate and assign destinations.

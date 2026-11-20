# Navigation Playground Sample

Drop-in interactive navigation sandbox showcasing `com.contentcontent.navigation`:

- **`NavSurface`** — Automatic obstacle scanning on a 20×20 `PlanarXZ` grid with `Grid.Version` tracking.
- **`NavAgent`** — Budgeted asynchronous pathfinding, waypoint following, and automatic repathing when the surface changes.
- **Dynamic Obstacles** — Toggle the central doorway gate at runtime to watch `NavAgent` immediately repath around or through the opening.

## Setup

1. Download **Navigation Playground** from **Window → Package Manager → ContentContent - Navigation → Downloadable Samples** (with `com.contentcontent.core` installed).
2. Click **Open Scene** (or open `NavPlayground.unity`) and press **Play** to use the on-screen HUD, toggle the gate, and assign destinations.

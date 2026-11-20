using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation.Unity;
using UnityEngine;

namespace ContentContent.Navigation.Samples
{
    /// <summary>
    /// Drop-in interactive navigation sandbox demonstrating <see cref="NavSurface"/>,
    /// <see cref="NavAgent"/>, dynamic obstacles, and line-of-sight smoothed pathfinding.
    /// Attach to any empty GameObject and press Play.
    /// </summary>
    public class NavPlaygroundSample : MonoBehaviour
    {
        [SerializeField] private float _agentSpeed = 6f;

        private NavSurface _surface;
        private NavAgent _agent;
        private GameObject _agentVisual;
        private GameObject _goalMarker;
        private GameObject _dynamicGate;
        private bool _gateClosed = true;

        private readonly List<Vector3> _previewPath = new List<Vector3>(32);
        private string _status = "Ready";

        private void Start()
        {
            // 1. Create NavSurface (default PlanarXZ, 20x20, cellSize 1)
            var surfaceGo = new GameObject("SampleNavSurface");
            surfaceGo.transform.SetParent(transform, false);
            _surface = surfaceGo.AddComponent<NavSurface>();

            // 2. Build visual floor (non-colliding so it does not block the surface scan)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor";
            floor.transform.SetParent(transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(20f, 20f, 1f);
            Destroy(floor.GetComponent<Collider>());

            // 3. Build static divider wall with a central doorway
            CreateWall("Wall_Left", new Vector3(-5.5f, 0f, 0f), new Vector3(7f, 1.2f, 1f));
            CreateWall("Wall_Right", new Vector3(5.5f, 0f, 0f), new Vector3(7f, 1.2f, 1f));

            // 4. Build toggleable dynamic gate blocking the doorway
            _dynamicGate = CreateWall("DynamicGate", new Vector3(0f, 0f, 0f), new Vector3(3.5f, 1.2f, 1f));

            // 5. Scan surface and ensure wall cells are marked even if Physics module is stripped
            RescanSurface();

            // 6. Create NavAgent with a capsule visual
            _agentVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _agentVisual.name = "SampleNavAgent";
            _agentVisual.transform.SetParent(transform, false);
            _agentVisual.transform.position = new Vector3(-7f, 0f, -7f);
            Destroy(_agentVisual.GetComponent<Collider>());

            _agent = _agentVisual.AddComponent<NavAgent>();
            _agent.Speed = _agentSpeed;
            _agent.SetSurface(_surface);
            _agent.OnDestinationReached += () => _status = "Destination reached!";
            _agent.OnPathFailed += () => _status = "No path found (blocked).";

            // 7. Create goal marker sphere
            _goalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _goalMarker.name = "GoalMarker";
            _goalMarker.transform.SetParent(transform, false);
            _goalMarker.transform.localScale = Vector3.one * 0.6f;
            Destroy(_goalMarker.GetComponent<Collider>());

            SendAgentTo(new Vector3(7f, 0f, 7f));
        }

        private GameObject CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(transform, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            return wall;
        }

        private void RescanSurface()
        {
            _surface.Scan();

            // Fallback cell marking so the sample works identically even without 3D Physics enabled
            if (_surface.Grid != null)
            {
                MarkBoxBlocked(new Vector3(-5.5f, 0f, 0f), new Vector3(7f, 1.2f, 1f), true);
                MarkBoxBlocked(new Vector3(5.5f, 0f, 0f), new Vector3(7f, 1.2f, 1f), true);
                MarkBoxBlocked(new Vector3(0f, 0f, 0f), new Vector3(3.5f, 1.2f, 1f), _gateClosed);
            }
        }

        private void MarkBoxBlocked(Vector3 center, Vector3 size, bool blocked)
        {
            Vector3 half = size * 0.5f;
            GridCoord3D min = _surface.WorldToCell(center - half);
            GridCoord3D max = _surface.WorldToCell(center + half);
            for (int z = min.Z; z <= max.Z; z++)
            {
                for (int x = min.X; x <= max.X; x++)
                {
                    if (_surface.Grid.IsInBounds(x, 0, z))
                    {
                        _surface.Grid.SetWalkable(x, 0, z, !blocked);
                    }
                }
            }
        }

        private void SendAgentTo(Vector3 target)
        {
            target.y = 0f;
            _goalMarker.transform.position = target;
            _surface.TryFindPath(_agent.transform.position, target, _previewPath, allowDiagonals: true, smoothPath: true);
            _agent.SetDestination(target);
            _status = $"Moving to ({target.x:0.0}, {target.z:0.0})";
        }

        private void ToggleGate()
        {
            _gateClosed = !_gateClosed;
            _dynamicGate.SetActive(_gateClosed);
            RescanSurface();
            _status = _gateClosed ? "Gate CLOSED (surface updated)" : "Gate OPENED (surface updated)";
        }

        private void OnDrawGizmos()
        {
            if (_previewPath == null || _previewPath.Count < 2)
                return;

            Gizmos.color = Color.green;
            for (int i = 0; i < _previewPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_previewPath[i] + Vector3.up * 0.25f, _previewPath[i + 1] + Vector3.up * 0.25f);
            }
        }

        private void OnGUI()
        {
            if (_agent == null || _surface == null)
                return;

            GUILayout.BeginArea(new Rect(16, 16, 380, 290), "Navigation Playground Sample", GUI.skin.window);

            GUILayout.Label($"Status: {_status}");
            GUILayout.Label($"Surface: {_surface.Mode} ({_surface.Grid.Width}x{_surface.Grid.Height}x{_surface.Grid.Depth}, v{_surface.Grid.Version})");
            GUILayout.Label($"Agent Pos: ({_agent.transform.position.x:0.0}, {_agent.transform.position.z:0.0})  |  Remaining: {_agent.RemainingDistance:0.0}m");
            GUILayout.Label($"Dynamic Gate: {(_gateClosed ? "CLOSED (forces detour)" : "OPEN (shortcut)")}");

            GUILayout.Space(8);
            if (GUILayout.Button(_gateClosed ? "Open Center Gate" : "Close Center Gate"))
            {
                ToggleGate();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go North-East")) SendAgentTo(new Vector3(7f, 0f, 7f));
            if (GUILayout.Button("Go South-West")) SendAgentTo(new Vector3(-7f, 0f, -7f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random Goal"))
            {
                float rx = UnityEngine.Random.Range(-8f, 8f);
                float rz = UnityEngine.Random.Range(-8f, 8f);
                if (Mathf.Abs(rz) < 1.5f) rz = rz < 0f ? -3f : 3f;
                SendAgentTo(new Vector3(rx, 0f, rz));
            }
            if (GUILayout.Button("Stop Agent"))
            {
                _agent.Stop();
                _status = "Stopped";
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}

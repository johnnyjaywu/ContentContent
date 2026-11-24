using ContentContent.Navigation.Unity;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Navigation.Editor
{
    [CustomEditor(typeof(NavSurface))]
    internal sealed class NavSurfaceEditor : UnityEditor.Editor
    {
        private SerializedProperty _obstacleMaskProp;

        private void OnEnable()
        {
            _obstacleMaskProp = serializedObject.FindProperty("_obstacleMask");
        }

        public override void OnInspectorGUI()
        {
            var surface = (NavSurface)target;

            serializedObject.Update();
            if (_obstacleMaskProp != null && _obstacleMaskProp.intValue == 0)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("Obstacle Mask is Nothing. No colliders will block agent paths.", MessageType.Warning);
                if (GUILayout.Button("Use Default Layer", GUILayout.Width(130f), GUILayout.Height(36f)))
                {
                    _obstacleMaskProp.intValue = 1;
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Navigation Grid Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mode", surface.Mode.ToString());
            if (surface.Grid != null)
            {
                EditorGUILayout.LabelField("Dimensions", $"{surface.Grid.Width} x {surface.Grid.Height} x {surface.Grid.Depth} ({surface.Grid.Count} cells)");
                EditorGUILayout.LabelField("Grid Version", surface.Grid.Version.ToString());
            }
            else
            {
                Vector3 sz = surface.Size;
                float cell = surface.CellSize;
                int w = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(sz.x) / cell));
                int h = surface.Mode == NavMode.PlanarXZ ? 1 : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(sz.y) / cell));
                int d = surface.Mode == NavMode.PlanarXY ? 1 : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(sz.z) / cell));
                EditorGUILayout.LabelField("Estimated Grid", $"{w} x {h} x {d} ({w * h * d} cells, not scanned yet)");
            }

            if (GUILayout.Button("Scan Surface Now"))
            {
                surface.Scan();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(NavAgent))]
    internal sealed class NavAgentEditor : UnityEditor.Editor
    {
        private SerializedProperty _surfaceProp;

        private void OnEnable()
        {
            _surfaceProp = serializedObject.FindProperty("_surface");
        }

        public override void OnInspectorGUI()
        {
            var agent = (NavAgent)target;

            serializedObject.Update();
            if (_surfaceProp != null && _surfaceProp.objectReferenceValue == null)
            {
                NavSurface sceneSurface = UnityEngine.Object.FindAnyObjectByType<NavSurface>();
                if (sceneSurface == null)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox("No NavSurface found in scene. Add a NavSurface so this agent can pathfind.", MessageType.Warning);
                    if (GUILayout.Button("Create NavSurface", GUILayout.Width(130f), GUILayout.Height(36f)))
                    {
                        var surfaceGo = new GameObject("NavSurface");
                        NavSurface created = surfaceGo.AddComponent<NavSurface>();
                        Undo.RegisterCreatedObjectUndo(surfaceGo, "Create NavSurface");
                        _surfaceProp.objectReferenceValue = created;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox($"Surface unset: will auto-discover nearest active NavSurface (e.g. '{sceneSurface.name}').", MessageType.Info);
                }
            }
            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Agent Telemetry", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Has Path", agent.HasPath ? "Yes" : "No");
            EditorGUILayout.LabelField("Is Stopped", agent.IsStopped ? "Yes" : "No");
            EditorGUILayout.LabelField("Remaining Distance", $"{agent.RemainingDistance:0.00}");
            EditorGUILayout.LabelField("Velocity", agent.Velocity.ToString("F2"));

            if (GUILayout.Button("Stop Agent"))
            {
                agent.Stop();
            }
            EditorGUILayout.EndVertical();
            Repaint();
        }
    }

    [CustomEditor(typeof(NavPatrol))]
    internal sealed class NavPatrolEditor : UnityEditor.Editor
    {
        private SerializedProperty _waypointsProp;

        private void OnEnable()
        {
            _waypointsProp = serializedObject.FindProperty("_waypoints");
        }

        public override void OnInspectorGUI()
        {
            var patrol = (NavPatrol)target;

            serializedObject.Update();
            int validCount = 0;
            if (_waypointsProp != null)
            {
                for (int i = 0; i < _waypointsProp.arraySize; i++)
                {
                    if (_waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue != null)
                        validCount++;
                }
            }

            if (validCount < 2)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.HelpBox($"NavPatrol needs at least 2 waypoints (currently {validCount}).", MessageType.Warning);
                if (GUILayout.Button("Add Child Waypoint", GUILayout.Width(140f), GUILayout.Height(36f)))
                {
                    int idx = _waypointsProp != null ? _waypointsProp.arraySize : 0;
                    var wp = new GameObject($"Waypoint_{idx}");
                    wp.transform.SetParent(patrol.transform, false);
                    wp.transform.localPosition = new Vector3((idx + 1) * 2f, 0f, 0f);
                    Undo.RegisterCreatedObjectUndo(wp, "Add Child Waypoint");

                    if (_waypointsProp != null)
                    {
                        _waypointsProp.InsertArrayElementAtIndex(idx);
                        _waypointsProp.GetArrayElementAtIndex(idx).objectReferenceValue = wp.transform;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();

            DrawDefaultInspector();
        }
    }
}

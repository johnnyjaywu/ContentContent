#if UNITY_EDITOR
using System.Collections.Generic;
using ContentContent.Navigation.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Navigation.Tests
{
    public class NavigationUnityAdapterTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        [Test]
        public void NavSurface_Scan_BuildsExpectedPlanarXYGrid()
        {
            var go = new GameObject("SurfaceXY");
            _created.Add(go);
            var surface = go.AddComponent<NavSurface>();

            var so = new SerializedObject(surface);
            so.FindProperty("_mode").intValue = (int)NavMode.PlanarXY;
            so.FindProperty("_size").vector3Value = new Vector3(10f, 6f, 1f);
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_obstacleMask").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            surface.Scan();

            Assert.IsNotNull(surface.Grid);
            Assert.AreEqual(NavMode.PlanarXY, surface.Grid.Mode);
            Assert.AreEqual(10, surface.Grid.Width);
            Assert.AreEqual(6, surface.Grid.Height);
            Assert.AreEqual(1, surface.Grid.Depth);

            var path = new List<Vector3>();
            bool found = surface.TryFindPath(new Vector3(-3f, -1f, 0f), new Vector3(3f, 1f, 0f), path);
            Assert.IsTrue(found);
            Assert.GreaterOrEqual(path.Count, 2);
        }

        [Test]
        public void NavPatrol_LoopAndPingPong_AdvanceIndicesCorrectly()
        {
            var surfaceGo = new GameObject("Surface");
            _created.Add(surfaceGo);
            var surface = surfaceGo.AddComponent<NavSurface>();
            surface.Scan();

            var agentGo = new GameObject("Patroller");
            _created.Add(agentGo);
            var agent = agentGo.AddComponent<NavAgent>();
            agent.SetSurface(surface);

            var patrol = agentGo.AddComponent<NavPatrol>();
            for (int i = 0; i < 3; i++)
            {
                var wp = new GameObject($"WP_{i}");
                _created.Add(wp);
                wp.transform.position = new Vector3(i * 2f, 0f, 0f);
                patrol.Waypoints.Add(wp.transform);
            }

            patrol.Mode = PatrolMode.Loop;
            patrol.StartPatrol();
            Assert.AreEqual(0, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(1, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(2, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(0, patrol.CurrentWaypointIndex);

            patrol.Mode = PatrolMode.PingPong;
            patrol.StartPatrol();
            Assert.AreEqual(0, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(1, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(2, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(1, patrol.CurrentWaypointIndex);
            patrol.AdvanceToNextWaypoint();
            Assert.AreEqual(0, patrol.CurrentWaypointIndex);
        }

        [Test]
        public void NavFollowTarget_ForceRepath_SetsAgentDestination()
        {
            var surfaceGo = new GameObject("Surface");
            _created.Add(surfaceGo);
            var surface = surfaceGo.AddComponent<NavSurface>();
            surface.Scan();

            var targetGo = new GameObject("PlayerTarget");
            _created.Add(targetGo);
            targetGo.transform.position = new Vector3(4f, 0f, 4f);

            var followerGo = new GameObject("Follower");
            _created.Add(followerGo);
            var agent = followerGo.AddComponent<NavAgent>();
            agent.SetSurface(surface);
            var follower = followerGo.AddComponent<NavFollowTarget>();

            follower.Target = targetGo.transform;
            follower.ForceRepath();

            Assert.IsFalse(agent.IsStopped);
            Assert.AreEqual(4f, agent.Destination.x, 0.001f);
            Assert.AreEqual(4f, agent.Destination.z, 0.001f);
        }
    }
}
#endif

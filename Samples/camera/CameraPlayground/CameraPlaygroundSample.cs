using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NumVector3 = System.Numerics.Vector3;

namespace ContentContent.Camera.Samples
{
    /// <summary>
    /// Interactive playground demonstrating <see cref="CameraDirector"/> look-ahead,
    /// grounded vertical peek, zone priority stacks (Room vs. Boss Arena), and instant snap cuts.
    /// Uses Unity's New Input System (<c>com.unity.inputsystem</c>).
    /// </summary>
    public class CameraPlaygroundSample : MonoBehaviour
    {
        private CameraDirector _director;
        private Transform _player;
        private Transform _anchorMarker;
        private UnityEngine.Camera _mainCam;

        private Vector3 _playerPos;
        private Vector3 _facing = Vector3.right;
        private bool _inBossArena;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            _director = new CameraDirector(CameraFramingConfig.CreateHollowKnightPreset());
            _director.Zones.OnActiveZoneChanged += e =>
                AppendLog($"[Zone] {(string.IsNullOrEmpty(e.PreviousZoneId) ? "None" : e.PreviousZoneId)} -> {(string.IsNullOrEmpty(e.ActiveZoneId) ? "None" : e.ActiveZoneId)}");
            _director.OnSnapRequested += e => AppendLog($"[Snap] {e.Reason}");

            _director.Zones.EnterZone(new CameraZoneConfig
            {
                ZoneId = "Crossroads_01",
                Priority = 1,
                BoundsId = "Crossroads_01_Bounds"
            });

            var playerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerObj.name = "PlayerTarget";
            Destroy(playerObj.GetComponent<Collider>());
            _player = playerObj.transform;

            var anchorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            anchorObj.name = "CameraFollowAnchor";
            anchorObj.transform.localScale = Vector3.one * 0.45f;
            Destroy(anchorObj.GetComponent<Collider>());
            _anchorMarker = anchorObj.transform;

            _mainCam = UnityEngine.Camera.main;
            if (_mainCam != null)
            {
                _mainCam.orthographic = true;
            }
        }

        private void Update()
        {
            if (_director == null)
                return;

            float moveX = 0f;
            float lookY = 0f;
            bool toggleBoss = false;
            bool snapNow = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) lookY += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) lookY -= 1f;
                toggleBoss = kb.bKey.wasPressedThisFrame;
                snapNow = kb.rKey.wasPressedThisFrame;
            }
#endif

            if (Mathf.Abs(moveX) > 0.01f)
            {
                _facing = moveX > 0f ? Vector3.right : Vector3.left;
                _playerPos.x = Mathf.Clamp(_playerPos.x + moveX * 7f * Time.deltaTime, -12f, 12f);
            }

            if (toggleBoss)
            {
                ToggleBossZone();
            }

            if (snapNow)
            {
                _playerPos = Vector3.zero;
                _director.RequestSnap(NumVector3.Zero, "Respawn");
            }

            _player.position = _playerPos;

            var input = new CameraFrameInput
            {
                TargetPosition = new NumVector3(_playerPos.x, _playerPos.y, _playerPos.z),
                FacingDirection = new NumVector3(_facing.x, _facing.y, _facing.z),
                VerticalLookInput = lookY,
                IsTargetGrounded = true,
                IsTargetStationary = Mathf.Abs(moveX) < 0.01f
            };

            CameraFrameOutput output = _director.Tick(Time.deltaTime, in input);
            var anchor = new Vector3(output.FollowAnchorPosition.X, output.FollowAnchorPosition.Y, 0f);
            _anchorMarker.position = anchor;

            if (_mainCam != null)
            {
                _mainCam.orthographicSize = output.OrthographicSize;
                _mainCam.transform.position = new Vector3(anchor.x, anchor.y, _mainCam.transform.position.z);
            }
        }

        private void ToggleBossZone()
        {
            _inBossArena = !_inBossArena;
            if (_inBossArena)
            {
                _director.Zones.EnterZone(new CameraZoneConfig
                {
                    ZoneId = "BossArena",
                    Priority = 10,
                    LockPosition = true,
                    LockedPosition = new NumVector3(0f, 1.5f, 0f),
                    OrthographicSizeOverride = 8.5f,
                    AllowLookAhead = false,
                    AllowVerticalPeek = false,
                    BoundsId = "BossArenaBounds"
                });
            }
            else
            {
                _director.Zones.ExitZone("BossArena");
            }
        }

        private void AppendLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 6)
            {
                _log.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (_director == null)
                return;

            CameraFrameOutput output = _director.CurrentOutput;
            GUILayout.BeginArea(new Rect(16, 16, 430, 320), "Camera Playground Sample", GUI.skin.window);
            GUILayout.Label($"Active Zone: {output.ActiveZoneId}  |  Locked: {output.IsPositionLocked}");
            GUILayout.Label($"LookAhead: {_director.CurrentLookAheadOffset:0.00}  |  PeekY: {_director.CurrentVerticalPeekOffset:0.00}  |  Ortho: {output.OrthographicSize:0.00}");
            GUILayout.Label("Controls (New Input System): A/D Move | Hold W/S Peek | B Boss Zone | R Snap");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_inBossArena ? "Exit Boss Zone" : "Enter Boss Zone")) ToggleBossZone();
            if (GUILayout.Button("Snap to Origin")) _director.RequestSnap(NumVector3.Zero, "ManualSnap");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            for (int i = 0; i < _log.Count; i++)
            {
                GUILayout.Label(_log[i]);
            }

            GUILayout.EndArea();
        }
    }
}

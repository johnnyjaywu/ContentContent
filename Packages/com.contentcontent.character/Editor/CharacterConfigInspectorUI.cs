using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Editor
{
    internal static class CharacterConfigInspectorUI
    {
        private static readonly List<string> WarningsBuffer = new List<string>(8);

        private static bool _showMovement = true;
        private static bool _showGravity = true;
        private static bool _showJump = true;
        private static bool _showDash = true;
        private static bool _showWallSlideJump;
        private static bool _showWallClimb;
        private static bool _showPlatforms;
        private static bool _showKnockback;

        public static void DrawConfig(
            SerializedProperty configProp,
            CharacterConfig runtimeConfig,
            UnityEngine.Object undoTarget,
            Action<CharacterPresetType> onPresetClicked,
            bool allow2DPresets,
            bool allow3DPreset)
        {
            if (configProp == null || runtimeConfig == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Movement & Feel Configuration", EditorStyles.boldLabel);

            DrawPresetButtons(configProp, undoTarget, onPresetClicked, allow2DPresets, allow3DPreset);

            SerializedProperty modeProp = configProp.FindPropertyRelative("Mode");
            EditorGUILayout.PropertyField(modeProp, new GUIContent(
                "Movement Mode",
                "Spatial plane and gravity model: SideScroller2D (X + Y gravity), TopDown2D (XY no gravity), or ThirdPerson3D (XZ + Y gravity)."));

            MovementMode mode = (MovementMode)modeProp.enumValueIndex;
            bool hasGravity = mode != MovementMode.TopDown2D;
            bool isSideScroller = mode == MovementMode.SideScroller2D;

            DrawValidationWarnings(runtimeConfig);

            EditorGUILayout.Space(2f);

            // 1. Movement
            _showMovement = EditorGUILayout.Foldout(_showMovement, "Planar Movement", true);
            if (_showMovement)
            {
                EditorGUI.indentLevel++;
                DrawProp(configProp, "MaxSpeed", "Max Speed", "Top horizontal/planar speed in units per second.");
                DrawProp(configProp, "GroundAcceleration", "Ground Acceleration", "How quickly the character reaches Max Speed on the ground.");
                DrawProp(configProp, "GroundDeceleration", "Ground Deceleration", "How quickly the character stops when movement input stops.");
                if (hasGravity)
                {
                    DrawProp(configProp, "AirAcceleration", "Air Acceleration", "Horizontal steering responsiveness while airborne.");
                    DrawProp(configProp, "AirDeceleration", "Air Deceleration", "Horizontal drag in mid-air when no input is held.");
                }
                DrawProp(configProp, "MinMoveThreshold", "Move Deadzone", "Minimum stick/input magnitude treated as intentional movement.");
                EditorGUI.indentLevel--;
            }

            // 2. Gravity & Falling
            if (hasGravity)
            {
                _showGravity = EditorGUILayout.Foldout(_showGravity, "Gravity & Falling", true);
                if (_showGravity)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(configProp, "Gravity", "Gravity", "Base downward acceleration in units/sec^2.");
                    DrawProp(configProp, "FallGravityMultiplier", "Fall Gravity Multiplier", "Extra gravity applied while descending (>1 makes jumps feel snappy, not floaty).");
                    DrawProp(configProp, "MaxFallSpeed", "Max Fall Speed", "Terminal downward speed cap.");

                    SerializedProperty apexThresholdProp = configProp.FindPropertyRelative("ApexVelocityThreshold");
                    DrawProp(apexThresholdProp, "Apex Hang Threshold", "Vertical speed range near the jump peak where gravity softens while Jump is held (0 disables).");
                    if (apexThresholdProp != null && apexThresholdProp.floatValue > 0f)
                    {
                        DrawProp(configProp, "ApexGravityMultiplier", "Apex Gravity Multiplier", "Gravity scale applied inside the apex hang window (0..1).");
                    }

                    DrawProp(configProp, "FastFallMaxSpeed", "Fast-Fall Cap", "Higher fall speed cap while holding Down mid-air (0 disables).");
                    DrawProp(configProp, "GroundStickVelocity", "Ground Stick Speed", "Small downward pull while grounded to stay planted on slopes.");
                    EditorGUI.indentLevel--;
                }
            }

            // 3. Jump Feel
            if (hasGravity)
            {
                _showJump = EditorGUILayout.Foldout(_showJump, "Jump Feel & Forgiveness", true);
                if (_showJump)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(configProp, "JumpVelocity", "Jump Speed", "Initial upward launch speed on jump.");
                    DrawProp(configProp, "JumpCutMultiplier", "Jump Release Cut", "Scales remaining upward speed when releasing Jump early (smaller = shorter tap hops; 1 = fixed height).");
                    DrawProp(configProp, "CoyoteTime", "Coyote Time", "Grace window in seconds after walking off a ledge where a ground jump still succeeds.");
                    DrawProp(configProp, "JumpBufferTime", "Jump Buffer", "Window before landing during which an early Jump press automatically fires on touchdown.");
                    DrawProp(configProp, "MaxAirJumps", "Air Jumps", "Number of extra mid-air jumps allowed before touching ground again (0 = single jump).");
                    EditorGUI.indentLevel--;
                }
            }

            // 4. Dash
            _showDash = EditorGUILayout.Foldout(_showDash, "Dash", true);
            if (_showDash)
            {
                EditorGUI.indentLevel++;
                DrawProp(configProp, "DashSpeed", "Dash Speed", "Burst speed during a dash.");
                DrawProp(configProp, "DashDuration", "Dash Duration", "Length of the dash burst in seconds (0 disables dash).");
                DrawProp(configProp, "DashCooldown", "Dash Cooldown", "Delay in seconds before another dash can trigger.");
                DrawProp(configProp, "DashBufferTime", "Dash Buffer", "Window during which a Dash press is remembered until ready.");
                DrawProp(configProp, "DashEndSpeedMultiplier", "Dash End Speed Scale", "Fraction of velocity retained when the dash finishes (0..1).");

                if (hasGravity)
                {
                    DrawProp(configProp, "DashSuspendsGravity", "Suspend Gravity", "Disables gravity during horizontal dashes.");
                    SerializedProperty allowAirDashProp = configProp.FindPropertyRelative("AllowAirDash");
                    DrawProp(allowAirDashProp, "Allow Air Dash", "Allows initiating a dash while airborne.");
                    if (allowAirDashProp != null && allowAirDashProp.boolValue)
                    {
                        DrawProp(configProp, "MaxAirDashes", "Max Air Dashes", "Number of mid-air dashes allowed before landing.");
                    }
                }

                if (isSideScroller)
                {
                    DrawProp(configProp, "AllowOmnidirectionalDash2D", "8-Way Dash", "Aim dashes in 8 directions using both X and Y input instead of horizontal only.");
                }
                EditorGUI.indentLevel--;
            }

            // 5. Wall Slide & Wall Jump
            if (isSideScroller)
            {
                _showWallSlideJump = EditorGUILayout.Foldout(_showWallSlideJump, "Wall Slide & Wall Jump", true);
                if (_showWallSlideJump)
                {
                    EditorGUI.indentLevel++;
                    SerializedProperty slideProp = configProp.FindPropertyRelative("EnableWallSlide");
                    DrawProp(slideProp, "Enable Wall Slide", "Slows fall speed when sliding along a wall.");
                    if (slideProp != null && slideProp.boolValue)
                    {
                        DrawProp(configProp, "WallSlideRequiresInput", "Require Input Toward Wall", "Only wall-slide when pressing horizontally toward the wall.");
                        DrawProp(configProp, "WallSlideMaxFallSpeed", "Wall Slide Speed", "Maximum downward fall speed while wall-sliding.");
                        DrawProp(configProp, "RefreshAirDashOnWallSlide", "Refresh Air Dash on Wall", "Restores mid-air dash charges when wall-sliding or wall-jumping.");
                    }

                    SerializedProperty wallJumpProp = configProp.FindPropertyRelative("EnableWallJump");
                    DrawProp(wallJumpProp, "Enable Wall Jump", "Allows kicking off walls in mid-air.");
                    if (wallJumpProp != null && wallJumpProp.boolValue)
                    {
                        DrawProp(configProp, "WallJumpHorizontalSpeed", "Kick Horizontal Speed", "Horizontal launch speed away from the wall.");
                        DrawProp(configProp, "WallJumpVerticalSpeed", "Kick Vertical Speed", "Upward launch speed on a wall jump.");
                        DrawProp(configProp, "WallJumpCoyoteTime", "Wall Coyote Time", "Grace window after leaving a wall where a wall jump still works.");
                        DrawProp(configProp, "WallJumpLockTime", "Wall Jump Steering Lock", "Brief horizontal input lock preserving the kickback arc away from the wall.");
                        DrawProp(configProp, "RefreshAirJumpOnWallJump", "Refresh Air Jump on Kick", "Restores mid-air jump charges after a wall jump.");
                    }
                    EditorGUI.indentLevel--;
                }

                // 6. Wall Climb & Stamina
                _showWallClimb = EditorGUILayout.Foldout(_showWallClimb, "Wall Climb & Stamina", true);
                if (_showWallClimb)
                {
                    EditorGUI.indentLevel++;
                    SerializedProperty climbProp = configProp.FindPropertyRelative("EnableWallClimb");
                    DrawProp(climbProp, "Enable Wall Climb", "Allows grabbing and climbing up/down walls while holding Grab.");
                    if (climbProp != null && climbProp.boolValue)
                    {
                        DrawProp(configProp, "WallClimbSpeed", "Climb Speed", "Vertical climbing speed.");
                        DrawProp(configProp, "MaxClimbStamina", "Max Climb Stamina", "Stamina pool refilled on landing.");
                        DrawProp(configProp, "ClimbStaminaDrainPerSecond", "Climb Drain / Sec", "Stamina spent per second while climbing upward.");
                        DrawProp(configProp, "ClimbHoldStaminaDrainPerSecond", "Hold Drain / Sec", "Stamina spent per second while clinging stationary.");
                        DrawProp(configProp, "WallJumpStaminaCost", "Climb Jump Cost", "Stamina spent when jumping off a wall while holding Grab.");
                    }
                    EditorGUI.indentLevel--;
                }

                // 7. Platforms
                _showPlatforms = EditorGUILayout.Foldout(_showPlatforms, "Moving & One-Way Platforms", true);
                if (_showPlatforms)
                {
                    EditorGUI.indentLevel++;
                    DrawProp(configProp, "PlatformMomentumInheritance", "Platform Launch Inheritance", "Fraction of moving platform velocity inherited when jumping off it.");
                    DrawProp(configProp, "OneWayDropThroughDuration", "Drop-Through Duration", "Time in seconds one-way platforms are ignored after pressing drop-through.");
                    EditorGUI.indentLevel--;
                }
            }

            // 8. Knockback
            _showKnockback = EditorGUILayout.Foldout(_showKnockback, "External Impulses & Knockback", true);
            if (_showKnockback)
            {
                EditorGUI.indentLevel++;
                DrawProp(configProp, "KnockbackDecay", "Knockback Decay", "Rate at which external impulses (knockback, launch pads) bleed back to zero.");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        public static void DrawTelemetry(CoreCharacterController controller)
        {
            if (controller == null)
            {
                return;
            }

            CharacterMotor motor = controller.Motor;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PlayMode Telemetry (Live)", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("HFSM State", controller.CurrentState?.ToString() ?? "None");
            EditorGUILayout.LabelField("Velocity", $"({motor.Velocity.X:0.00}, {motor.Velocity.Y:0.00}, {motor.Velocity.Z:0.00})  |  Planar {motor.PlanarSpeed:0.00}");
            EditorGUILayout.LabelField("Contacts", $"Grounded: {motor.IsGrounded}   Wall: {motor.IsWallSliding || motor.Sensors.IsTouchingWall}   Ceiling: {motor.Sensors.IsTouchingCeiling}");
            EditorGUILayout.LabelField("Charges", $"Air Jumps: {motor.AirJumpsRemaining}/{controller.Config.MaxAirJumps}   Air Dashes: {motor.AirDashesRemaining}/{controller.Config.MaxAirDashes}");
            EditorGUILayout.LabelField("Forgiveness", $"Coyote: {motor.CoyoteTimeRemaining:0.00}s   WallCoyote: {motor.WallCoyoteTimeRemaining:0.00}s   JumpBuf: {motor.HasBufferedJump}   DashBuf: {motor.HasBufferedDash}");
            EditorGUILayout.LabelField("Dash Cooldown", motor.IsDashCooldownReady ? "Ready" : $"{motor.DashCooldownRemaining:0.00}s remaining");

            if (controller.Config.Mode == MovementMode.SideScroller2D && controller.Config.EnableWallClimb && controller.Config.MaxClimbStamina > 0f)
            {
                Rect barRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                float ratio = Mathf.Clamp01(motor.ClimbStamina / controller.Config.MaxClimbStamina);
                EditorGUI.ProgressBar(barRect, ratio, $"Climb Stamina: {motor.ClimbStamina:0.0}/{controller.Config.MaxClimbStamina:0.0}");
            }

            string actionLabel = controller.Actions.IsActionActive
                ? $"{controller.Actions.ActiveAction.Id} (Locks: {motor.ActiveLocks})"
                : (motor.ActiveLocks != ActionLockFlags.None ? $"None (Manual Locks: {motor.ActiveLocks})" : "None");
            EditorGUILayout.LabelField("Active Action", actionLabel);

            EditorGUILayout.EndVertical();
        }

        public static Type ResolveOptionalType(string assemblyQualifiedName)
        {
            return Type.GetType(assemblyQualifiedName, throwOnError: false);
        }

        private static void DrawPresetButtons(
            SerializedProperty configProp,
            UnityEngine.Object undoTarget,
            Action<CharacterPresetType> onPresetClicked,
            bool allow2DPresets,
            bool allow3DPreset)
        {
            if (onPresetClicked == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Apply Preset", "Overwrite settings below with a tuned starting point (supports Undo)."));

            if (allow2DPresets)
            {
                if (GUILayout.Button("Side-Scroller")) InvokePreset(configProp, undoTarget, onPresetClicked, CharacterPresetType.SideScroller);
                if (GUILayout.Button("Action")) InvokePreset(configProp, undoTarget, onPresetClicked, CharacterPresetType.ActionPlatformer);
                if (GUILayout.Button("Climber")) InvokePreset(configProp, undoTarget, onPresetClicked, CharacterPresetType.PrecisionClimber);
                if (GUILayout.Button("Top-Down")) InvokePreset(configProp, undoTarget, onPresetClicked, CharacterPresetType.TopDown);
            }

            if (allow3DPreset)
            {
                if (GUILayout.Button("3D Third-Person")) InvokePreset(configProp, undoTarget, onPresetClicked, CharacterPresetType.ThirdPerson);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private static void InvokePreset(
            SerializedProperty configProp,
            UnityEngine.Object undoTarget,
            Action<CharacterPresetType> onPresetClicked,
            CharacterPresetType preset)
        {
            if (undoTarget != null)
            {
                Undo.RecordObject(undoTarget, $"Apply {preset} Preset");
            }

            onPresetClicked(preset);

            if (undoTarget != null)
            {
                EditorUtility.SetDirty(undoTarget);
            }

            configProp?.serializedObject?.Update();
        }

        private static void DrawValidationWarnings(CharacterConfig runtimeConfig)
        {
            runtimeConfig.Validate(WarningsBuffer);
            for (int i = 0; i < WarningsBuffer.Count; i++)
            {
                EditorGUILayout.HelpBox(WarningsBuffer[i], MessageType.Warning);
            }
        }

        private static void DrawProp(SerializedProperty parent, string relativeName, string label, string tooltip)
        {
            SerializedProperty prop = parent.FindPropertyRelative(relativeName);
            DrawProp(prop, label, tooltip);
        }

        private static void DrawProp(SerializedProperty prop, string label, string tooltip)
        {
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            }
        }
    }
}

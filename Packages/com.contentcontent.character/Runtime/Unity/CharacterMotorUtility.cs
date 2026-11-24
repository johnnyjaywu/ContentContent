using System.Collections.Generic;
using UnityEngine;
using CoreCharacterController = ContentContent.Character.CharacterController;

namespace ContentContent.Character.Unity
{
    internal static class CharacterMotorUtility
    {
        public static void RegisterActionBindings(
            CoreCharacterController controller,
            IReadOnlyList<CharacterActionBinding> bindings,
            Object owner)
        {
            if (controller == null || bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                CharacterActionBinding binding = bindings[i];
                if (binding == null || binding.Action == null)
                {
                    continue;
                }

                string actionId = string.IsNullOrWhiteSpace(binding.Action.ActionId)
                    ? binding.Action.name
                    : binding.Action.ActionId.Trim();

                if (controller.Actions.TryGetAction(actionId, out _))
                {
                    Debug.LogWarning(
                        $"Duplicate character action ID '{actionId}' on '{owner.name}'. Skipping duplicate binding.",
                        owner);
                    continue;
                }

                CharacterActionBinding captured = binding;
                SimpleCharacterAction runtimeAction = binding.Action.BuildRuntimeAction(
                    onStart: _ => captured.OnStarted?.Invoke(),
                    onEnd: (_, interrupted) =>
                    {
                        captured.OnEnded?.Invoke();
                        if (interrupted)
                        {
                            captured.OnInterrupted?.Invoke();
                        }
                    });

                controller.RegisterAction(runtimeAction);
            }
        }

        public static void SyncLiveConfig(
            CoreCharacterController controller,
            CharacterConfigAsset configAsset,
            CharacterConfig inlineConfig)
        {
            if (controller == null)
            {
                return;
            }

            CharacterConfig source = configAsset != null ? configAsset.Config : inlineConfig;
            if (source == null)
            {
                return;
            }

            controller.Config.CopyFrom(source);
            controller.Motor.RefreshConfig();
        }

        public static void ApplyPreset(
            CoreCharacterController controller,
            CharacterConfigAsset configAsset,
            CharacterConfig inlineConfig,
            CharacterPresetType preset)
        {
            if (configAsset != null)
            {
                configAsset.ApplyPreset(preset);
            }
            else if (inlineConfig != null)
            {
                inlineConfig.CopyFrom(CharacterConfig.CreatePreset(preset));
            }

            SyncLiveConfig(controller, configAsset, inlineConfig);
        }
    }
}

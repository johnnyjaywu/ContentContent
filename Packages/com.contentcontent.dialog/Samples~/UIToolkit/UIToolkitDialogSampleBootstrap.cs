using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Samples.UIToolkit
{
    /// <summary>
    /// Bootstraps the UI Toolkit dialog sample, wiring <see cref="DialogRunnerBehaviour"/>
    /// to <see cref="UIToolkitDialogView"/> and <see cref="SampleDialogData"/>.
    /// </summary>
    [RequireComponent(typeof(DialogRunnerBehaviour))]
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(UIToolkitDialogView))]
    public class UIToolkitDialogSampleBootstrap : MonoBehaviour
    {
        private DialogRunnerBehaviour _runner;
        private UIToolkitDialogView _dialogView;
        private UIDocument _document;
        private DialogGraph _graph;
        private string _lastCommand = "none";

        private void Awake()
        {
            _runner = GetComponent<DialogRunnerBehaviour>();
            _dialogView = GetComponent<UIToolkitDialogView>();
            _document = GetComponent<UIDocument>();

            if (_document.panelSettings == null)
            {
                PanelSettings runtimePanel = ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                runtimePanel.referenceResolution = new Vector2Int(1920, 1080);
                runtimePanel.match = 0.5f;
                _document.panelSettings = runtimePanel;
            }

            _dialogView.Configure(_runner);
            _runner.SetLocalizer(SampleDialogData.CreateLocalizer());
            _runner.SetView(_dialogView);
            _runner.RegisterCommandHandler("play_sfx", HandleCommand);
            _runner.RegisterCommandHandler("open_gate", HandleCommand);
            _runner.RegisterCommandHandler("grant_bonus", HandleCommand);
            _runner.Variables.OnVariableChanged += (_, __) => UpdateStatusHud();

            _dialogView.SetRestartCallback(StartSampleDialog);
        }

        private void Start()
        {
            StartSampleDialog();
        }

        private void StartSampleDialog()
        {
            _runner.Variables.Clear();
            _lastCommand = "none";
            _graph = SampleDialogData.CreateGraph();
            _runner.StartDialog(_graph);
            UpdateStatusHud();
        }

        private void HandleCommand(DialogCommand command)
        {
            string args = command.Arguments.Count > 0 ? $"({string.Join(", ", command.Arguments)})" : string.Empty;
            _lastCommand = $"{command.Name}{args}";
            Debug.Log($"[UI Toolkit Dialog Sample] Command: {_lastCommand}");
            UpdateStatusHud();
        }

        private void UpdateStatusHud()
        {
            if (_dialogView == null)
                return;

            int gold = _runner.Variables.GetInt("gold");
            bool gateOpen = _runner.Variables.GetBool("gate_open");
            _dialogView.SetStatusMessage($"[UI Toolkit] Gold: {gold}   |   Gate Open: {gateOpen}   |   Last Command: {_lastCommand}");
        }
    }
}

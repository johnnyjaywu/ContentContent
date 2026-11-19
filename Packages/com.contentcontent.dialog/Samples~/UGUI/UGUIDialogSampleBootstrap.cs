using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ContentContent.Dialog.Samples.UGUI
{
    /// <summary>
    /// Bootstraps the uGUI dialog sample. If no Canvas/View is pre-wired in the scene,
    /// builds a responsive Canvas hierarchy at runtime so the sample runs out of the box.
    /// </summary>
    [RequireComponent(typeof(DialogRunnerBehaviour))]
    public class UGUIDialogSampleBootstrap : MonoBehaviour
    {
        [SerializeField] private UGUIDialogView _dialogView;

        private DialogRunnerBehaviour _runner;
        private DialogGraph _graph;
        private string _lastCommand = "none";

        private void Awake()
        {
            _runner = GetComponent<DialogRunnerBehaviour>();
            EnsureEventSystem();

            if (_dialogView == null)
                _dialogView = BuildRuntimeUI(_runner);

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
            Debug.Log($"[uGUI Dialog Sample] Command: {_lastCommand}");
            UpdateStatusHud();
        }

        private void UpdateStatusHud()
        {
            if (_dialogView == null)
                return;

            int gold = _runner.Variables.GetInt("gold");
            bool gateOpen = _runner.Variables.GetBool("gate_open");
            _dialogView.SetStatusMessage($"[uGUI] Gold: {gold}   |   Gate Open: {gateOpen}   |   Last Command: {_lastCommand}");
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        public static UGUIDialogView BuildRuntimeUI(DialogRunnerBehaviour runner, Transform parent = null)
        {
            var canvasGo = new GameObject("DialogCanvas");
            if (parent != null)
                canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Status HUD bar (top)
            var statusBar = CreatePanel("StatusBar", canvasGo.transform, new Color(0.08f, 0.10f, 0.14f, 0.90f));
            var statusRect = statusBar.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 1f);
            statusRect.anchorMax = new Vector2(0.95f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -24f);
            statusRect.sizeDelta = new Vector2(0f, 52f);

            TMP_Text statusText = CreateLabel("StatusText", statusBar.transform, 22f, new Color(0.78f, 0.85f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft);
            StretchWithPadding(statusText.rectTransform, 20f, 8f);

            // Dialog box (bottom)
            var panel = CreatePanel("DialogPanel", canvasGo.transform, new Color(0.09f, 0.11f, 0.16f, 0.95f));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.10f, 0f);
            panelRect.anchorMax = new Vector2(0.90f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(0f, 380f);

            var panelLayout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(32, 32, 24, 24);
            panelLayout.spacing = 14f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            TMP_Text speakerText = CreateLabel("SpeakerText", panel.transform, 28f, new Color(0.96f, 0.78f, 0.36f, 1f), TextAlignmentOptions.MidlineLeft);
            speakerText.fontStyle = FontStyles.Bold;
            SetPreferredHeight(speakerText.gameObject, 38f);

            TMP_Text bodyText = CreateLabel("BodyText", panel.transform, 26f, new Color(0.94f, 0.95f, 0.98f, 1f), TextAlignmentOptions.TopLeft);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            SetPreferredHeight(bodyText.gameObject, 110f);

            // Choices container
            var choicesGo = new GameObject("ChoicesContainer", typeof(RectTransform));
            choicesGo.transform.SetParent(panel.transform, false);
            var choicesLayout = choicesGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            choicesLayout.spacing = 8f;
            choicesLayout.childControlWidth = true;
            choicesLayout.childControlHeight = false;
            choicesLayout.childForceExpandWidth = true;
            choicesLayout.childForceExpandHeight = false;

            // Choice button template
            UnityEngine.UI.Button choiceTemplate = CreateButton("ChoiceButtonTemplate", choicesGo.transform, "Choice", new Color(0.17f, 0.22f, 0.31f, 1f), 46f, out _);
            choiceTemplate.gameObject.SetActive(false);

            // Footer buttons
            var footerGo = new GameObject("Footer", typeof(RectTransform));
            footerGo.transform.SetParent(panel.transform, false);
            var footerLayout = footerGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            footerLayout.spacing = 12f;
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = false;
            footerLayout.childControlHeight = false;
            SetPreferredHeight(footerGo, 48f);

            UnityEngine.UI.Button continueButton = CreateButton("ContinueButton", footerGo.transform, "Continue", new Color(0.22f, 0.45f, 0.78f, 1f), 46f, out TMP_Text continueLabel, 180f);
            UnityEngine.UI.Button restartButton = CreateButton("RestartButton", footerGo.transform, "Restart", new Color(0.20f, 0.56f, 0.42f, 1f), 46f, out _, 180f);
            restartButton.gameObject.SetActive(false);

            var view = canvasGo.AddComponent<UGUIDialogView>();
            view.Configure(
                runner,
                panel,
                speakerText,
                bodyText,
                statusText,
                continueButton,
                continueLabel,
                restartButton,
                choicesGo.transform,
                choiceTemplate);

            return view;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static UnityEngine.UI.Button CreateButton(
            string name,
            Transform parent,
            string labelText,
            Color backgroundColor,
            float height,
            out TMP_Text label,
            float width = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width > 0f ? width : 400f, height);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;

            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            var layoutElement = go.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredHeight = height;
            if (width > 0f)
                layoutElement.preferredWidth = width;

            label = CreateLabel("Label", go.transform, 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            label.text = labelText;
            StretchWithPadding(label.rectTransform, 18f, 4f);

            return button;
        }

        private static void SetPreferredHeight(GameObject go, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

            var element = go.AddComponent<UnityEngine.UI.LayoutElement>();
            element.preferredHeight = height;
        }

        private static void StretchWithPadding(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}

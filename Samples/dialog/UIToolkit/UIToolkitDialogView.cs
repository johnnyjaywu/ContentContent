using System;
using System.Collections.Generic;
using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContentContent.Dialog.Samples.UIToolkit
{
    /// <summary>
    /// UI Toolkit implementation of <see cref="IDialogView"/> driving a <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitDialogView : MonoBehaviour, IDialogView
    {
        private const string HiddenClass = "is-hidden";

        [Header("Runner")]
        [SerializeField] private DialogRunnerBehaviour _runner;

        [Header("Typewriter")]
        [SerializeField] private float _charactersPerSecond = 45f;

        private UIDocument _document;
        private VisualElement _dialogPanel;
        private Label _speakerLabel;
        private Label _bodyLabel;
        private Label _statusLabel;
        private VisualElement _choicesContainer;
        private Button _continueButton;
        private Button _restartButton;

        private readonly List<DialogChoiceViewModel> _pendingChoices = new List<DialogChoiceViewModel>();

        private string _fullLineText = string.Empty;
        private float _revealProgress;
        private int _lastVisibleLength = -1;
        private bool _isTyping;
        private bool _lineHasChoices;
        private bool _lineIsEnd;
        private Action _onRestartRequested;

        public void Configure(DialogRunnerBehaviour runner)
        {
            _runner = runner;
        }

        public void SetRestartCallback(Action onRestart)
        {
            _onRestartRequested = onRestart;
        }

        public void SetStatusMessage(string message)
        {
            EnsureBound();
            if (_statusLabel != null)
                _statusLabel.text = message ?? string.Empty;
        }

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            BindElements();
        }

        private void OnDisable()
        {
            if (_continueButton != null)
                _continueButton.clicked -= HandleContinueClicked;

            if (_restartButton != null)
                _restartButton.clicked -= HandleRestartClicked;
        }

        private void Update()
        {
            if (!_isTyping || _bodyLabel == null)
                return;

            if (_charactersPerSecond <= 0f)
            {
                FinishTyping();
                return;
            }

            _revealProgress += Time.deltaTime * _charactersPerSecond;
            int visibleChars = Mathf.Clamp(Mathf.FloorToInt(_revealProgress), 0, _fullLineText.Length);
            if (visibleChars != _lastVisibleLength)
            {
                _lastVisibleLength = visibleChars;
                _bodyLabel.text = _fullLineText.Substring(0, visibleChars);
            }

            if (visibleChars >= _fullLineText.Length)
                FinishTyping();
        }

        public void OnDialogStarted(DialogGraph graph)
        {
            EnsureBound();
            SetVisible(_dialogPanel, true);
            SetVisible(_restartButton, false);
            ClearChoices();
        }

        public void ShowLine(DialogLineViewModel line)
        {
            EnsureBound();
            ClearChoices();
            _pendingChoices.Clear();

            _lineHasChoices = line.HasChoices;
            _lineIsEnd = line.IsEnd;
            _fullLineText = line.Text ?? string.Empty;
            _revealProgress = 0f;
            _lastVisibleLength = -1;
            _isTyping = _charactersPerSecond > 0f && _fullLineText.Length > 0;

            if (_speakerLabel != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(line.Speaker);
                SetVisible(_speakerLabel, hasSpeaker);
                _speakerLabel.text = line.Speaker;
            }

            if (_bodyLabel != null)
                _bodyLabel.text = _isTyping ? string.Empty : _fullLineText;

            if (_continueButton != null)
            {
                SetVisible(_continueButton, true);
                _continueButton.text = _isTyping ? "Skip" : (_lineIsEnd ? "Finish" : "Continue");
            }
        }

        public void ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices)
        {
            EnsureBound();
            _pendingChoices.Clear();
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                    _pendingChoices.Add(choices[i]);
            }

            if (!_isTyping)
                RevealPendingChoices();
        }

        public void OnDialogEnded(DialogGraph graph)
        {
            EnsureBound();
            _isTyping = false;
            ClearChoices();

            SetVisible(_continueButton, false);
            SetVisible(_restartButton, true);

            if (_speakerLabel != null)
            {
                SetVisible(_speakerLabel, true);
                _speakerLabel.text = "Dialog Complete";
            }

            if (_bodyLabel != null)
                _bodyLabel.text = "The conversation has ended. Press Restart to play again.";
        }

        private void EnsureBound()
        {
            if (_dialogPanel == null)
                BindElements();
        }

        private void BindElements()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();

            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
                return;

            _dialogPanel = root.Q<VisualElement>("dialogPanel");
            _speakerLabel = root.Q<Label>("speakerLabel");
            _bodyLabel = root.Q<Label>("bodyLabel");
            _statusLabel = root.Q<Label>("statusLabel");
            _choicesContainer = root.Q<VisualElement>("choicesContainer");
            _continueButton = root.Q<Button>("continueButton");
            _restartButton = root.Q<Button>("restartButton");

            if (_continueButton != null)
            {
                _continueButton.clicked -= HandleContinueClicked;
                _continueButton.clicked += HandleContinueClicked;
            }

            if (_restartButton != null)
            {
                _restartButton.clicked -= HandleRestartClicked;
                _restartButton.clicked += HandleRestartClicked;
            }
        }

        private void HandleContinueClicked()
        {
            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            if (_runner != null && !_lineHasChoices)
                _runner.Continue();
        }

        private void HandleRestartClicked()
        {
            _onRestartRequested?.Invoke();
        }

        private void FinishTyping()
        {
            _isTyping = false;
            if (_bodyLabel != null)
                _bodyLabel.text = _fullLineText;

            if (_pendingChoices.Count > 0)
            {
                RevealPendingChoices();
            }
            else if (_continueButton != null)
            {
                SetVisible(_continueButton, true);
                _continueButton.text = _lineIsEnd ? "Finish" : "Continue";
            }
        }

        private void RevealPendingChoices()
        {
            ClearChoices();
            SetVisible(_continueButton, false);

            if (_choicesContainer == null)
                return;

            for (int i = 0; i < _pendingChoices.Count; i++)
            {
                DialogChoiceViewModel choice = _pendingChoices[i];
                int capturedIndex = choice.Index;

                var button = new Button(() => _runner?.SelectChoice(capturedIndex))
                {
                    name = $"choiceButton{choice.Index}",
                    text = $"{choice.Index + 1}. {choice.Text}"
                };
                button.AddToClassList("choice-button");
                _choicesContainer.Add(button);
            }
        }

        private void ClearChoices()
        {
            _choicesContainer?.Clear();
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element == null)
                return;

            if (visible)
                element.RemoveFromClassList(HiddenClass);
            else
                element.AddToClassList(HiddenClass);
        }
    }
}

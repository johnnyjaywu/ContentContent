using System;
using System.Collections.Generic;
using ContentContent.Dialog;
using ContentContent.Dialog.Unity;
using TMPro;
using UnityEngine;

namespace ContentContent.Dialog.Samples.UGUI
{
    /// <summary>
    /// uGUI implementation of <see cref="IDialogView"/> using TextMeshPro and UnityEngine.UI.
    /// </summary>
    public class UGUIDialogView : MonoBehaviour, IDialogView
    {
        [Header("Runner")]
        [SerializeField] private DialogRunnerBehaviour _runner;

        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private UnityEngine.UI.Button _continueButton;
        [SerializeField] private TMP_Text _continueButtonLabel;
        [SerializeField] private UnityEngine.UI.Button _restartButton;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private UnityEngine.UI.Button _choiceButtonTemplate;

        [Header("Typewriter")]
        [SerializeField] private float _charactersPerSecond = 45f;

        private readonly List<UnityEngine.UI.Button> _spawnedChoices = new List<UnityEngine.UI.Button>();
        private readonly List<DialogChoiceViewModel> _pendingChoices = new List<DialogChoiceViewModel>();

        private string _fullLineText = string.Empty;
        private float _revealProgress;
        private bool _isTyping;
        private bool _lineHasChoices;
        private bool _lineIsEnd;
        private Action _onRestartRequested;

        public void Configure(
            DialogRunnerBehaviour runner,
            GameObject panelRoot,
            TMP_Text speakerText,
            TMP_Text bodyText,
            TMP_Text statusText,
            UnityEngine.UI.Button continueButton,
            TMP_Text continueButtonLabel,
            UnityEngine.UI.Button restartButton,
            Transform choicesContainer,
            UnityEngine.UI.Button choiceButtonTemplate)
        {
            _runner = runner;
            _panelRoot = panelRoot;
            _speakerText = speakerText;
            _bodyText = bodyText;
            _statusText = statusText;
            _continueButton = continueButton;
            _continueButtonLabel = continueButtonLabel;
            _restartButton = restartButton;
            _choicesContainer = choicesContainer;
            _choiceButtonTemplate = choiceButtonTemplate;

            BindButtons();
        }

        public void SetRestartCallback(Action onRestart)
        {
            _onRestartRequested = onRestart;
        }

        public void SetStatusMessage(string message)
        {
            if (_statusText != null)
                _statusText.text = message ?? string.Empty;
        }

        private void Awake()
        {
            BindButtons();
            if (_choiceButtonTemplate != null)
                _choiceButtonTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isTyping || _bodyText == null)
                return;

            if (_charactersPerSecond <= 0f)
            {
                FinishTyping();
                return;
            }

            _revealProgress += Time.deltaTime * _charactersPerSecond;
            int visibleChars = Mathf.Clamp(Mathf.FloorToInt(_revealProgress), 0, _fullLineText.Length);
            _bodyText.maxVisibleCharacters = visibleChars;

            if (visibleChars >= _fullLineText.Length)
                FinishTyping();
        }

        public void OnDialogStarted(DialogGraph graph)
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            if (_restartButton != null)
                _restartButton.gameObject.SetActive(false);

            ClearChoices();
        }

        public void ShowLine(DialogLineViewModel line)
        {
            ClearChoices();
            _pendingChoices.Clear();

            _lineHasChoices = line.HasChoices;
            _lineIsEnd = line.IsEnd;
            _fullLineText = line.Text ?? string.Empty;
            _revealProgress = 0f;
            _isTyping = _charactersPerSecond > 0f && _fullLineText.Length > 0;

            if (_speakerText != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(line.Speaker);
                _speakerText.gameObject.SetActive(hasSpeaker);
                _speakerText.text = line.Speaker;
            }

            if (_bodyText != null)
            {
                _bodyText.text = _fullLineText;
                _bodyText.maxVisibleCharacters = _isTyping ? 0 : int.MaxValue;
            }

            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
                if (_continueButtonLabel != null)
                    _continueButtonLabel.text = _isTyping ? "Skip" : (_lineIsEnd ? "Finish" : "Continue");
            }
        }

        public void ShowChoices(IReadOnlyList<DialogChoiceViewModel> choices)
        {
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
            _isTyping = false;
            ClearChoices();

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            if (_restartButton != null)
                _restartButton.gameObject.SetActive(true);

            if (_speakerText != null)
                _speakerText.text = "Dialog Complete";

            if (_bodyText != null)
            {
                _bodyText.maxVisibleCharacters = int.MaxValue;
                _bodyText.text = "The conversation has ended. Press Restart to play again.";
            }
        }

        private void BindButtons()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinueClicked);
                _continueButton.onClick.AddListener(HandleContinueClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
                _restartButton.onClick.AddListener(HandleRestartClicked);
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
            if (_bodyText != null)
                _bodyText.maxVisibleCharacters = int.MaxValue;

            if (_pendingChoices.Count > 0)
            {
                RevealPendingChoices();
            }
            else if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
                if (_continueButtonLabel != null)
                    _continueButtonLabel.text = _lineIsEnd ? "Finish" : "Continue";
            }
        }

        private void RevealPendingChoices()
        {
            ClearChoices();
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            if (_choicesContainer == null || _choiceButtonTemplate == null)
                return;

            for (int i = 0; i < _pendingChoices.Count; i++)
            {
                DialogChoiceViewModel choice = _pendingChoices[i];
                UnityEngine.UI.Button button = Instantiate(_choiceButtonTemplate, _choicesContainer);
                button.gameObject.SetActive(true);
                button.name = $"ChoiceButton_{choice.Index}";

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"{choice.Index + 1}. {choice.Text}";

                int capturedIndex = choice.Index;
                button.onClick.AddListener(() => _runner?.SelectChoice(capturedIndex));
                _spawnedChoices.Add(button);
            }
        }

        private void ClearChoices()
        {
            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] != null)
                    Destroy(_spawnedChoices[i].gameObject);
            }

            _spawnedChoices.Clear();
        }
    }
}

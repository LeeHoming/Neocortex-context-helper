using System.Collections;
using Neocortex;
using UnityEngine;
using UnityEngine.UI;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Triggers a delayed context submission to the Neocortex smart agent when the user presses the button.
    /// </summary>
    public class ContextTimerButton : MonoBehaviour
    {
    [Header("References")]
    [SerializeField, HideInInspector] private Button triggerButton;
    [SerializeField, HideInInspector] private ContextInputFieldController contextField;
    [SerializeField, HideInInspector] private NeocortexSmartAgent smartAgent;
    [SerializeField, HideInInspector] private ConversationHistoryDisplay historyDisplay;
    [SerializeField, HideInInspector] private VoiceInputModeController voiceController;

    [Header("Timer Settings")]
    [SerializeField, Min(1f)] private float timerDurationSeconds = 30f;
    [SerializeField, Min(0f)] private float leadTimeSeconds = 3f;
    [SerializeField] private string contextSnippetFormat = "User set a {0} seconds timer and has ended.";
    [SerializeField] private string promptFormat = "The {0}-second timer has finished. Let the user know.";

        private Coroutine timerRoutine;

        private void Reset()
        {
            triggerButton = GetComponent<Button>();
            contextField = FindObjectOfType<ContextInputFieldController>();
            smartAgent = FindObjectOfType<NeocortexSmartAgent>();
            historyDisplay = FindObjectOfType<ConversationHistoryDisplay>();
            voiceController = FindObjectOfType<VoiceInputModeController>();
        }

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void OnValidate()
        {
            AutoAssignReferences();
        }

        private void OnEnable()
        {
            if (triggerButton != null)
            {
                triggerButton.onClick.AddListener(HandleButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (triggerButton != null)
            {
                triggerButton.onClick.RemoveListener(HandleButtonClicked);
            }

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private void HandleButtonClicked()
        {
            if (timerRoutine != null)
            {
                return;
            }

            timerRoutine = StartCoroutine(TimerRoutine());
        }

        private IEnumerator TimerRoutine()
        {
            if (triggerButton != null)
            {
                triggerButton.interactable = false;
            }

            var waitSeconds = Mathf.Max(0f, timerDurationSeconds - leadTimeSeconds);
            if (waitSeconds > 0f)
            {
                yield return new WaitForSeconds(waitSeconds);
            }

            SendTimerCompletionMessage();

            if (triggerButton != null)
            {
                triggerButton.interactable = true;
            }

            timerRoutine = null;
        }

        private void SendTimerCompletionMessage()
        {
            var roundedDuration = Mathf.RoundToInt(timerDurationSeconds);
            var prompt = string.Format(promptFormat, roundedDuration).Trim();
            if (string.IsNullOrEmpty(prompt))
            {
                Debug.LogWarning("[ContextTimerButton] Prompt format produced an empty message.");
                return;
            }

            var message = BuildMessageWithContext(prompt, roundedDuration);
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("[ContextTimerButton] Final message was empty.");
                return;
            }

            historyDisplay?.AddUserMessage(message);

            if (smartAgent == null)
            {
                Debug.LogWarning("[ContextTimerButton] NeocortexSmartAgent reference is missing.");
                return;
            }

            voiceController?.SetInputLock(true);
            smartAgent.TextToAudio(message);
        }

        private string BuildMessageWithContext(string prompt, int roundedDuration)
        {
            var contextSnippet = string.Format(contextSnippetFormat, roundedDuration).Trim();
            if (string.IsNullOrEmpty(contextSnippet))
            {
                return prompt;
            }

            if (contextField == null)
            {
                return $"{prompt} [{contextSnippet}]".Trim();
            }

            var inputField = contextField.InputField;
            var previousText = inputField != null ? inputField.text : string.Empty;

            contextField.AppendContext(contextSnippet);
            var combinedContext = contextField.GetContextSuffix();

            if (inputField != null)
            {
                inputField.text = previousText;
            }

            return $"{prompt}{combinedContext}".Trim();
        }

        private void AutoAssignReferences()
        {
            if (triggerButton == null)
            {
                triggerButton = GetComponent<Button>();
            }

            if (contextField == null)
            {
                contextField = FindObjectOfType<ContextInputFieldController>();
            }

            if (smartAgent == null)
            {
                smartAgent = FindObjectOfType<NeocortexSmartAgent>();
            }

            if (historyDisplay == null)
            {
                historyDisplay = FindObjectOfType<ConversationHistoryDisplay>();
            }

            if (voiceController == null)
            {
                voiceController = FindObjectOfType<VoiceInputModeController>();
            }
        }
    }
}

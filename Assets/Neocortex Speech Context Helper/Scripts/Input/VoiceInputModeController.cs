using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Manages microphone input according to the selected listening mode and passes recordings to the Deepgram service.
    /// </summary>
    public class VoiceInputModeController : MonoBehaviour
    {
        public enum ListeningMode
        {
            PushToTalk,
            ClickToToggle,
            AutomaticVoiceActivity
        }

        [Header("References")]
    [SerializeField] private DeepgramAudioReceiver audioReceiver;
        [SerializeField] private DeepgramSpeechService speechService;

        [Header("Control")]
        [SerializeField] private ListeningMode listeningMode = ListeningMode.PushToTalk;
        [SerializeField] private KeyCode activationKey = KeyCode.F1;
        [SerializeField] private bool enableKeyboardControl = true;
        [SerializeField] private bool startListeningAutomatically = true;

        [Header("UI")]
        [SerializeField] private Button voiceButton;

        [Header("Events")]
        public UnityEvent OnRecordingStarted = new();
        public UnityEvent OnRecordingStopped = new();

        private bool isRecording;
        private bool isLocked;
        private bool autoListeningArmed;
        private Coroutine autoRestartRoutine;
    private bool suppressNextRecording;

    public ListeningMode CurrentMode => listeningMode;

        private void Reset()
        {
            audioReceiver = FindObjectOfType<DeepgramAudioReceiver>();
            speechService = FindObjectOfType<DeepgramSpeechService>();
        }

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents(true);
            ConfigureListeningMode();
        }

        private void OnDisable()
        {
            SubscribeEvents(false);
        }

        private void Update()
        {
            if (!enableKeyboardControl || isLocked)
            {
                return;
            }

            switch (listeningMode)
            {
                case ListeningMode.PushToTalk:
                    HandlePushToTalkInput();
                    break;
                case ListeningMode.ClickToToggle:
                    HandleToggleInput();
                    break;
                case ListeningMode.AutomaticVoiceActivity:
                    // Automatic mode does not rely on keyboard input by default.
                    break;
            }
        }

        /// <summary>
        /// Disables or enables user initiated recordings. When locked ongoing recordings will be cancelled.
        /// </summary>
        public void SetInputLock(bool locked)
        {
            isLocked = locked;
            if (locked && isRecording)
            {
                suppressNextRecording = true;
                StopRecording();
            }

            if (!locked && listeningMode == ListeningMode.AutomaticVoiceActivity)
            {
                ArmAutomaticListening();
            }
        }

        /// <summary>
        /// Invoked by UI to signal a button press.
        /// </summary>
        public void HandleButtonPress()
        {
            if (isLocked)
            {
                return;
            }

            switch (listeningMode)
            {
                case ListeningMode.PushToTalk:
                    BeginRecording();
                    break;
                case ListeningMode.ClickToToggle:
                    ToggleRecording();
                    break;
                case ListeningMode.AutomaticVoiceActivity:
                    ToggleAutomaticListening();
                    break;
            }
        }

        /// <summary>
        /// Invoked by UI to signal a button release (used for push-to-talk).
        /// </summary>
        public void HandleButtonRelease()
        {
            if (isLocked)
            {
                return;
            }

            if (listeningMode == ListeningMode.PushToTalk)
            {
                StopRecording();
            }
        }

        private void ConfigureListeningMode()
        {
            if (audioReceiver == null)
            {
                return;
            }

            switch (listeningMode)
            {
                case ListeningMode.PushToTalk:
                    audioReceiver.PushToTalkMode = true;
                    break;
                case ListeningMode.ClickToToggle:
                case ListeningMode.AutomaticVoiceActivity:
                    audioReceiver.PushToTalkMode = false;
                    if (listeningMode == ListeningMode.AutomaticVoiceActivity && startListeningAutomatically)
                    {
                        autoListeningArmed = true;
                        ArmAutomaticListening();
                    }
                    break;
            }
        }

        private void HandlePushToTalkInput()
        {
            if (Input.GetKeyDown(activationKey))
            {
                BeginRecording();
            }

            if (Input.GetKeyUp(activationKey))
            {
                StopRecording();
            }
        }

        private void HandleToggleInput()
        {
            if (Input.GetKeyDown(activationKey))
            {
                ToggleRecording();
            }
        }

        private void BeginRecording()
        {
            if (isRecording || audioReceiver == null)
            {
                return;
            }

            audioReceiver.StartMicrophone();
            isRecording = true;
            OnRecordingStarted?.Invoke();
        }

        private void StopRecording()
        {
            if (!isRecording || audioReceiver == null)
            {
                return;
            }

            audioReceiver.StopMicrophone();
            isRecording = false;
            OnRecordingStopped?.Invoke();
        }

        private void ToggleRecording()
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                BeginRecording();
            }
        }

        private void ToggleAutomaticListening()
        {
            autoListeningArmed = !autoListeningArmed;
            if (autoListeningArmed)
            {
                ArmAutomaticListening();
            }
            else if (isRecording)
            {
                suppressNextRecording = true;
                StopRecording();
            }
        }

        private void ArmAutomaticListening()
        {
            if (audioReceiver == null || isLocked)
            {
                return;
            }

            autoListeningArmed = true;
            if (!isRecording)
            {
                audioReceiver.StartMicrophone();
                isRecording = true;
                OnRecordingStarted?.Invoke();
            }
        }

        private void HandleAudioRecorded(AudioClip clip)
        {
            isRecording = false;
            OnRecordingStopped?.Invoke();

            if (suppressNextRecording)
            {
                suppressNextRecording = false;
            }
            else if (speechService != null)
            {
                speechService.ProcessAudioClip(clip);
            }

            if (listeningMode == ListeningMode.AutomaticVoiceActivity && !isLocked && autoListeningArmed)
            {
                if (autoRestartRoutine != null)
                {
                    StopCoroutine(autoRestartRoutine);
                }

                autoRestartRoutine = StartCoroutine(RestartMicrophoneAfterFrame());
            }
        }

        private IEnumerator RestartMicrophoneAfterFrame()
        {
            yield return null;
            ArmAutomaticListening();
        }

        private void HandleRecordingFailed(string error)
        {
            isRecording = false;
            OnRecordingStopped?.Invoke();
            Debug.LogWarning($"[VoiceInputModeController] Recording failed: {error}");
        }

        private void SubscribeEvents(bool subscribe)
        {
            if (audioReceiver == null)
            {
                return;
            }

            if (subscribe)
            {
                audioReceiver.OnAudioRecorded.AddListener(HandleAudioRecorded);
                audioReceiver.OnRecordingFailed.AddListener(HandleRecordingFailed);
            }
            else
            {
                audioReceiver.OnAudioRecorded.RemoveListener(HandleAudioRecorded);
                audioReceiver.OnRecordingFailed.RemoveListener(HandleRecordingFailed);
            }

            if (voiceButton != null)
            {
                var buttonHandler = voiceButton.GetComponent<VoiceInputButton>();
                if (subscribe)
                {
                    if (buttonHandler == null)
                    {
                        buttonHandler = voiceButton.gameObject.AddComponent<VoiceInputButton>();
                    }

                    buttonHandler.Initialise(this);
                }
                else if (buttonHandler != null)
                {
                    buttonHandler.Cleanup();
                }
            }
        }

        private void ValidateReferences()
        {
            if (audioReceiver == null)
            {
                Debug.LogError("[VoiceInputModeController] DeepgramAudioReceiver reference is missing.");
            }

            if (speechService == null)
            {
                Debug.LogError("[VoiceInputModeController] DeepgramSpeechService reference is missing.");
            }
        }
    }
}

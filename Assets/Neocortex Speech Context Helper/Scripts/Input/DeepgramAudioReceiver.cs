using System;
using Neocortex;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Custom audio receiver that exposes configuration used by the helper toolkit whilst leveraging Neocortex's microphone utilities.
    /// </summary>
    public class DeepgramAudioReceiver : AudioReceiver
    {
        private const int DefaultLoopSeconds = 999;
        private const int DefaultSampleWindow = 64;
        private const int AmplitudeMultiplier = 10;

        [SerializeField, Range(0f, 1f)] private float amplitudeThreshold = 0.1f;
        [SerializeField, Range(0f, 5f)] private float silenceTimeoutSeconds = 1f;
        [SerializeField] private int sampleWindow = DefaultSampleWindow;
        [SerializeField] private int sampleFrequency = 22050;

        private AudioClip recordingClip;
        private bool initialised;
        private bool userSpeaking;

        /// <summary>
        /// Current name of the microphone device that is being recorded.
        /// </summary>
        public string SelectedMicrophone { get; private set; }

        /// <summary>
        /// When true the controller handles start/stop events manually (push-to-talk).
        /// </summary>
        public bool PushToTalkMode { get; set; }

        /// <summary>
        /// Gets or sets the amplitude threshold used for silence detection.
        /// </summary>
        public float AmplitudeThreshold
        {
            get => amplitudeThreshold;
            set => amplitudeThreshold = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Gets or sets the silence timeout used before recording automatically stops.
        /// </summary>
        public float SilenceTimeoutSeconds
        {
            get => silenceTimeoutSeconds;
            set => silenceTimeoutSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Frequency used when opening the microphone (Hz).
        /// </summary>
        public int SampleFrequency
        {
            get => sampleFrequency;
            set => sampleFrequency = Mathf.Max(8000, value);
        }

        /// <summary>
        /// Returns true while the microphone is capturing audio.
        /// </summary>
        public bool IsRecording => initialised;

        public override void StartMicrophone()
        {
            try
            {
                SelectedMicrophone = NeocortexMicrophone.devices[PlayerPrefs.GetInt(MIC_INDEX_KEY, 0)];
                recordingClip = NeocortexMicrophone.Start(SelectedMicrophone, true, DefaultLoopSeconds, sampleFrequency);
                initialised = true;
                userSpeaking = false;
                ElapsedWaitTime = 0f;
            }
            catch (Exception ex)
            {
                OnRecordingFailed?.Invoke(ex.Message);
            }
        }

        public override void StopMicrophone()
        {
            if (!initialised)
            {
                return;
            }

            NeocortexMicrophone.End(SelectedMicrophone);
            initialised = false;
            userSpeaking = false;
            DispatchRecording();
        }

        private void Update()
        {
            if (!initialised)
            {
                return;
            }

            UpdateAmplitude();

            if (PushToTalkMode)
            {
                return;
            }

            if (!userSpeaking && Amplitude > amplitudeThreshold)
            {
                userSpeaking = true;
            }

            if (!userSpeaking)
            {
                return;
            }

            if (Amplitude < amplitudeThreshold)
            {
                ElapsedWaitTime += Time.deltaTime;

                if (ElapsedWaitTime >= silenceTimeoutSeconds)
                {
                    ElapsedWaitTime = 0f;
                    StopMicrophone();
                }
            }
            else
            {
                ElapsedWaitTime = 0f;
            }
        }

        private void DispatchRecording()
        {
            if (recordingClip == null)
            {
                return;
            }

            var trimmed = recordingClip.Trim();
            if (!trimmed)
            {
                StartMicrophone();
            }
            else
            {
                OnAudioRecorded?.Invoke(trimmed);
            }
        }

        private void UpdateAmplitude()
        {
            if (recordingClip == null)
            {
                Amplitude = 0f;
                return;
            }

            var clipPosition = NeocortexMicrophone.GetPosition(SelectedMicrophone);
            var startPosition = Mathf.Max(0, clipPosition - sampleWindow);
            var audioSamples = new float[sampleWindow];
            recordingClip.GetData(audioSamples, startPosition);

            float sum = 0f;
            for (var i = 0; i < sampleWindow; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }

            Amplitude = Mathf.Clamp01(sum / sampleWindow * AmplitudeMultiplier);
        }

        private void OnDestroy()
        {
            if (initialised)
            {
                NeocortexMicrophone.End(SelectedMicrophone);
            }
        }
    }
}

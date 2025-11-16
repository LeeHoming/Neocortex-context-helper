using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Sends pre-recorded audio clips to Deepgram's REST transcription endpoint and raises transcription events.
    /// </summary>
    public class DeepgramSpeechService : MonoBehaviour
    {
        [Header("Request Settings")]
        [SerializeField] private bool punctuate = true;
        [SerializeField] private string model = "nova-2-general";
        [SerializeField] private string language = "en-US";
        [SerializeField, Tooltip("Timeout for the Deepgram request in seconds. Set to 0 to use Unity's default.")]
        private int requestTimeoutSeconds = 30;
        [SerializeField] private bool logDiagnostics = false;

        [Header("Events")]
        public UnityEvent<string> OnFinalTranscription = new();
        public UnityEvent<string> OnError = new();

        private bool isRequestInFlight;

        /// <summary>
        /// Converts the provided audio clip to PCM16 and submits it to Deepgram's pre-recorded REST API.
        /// </summary>
        public void ProcessAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                LogWarning("Attempted to process a null AudioClip.");
                return;
            }

            if (clip.samples == 0)
            {
                LogWarning("AudioClip contained no samples; skipping transcription request.");
                return;
            }

            if (isRequestInFlight)
            {
                LogWarning("A Deepgram request is already in progress. Ignoring new audio clip.");
                return;
            }

            if (!DeepgramApiKeyStore.HasValidKey())
            {
                HandleError("Deepgram API key is not configured. Use Tools > Neocortex Helper > Deepgram API Key... to set it up.");
                return;
            }

            var pcmData = ConvertToPcm16(clip);
            if (pcmData.Length == 0)
            {
                LogWarning("Audio clip conversion resulted in empty payload; skipping send.");
                return;
            }

            var wavData = ConvertPcmToWav(pcmData, clip.frequency, clip.channels);
            StartCoroutine(SendToDeepgramCoroutine(wavData));
        }

        private IEnumerator SendToDeepgramCoroutine(byte[] wavData)
        {
            isRequestInFlight = true;

            var url = BuildEndpointUrl();
            var apiKey = DeepgramApiKeyStore.GetApiKey();
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(wavData),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(0, requestTimeoutSeconds)
            };

            request.SetRequestHeader("Authorization", $"Token {apiKey}");
            request.SetRequestHeader("Content-Type", "audio/wav");

            yield return request.SendWebRequest();

            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    HandleError($"Deepgram request failed: HTTP {request.responseCode} {request.error}\n{body}");
                    yield break;
                }

                var responseJson = request.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    HandleError("Deepgram response was empty.");
                    yield break;
                }

                HandleTranscriptionResponse(responseJson);
            }
            finally
            {
                request.Dispose();
                isRequestInFlight = false;
            }
        }

        private string BuildEndpointUrl()
        {
            var builder = new StringBuilder("https://api.deepgram.com/v1/listen");
            builder.Append("?model=").Append(Uri.EscapeDataString(model));
            builder.Append("&punctuate=").Append(punctuate ? "true" : "false");

            if (!string.IsNullOrWhiteSpace(language))
            {
                builder.Append("&language=").Append(Uri.EscapeDataString(language));
            }

            return builder.ToString();
        }

        private void HandleTranscriptionResponse(string json)
        {
            try
            {
                var response = JsonUtility.FromJson<DeepgramPrerecordedResponse>(json);
                if (response?.results?.channels == null || response.results.channels.Length == 0)
                {
                    LogWarning("Deepgram response did not include any channels.");
                    return;
                }

                var alternatives = response.results.channels[0]?.alternatives;
                if (alternatives == null || alternatives.Length == 0)
                {
                    LogWarning("Deepgram response did not include transcription alternatives.");
                    return;
                }

                var transcript = alternatives[0]?.transcript;
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    LogWarning("Deepgram response transcript was empty.");
                    return;
                }

                OnFinalTranscription?.Invoke(transcript.Trim());
            }
            catch (Exception ex)
            {
                HandleError($"Failed to parse Deepgram response: {ex.Message}");
            }
        }

        private static byte[] ConvertToPcm16(AudioClip clip)
        {
            var sampleCount = clip.samples * clip.channels;
            var buffer = new float[sampleCount];
            clip.GetData(buffer, 0);

            var output = new byte[sampleCount * sizeof(short)];
            var offset = 0;

            for (var i = 0; i < sampleCount; i++)
            {
                var sample = Mathf.Clamp(buffer[i], -1f, 1f);
                var intData = (short)Mathf.Round(sample * short.MaxValue);
                output[offset++] = (byte)(intData & 0xff);
                output[offset++] = (byte)((intData >> 8) & 0xff);
            }

            return output;
        }

        private static byte[] ConvertPcmToWav(byte[] pcmData, int sampleRate, int channelCount)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            var byteRate = sampleRate * channelCount * sizeof(short);
            var blockAlign = (short)(channelCount * sizeof(short));
            var subChunk2Size = pcmData.Length;
            var chunkSize = 36 + subChunk2Size;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size for PCM
            writer.Write((short)1); // AudioFormat PCM
            writer.Write((short)channelCount);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)16); // BitsPerSample
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);
            writer.Write(pcmData);

            writer.Flush();
            return memoryStream.ToArray();
        }

        private void HandleError(string message)
        {
            LogWarning(message);
            OnError?.Invoke(message);
        }

        private void Log(string message)
        {
            if (logDiagnostics)
            {
                Debug.Log($"[DeepgramSpeechService] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (logDiagnostics)
            {
                Debug.LogWarning($"[DeepgramSpeechService] {message}");
            }
        }

        [Serializable]
        private class DeepgramPrerecordedResponse
        {
            public DeepgramResults results;
        }

        [Serializable]
        private class DeepgramResults
        {
            public DeepgramChannel[] channels;
        }

        [Serializable]
        private class DeepgramChannel
        {
            public DeepgramAlternative[] alternatives;
        }

        [Serializable]
        private class DeepgramAlternative
        {
            public string transcript;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DeepgramResponse
{
    public int[] channel_index;
    public bool is_final;
    public Channel channel;
}

[System.Serializable]
public class Channel
{
    public Alternative[] alternatives;
}

[System.Serializable]
public class Alternative
{
    public string transcript;
}

public class DeepgramInstance : MonoBehaviour
{
    [SerializeField]
    private string apiKey;

    [SerializeField]
    private TMP_Text outputText;

    private WebSocket websocket;

    private readonly StringBuilder outputBuilder = new StringBuilder();
    private bool hasNotifiedSendBlocked;

    private async void Start()
    {
        ResetOutput();

        if (outputText == null)
        {
            Debug.LogWarning("Output TMP_Text is not assigned. Logs will only appear in the console.");
        }

        var trimmedApiKey = apiKey?.Trim();
        if (string.IsNullOrEmpty(trimmedApiKey))
        {
            AppendOutput("Deepgram API key is missing. Please set it in the inspector.", LogType.Error);
            return;
        }

        AppendOutput("Connecting to Deepgram...");

        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Token " + trimmedApiKey }
        };

        websocket = new WebSocket($"wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate={AudioSettings.outputSampleRate}", headers);

        websocket.OnOpen += () =>
        {
            hasNotifiedSendBlocked = false;
            AppendOutput("Connected to Deepgram.");
        };

        websocket.OnError += (error) =>
        {
            AppendOutput("WebSocket error: " + error, LogType.Error);
        };

        websocket.OnClose += (closeCode) =>
        {
            AppendOutput("Connection closed: " + closeCode);
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            HandleIncomingMessage(message);
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            AppendOutput("Failed to connect to Deepgram: " + ex.Message, LogType.Error);
        }
    }

    private void Update()
    {
    #if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
    #endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            try
            {
                await websocket.Close();
            }
            catch (Exception ex)
            {
                AppendOutput("Error while closing WebSocket: " + ex.Message, LogType.Warning);
            }
        }
    }

    public async void ProcessAudio(byte[] audio)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            hasNotifiedSendBlocked = false;
            await websocket.Send(audio);
        }
        else if (!hasNotifiedSendBlocked)
        {
            hasNotifiedSendBlocked = true;
            AppendOutput("Attempted to send audio while WebSocket is not open.", LogType.Warning);
        }
    }

    private void HandleIncomingMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            AppendOutput("Received empty message from Deepgram.", LogType.Warning);
            return;
        }

        AppendOutput("Message: " + message);

        DeepgramResponse response = null;

        try
        {
            response = JsonUtility.FromJson<DeepgramResponse>(message);
        }
        catch (Exception ex)
        {
            AppendOutput("Failed to parse Deepgram message: " + ex.Message, LogType.Warning);
            return;
        }

        if (response == null)
        {
            AppendOutput("Deepgram response was null after parsing.", LogType.Warning);
            return;
        }

        var transcript = ExtractTranscript(response);

        if (!string.IsNullOrWhiteSpace(transcript))
        {
            var label = response.is_final ? "Final transcript" : "Partial transcript";
            AppendOutput($"{label}: {transcript.Trim()}");
        }
    }

    private static string ExtractTranscript(DeepgramResponse response)
    {
        if (response?.channel?.alternatives == null || response.channel.alternatives.Length == 0)
        {
            return string.Empty;
        }

        var alternative = response.channel.alternatives[0];
        return alternative?.transcript ?? string.Empty;
    }

    private void AppendOutput(string message, LogType logType = LogType.Log)
    {
        switch (logType)
        {
            case LogType.Error:
                Debug.LogError(message);
                break;
            case LogType.Warning:
                Debug.LogWarning(message);
                break;
            default:
                Debug.Log(message);
                break;
        }

        if (outputText == null)
        {
            return;
        }

        outputBuilder.AppendLine(message);
        outputText.text = outputBuilder.ToString();
    }

    private void ResetOutput()
    {
        outputBuilder.Clear();

        if (outputText != null)
        {
            outputText.text = string.Empty;
        }
    }
}


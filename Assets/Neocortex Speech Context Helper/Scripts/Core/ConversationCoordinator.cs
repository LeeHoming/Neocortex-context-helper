using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Neocortex;
using Neocortex.Data;
using UnityEngine;
using UnityEngine.Events;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Coordinates multi-agent conversations, managing turn order, context generation, and persistence.
    /// </summary>
    public class ConversationCoordinator : MonoBehaviour
    {
        private const string DefaultPlayerId = "player";

        [Header("Input & Output")]
        [SerializeField] private DeepgramSpeechService speechService;
        [SerializeField] private VoiceInputModeController voiceController;
        [SerializeField] private ContextInputFieldController contextField;
        [SerializeField] private ConversationHistoryDisplay historyDisplay;
        [SerializeField] private AudioSource playbackAudioSource;

        [Header("Participant Configuration")]
        [SerializeField] private string playerDisplayName = "Player";
        [SerializeField] private string playerIdentifier = DefaultPlayerId;
        [SerializeField] private ConversationRoster roster = new();
        [SerializeField] private ConversationContextBuilder contextBuilder = new();

        [Header("Conversation State")]
        [SerializeField] private ConversationLog conversationLog = new();

        [Header("Persistence")]
        [SerializeField] private string logDirectoryName = "ConversationLogs";
        [SerializeField] private bool prettyPrintLog = true;

        private readonly Dictionary<NeocortexSmartAgent, AgentSubscription> agentSubscriptions = new();

        private ConversationAgentProfile currentAgent;
        private Coroutine agentCycleRoutine;
        private Coroutine playbackRoutine;
        private bool awaitingAgentResponse;
        private string sessionIdentifier;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(playerIdentifier))
            {
                playerIdentifier = DefaultPlayerId;
            }

            sessionIdentifier = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private void Reset()
        {
            speechService = FindObjectOfType<DeepgramSpeechService>();
            voiceController = FindObjectOfType<VoiceInputModeController>();
            contextField = FindObjectOfType<ContextInputFieldController>();
            historyDisplay = FindObjectOfType<ConversationHistoryDisplay>();
            playbackAudioSource = FindObjectOfType<AudioSource>();
        }

        private void OnEnable()
        {
            if (speechService != null)
            {
                speechService.OnFinalTranscription.AddListener(HandleFinalTranscription);
                speechService.OnError.AddListener(HandleSpeechServiceError);
            }

            RegisterAgentEvents();
        }

        private void OnDisable()
        {
            if (speechService != null)
            {
                speechService.OnFinalTranscription.RemoveListener(HandleFinalTranscription);
                speechService.OnError.RemoveListener(HandleSpeechServiceError);
            }

            UnregisterAgentEvents();
        }

        /// <summary>
        /// Adds an agent to the roster for future turns.
        /// </summary>
        public void QueueAddAgent(ConversationAgentProfile profile)
        {
            roster?.QueueAddAgent(profile);
            RegisterAgent(profile);
        }

        /// <summary>
        /// Removes an agent from the roster in the next cycle.
        /// </summary>
        public void QueueRemoveAgent(string agentId)
        {
            roster?.QueueRemoveAgent(agentId);
            if (TryFindAgentById(agentId, out var smartAgent))
            {
                UnregisterAgent(smartAgent);
            }
        }

        private void HandleFinalTranscription(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            var contextSuffix = contextField != null ? contextField.GetContextSuffix() : string.Empty;
            var finalMessage = ($"{transcript}{contextSuffix}").Trim();

            if (string.IsNullOrEmpty(finalMessage))
            {
                contextField?.Clear();
                voiceController?.SetInputLock(false);
                return;
            }

            conversationLog?.AppendTurn(playerIdentifier, playerDisplayName, finalMessage);
            historyDisplay?.AddUserMessage(finalMessage);

            contextField?.Clear();

            if (agentCycleRoutine != null)
            {
                StopCoroutine(agentCycleRoutine);
            }

            agentCycleRoutine = StartCoroutine(RunAgentCycle());
        }

        private IEnumerator RunAgentCycle()
        {
            voiceController?.SetInputLock(true);

            roster?.BeginCycle();

            while (roster != null && roster.TryGetNextAgent(out var agent))
            {
                if (!IsAgentInteractive(agent))
                {
                    continue;
                }

                yield return ExecuteAgentTurn(agent);
            }

            voiceController?.SetInputLock(false);
            PersistLog();

            agentCycleRoutine = null;
        }

        private IEnumerator ExecuteAgentTurn(ConversationAgentProfile agent)
        {
            if (agent == null || agent.SmartAgent == null)
            {
                yield break;
            }

            if (!PreflightAgent(agent))
            {
                yield break;
            }

            currentAgent = agent;
            awaitingAgentResponse = true;

            var prompt = BuildPromptForAgent(agent);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                awaitingAgentResponse = false;
                currentAgent = null;
                yield break;
            }

            agent.SmartAgent.TextToAudio(prompt);

            while (awaitingAgentResponse)
            {
                yield return null;
            }
        }

        private string BuildPromptForAgent(ConversationAgentProfile agent)
        {
            if (contextBuilder == null)
            {
                return string.Empty;
            }

            return contextBuilder.BuildPrompt(agent, conversationLog, string.Empty);
        }

        private bool IsAgentInteractive(ConversationAgentProfile agent)
        {
            return agent != null && agent.ParticipatesInConversation && agent.SmartAgent != null;
        }

        private bool PreflightAgent(ConversationAgentProfile agent)
        {
            // Basic configuration checks to avoid null-reference deep inside SDK.
            var ok = true;

            if (agent == null)
            {
                return false;
            }

            if (agent.SmartAgent == null)
            {
                Debug.LogWarning($"[ConversationCoordinator] Agent '{agent.DisplayName}' is missing NeocortexSmartAgent reference.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(agent.ProjectId))
            {
                Debug.LogWarning($"[ConversationCoordinator] Agent '{agent.DisplayName}' is missing Project ID. Please assign it in the agent profile (Inspector).\n该错误常导致 SDK 内部出现 NullReference。");
                ok = false;
            }

            // If your NeocortexSmartAgent exposes a Project ID property/method, set it here.
            // Example (uncomment and adapt if available):
            // agent.SmartAgent.ProjectId = agent.ProjectId;

            return ok;
        }

        private void HandleAgentChatResponse(ConversationAgentProfile agent, ChatResponse response)
        {
            if (agent == null || response == null)
            {
                return;
            }

            var message = response.message;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            conversationLog?.AppendTurn(agent.AgentId, agent.DisplayName, message);
            historyDisplay?.AddAssistantMessage($"{agent.DisplayName}: {message}");
        }

        private void HandleAgentAudioResponse(ConversationAgentProfile agent, AudioClip clip)
        {
            if (agent == null || currentAgent == null || !ReferenceEquals(agent, currentAgent))
            {
                return;
            }

            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
            }

            playbackRoutine = StartCoroutine(PlayAudioAndCompleteTurn(clip));
        }

        private IEnumerator PlayAudioAndCompleteTurn(AudioClip clip)
        {
            if (playbackAudioSource != null && clip != null)
            {
                playbackAudioSource.Stop();
                playbackAudioSource.clip = clip;
                playbackAudioSource.Play();

                yield return new WaitForSeconds(Mathf.Max(0f, clip.length));
            }

            awaitingAgentResponse = false;
            currentAgent = null;
            playbackRoutine = null;
        }

        private void HandleAgentRequestFailed(ConversationAgentProfile agent, string error)
        {
            var agentName = agent != null ? agent.DisplayName : "Unknown";
            Debug.LogWarning($"[ConversationCoordinator] Agent '{agentName}' request failed: {error}");

            awaitingAgentResponse = false;
            currentAgent = null;
        }

        private void HandleSpeechServiceError(string error)
        {
            Debug.LogWarning($"[ConversationCoordinator] Deepgram error: {error}");
            voiceController?.SetInputLock(false);
        }

        private void PersistLog()
        {
            if (conversationLog == null)
            {
                return;
            }

            var directory = Path.Combine(Application.persistentDataPath, logDirectoryName);
            var fileName = $"conversation_{sessionIdentifier}.json";
            conversationLog.SaveToDisk(directory, fileName, prettyPrintLog);
        }

        private void RegisterAgentEvents()
        {
            if (roster == null)
            {
                return;
            }

            var baseOrder = roster.BaseOrder;
            for (var i = 0; i < baseOrder.Count; i++)
            {
                RegisterAgent(baseOrder[i]);
            }
        }

        private void RegisterAgent(ConversationAgentProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.EnsureIdentifier();

            var smartAgent = profile.SmartAgent;
            if (smartAgent == null || agentSubscriptions.ContainsKey(smartAgent))
            {
                return;
            }

            var subscription = new AgentSubscription
            {
                Profile = profile,
                ChatHandler = response => HandleAgentChatResponse(profile, response),
                AudioHandler = clip => HandleAgentAudioResponse(profile, clip),
                ErrorHandler = error => HandleAgentRequestFailed(profile, error)
            };

            smartAgent.OnChatResponseReceived.AddListener(subscription.ChatHandler);
            smartAgent.OnAudioResponseReceived.AddListener(subscription.AudioHandler);
            smartAgent.OnRequestFailed.AddListener(subscription.ErrorHandler);

            agentSubscriptions.Add(smartAgent, subscription);
        }

        private void UnregisterAgentEvents()
        {
            foreach (var pair in agentSubscriptions)
            {
                var smartAgent = pair.Key;
                var subscription = pair.Value;
                if (smartAgent == null)
                {
                    continue;
                }

                if (subscription.ChatHandler != null)
                {
                    smartAgent.OnChatResponseReceived.RemoveListener(subscription.ChatHandler);
                }

                if (subscription.AudioHandler != null)
                {
                    smartAgent.OnAudioResponseReceived.RemoveListener(subscription.AudioHandler);
                }

                if (subscription.ErrorHandler != null)
                {
                    smartAgent.OnRequestFailed.RemoveListener(subscription.ErrorHandler);
                }
            }

            agentSubscriptions.Clear();
        }

        private void UnregisterAgent(NeocortexSmartAgent smartAgent)
        {
            if (smartAgent == null || !agentSubscriptions.TryGetValue(smartAgent, out var subscription))
            {
                return;
            }

            if (subscription.ChatHandler != null)
            {
                smartAgent.OnChatResponseReceived.RemoveListener(subscription.ChatHandler);
            }

            if (subscription.AudioHandler != null)
            {
                smartAgent.OnAudioResponseReceived.RemoveListener(subscription.AudioHandler);
            }

            if (subscription.ErrorHandler != null)
            {
                smartAgent.OnRequestFailed.RemoveListener(subscription.ErrorHandler);
            }

            agentSubscriptions.Remove(smartAgent);
        }

        private bool TryFindAgentById(string agentId, out NeocortexSmartAgent smartAgent)
        {
            smartAgent = null;

            if (roster == null || !roster.TryGetProfile(agentId, out var profile))
            {
                return false;
            }

            smartAgent = profile.SmartAgent;
            return smartAgent != null;
        }

        private sealed class AgentSubscription
        {
            public ConversationAgentProfile Profile;
            public UnityAction<ChatResponse> ChatHandler;
            public UnityAction<AudioClip> AudioHandler;
            public UnityAction<string> ErrorHandler;
        }
    }
}

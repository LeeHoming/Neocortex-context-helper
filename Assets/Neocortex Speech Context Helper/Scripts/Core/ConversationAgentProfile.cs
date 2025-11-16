using System;
using Neocortex;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Serializable profile that binds a Neocortex smart agent to metadata used by the conversation system.
    /// </summary>
    [Serializable]
    public class ConversationAgentProfile
    {
        [SerializeField] private string agentId = Guid.NewGuid().ToString();
        [SerializeField] private string displayName = "Agent";
        [SerializeField] private string projectId = string.Empty;
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private bool participatesInConversation = true;
        [SerializeField] private bool openingSpeaker;
        [SerializeField, TextArea] private string initialPrompt;

        /// <summary>
        /// Unique identifier used internally when tracking turns and roster operations.
        /// </summary>
        public string AgentId => agentId;

        /// <summary>
        /// Human friendly name shown in logs and prompts.
        /// </summary>
        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        /// <summary>
        /// Deepgram Project ID (or equivalent) for this agent.
        /// </summary>
        public string ProjectId
        {
            get => projectId;
            set => projectId = value;
        }

        /// <summary>
        /// Linked smart agent component that performs LLM and TTS calls.
        /// </summary>
        public NeocortexSmartAgent SmartAgent
        {
            get => smartAgent;
            set => smartAgent = value;
        }

        /// <summary>
        /// Indicates whether the profile is currently active in the rotation.
        /// </summary>
        public bool ParticipatesInConversation
        {
            get => participatesInConversation;
            set => participatesInConversation = value;
        }

        /// <summary>
        /// When true, this agent is placed at the top of the roster for the first turn.
        /// </summary>
        public bool OpeningSpeaker
        {
            get => openingSpeaker;
            set => openingSpeaker = value;
        }

        /// <summary>
        /// Optional persona prompt injected ahead of conversation history for this agent.
        /// </summary>
        public string InitialPrompt
        {
            get => initialPrompt;
            set => initialPrompt = value;
        }

        /// <summary>
        /// Ensures the profile has a persistent identifier.
        /// </summary>
        public void EnsureIdentifier()
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                agentId = Guid.NewGuid().ToString();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Creates contextual prompts for agents based on new messages since their last speech.
    /// This avoids sending duplicate context that the cloud AI already has in memory.
    /// </summary>
    [Serializable]
    public class ConversationContextBuilder
    {
        [SerializeField] private string turnFormat = "{0}: {1}";
        [SerializeField] private string separator = "\n";
        [SerializeField] private bool includeInitialPrompt = true;

        /// <summary>
        /// Builds a prompt for the provided agent using the conversation log and optional extra context.
        /// Conditionally includes multi-participant conversation context based on the includeParticipantContext flag.
        /// </summary>
        public string BuildPrompt(ConversationAgentProfile agent, ConversationLog log, string additionalContext, string playerDisplayName = "Player", IReadOnlyList<ConversationAgentProfile> allAgents = null, bool includeParticipantContext = true)
        {
            if (agent == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            // Add multi-participant conversation context only when needed
            if (includeParticipantContext)
            {
                var conversationContext = BuildConversationContext(playerDisplayName, allAgents, agent);
                if (!string.IsNullOrWhiteSpace(conversationContext))
                {
                    builder.AppendLine(conversationContext);
                    builder.AppendLine(); // Add blank line for separation
                }
            }

            if (includeInitialPrompt && !string.IsNullOrWhiteSpace(agent.InitialPrompt))
            {
                builder.AppendLine(agent.InitialPrompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(additionalContext))
            {
                builder.AppendLine(additionalContext.Trim());
            }

            if (log != null && log.Count > 0)
            {
                // Get all turns since this agent last spoke (excluding the agent's own messages)
                var turnsSinceLastSpeak = log.GetTurnsSinceLastAgentSpeak(agent.AgentId);
                var relevantTurns = new List<ConversationTurn>();
                
                foreach (var turn in turnsSinceLastSpeak)
                {
                    // Exclude the agent's own messages from context
                    if (!string.Equals(turn.SpeakerId, agent.AgentId, StringComparison.Ordinal))
                    {
                        relevantTurns.Add(turn);
                    }
                }
                
                AppendTurns(builder, relevantTurns);
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// Builds the conversation context header that informs the agent about all participants.
        /// </summary>
        private string BuildConversationContext(string playerDisplayName, IReadOnlyList<ConversationAgentProfile> allAgents, ConversationAgentProfile currentAgent)
        {
            var participants = new List<string>();
            
            // Add player
            if (!string.IsNullOrWhiteSpace(playerDisplayName))
            {
                participants.Add(playerDisplayName);
            }
            
            // Add all other agents (excluding the current one)
            if (allAgents != null)
            {
                foreach (var otherAgent in allAgents)
                {
                    if (otherAgent != null && 
                        !string.Equals(otherAgent.AgentId, currentAgent.AgentId, StringComparison.Ordinal) &&
                        otherAgent.ParticipatesInConversation &&
                        !string.IsNullOrWhiteSpace(otherAgent.DisplayName))
                    {
                        participants.Add(otherAgent.DisplayName);
                    }
                }
            }
            
            if (participants.Count == 0)
            {
                return string.Empty;
            }
            
            var participantList = string.Join("/", participants);
            return $"[You are in a conversation with {participantList}]";
        }

        private void AppendTurns(StringBuilder builder, List<ConversationTurn> turns)
        {
            if (builder == null || turns == null || turns.Count == 0)
            {
                return;
            }

            for (var i = 0; i < turns.Count; i++)
            {
                var turn = turns[i];
                builder.AppendFormat(turnFormat, turn.SpeakerName, turn.Message);
                if (i < turns.Count - 1)
                {
                    builder.Append(separator);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Creates contextual prompts for agents based on recent conversation history.
    /// </summary>
    [Serializable]
    public class ConversationContextBuilder
    {
        [SerializeField, Min(1)] private int maxContextTurns = 6;
        [SerializeField] private string turnFormat = "{0}: {1}";
        [SerializeField] private string separator = "\n";
        [SerializeField] private bool includeInitialPrompt = true;

        /// <summary>
        /// Builds a prompt for the provided agent using the conversation log and optional extra context.
        /// </summary>
        public string BuildPrompt(ConversationAgentProfile agent, ConversationLog log, string additionalContext)
        {
            if (agent == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

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
                var relevantTurns = log.GetRecentTurns(turn => !string.Equals(turn.SpeakerId, agent.AgentId, StringComparison.Ordinal), maxContextTurns);
                AppendTurns(builder, relevantTurns);
            }

            return builder.ToString().Trim();
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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Maintains the rotation of active conversation agents, supporting dynamic enrolment per round.
    /// </summary>
    [Serializable]
    public class ConversationRoster
    {
        [SerializeField] private bool randomiseAfterOpening;
        [SerializeField] private List<ConversationAgentProfile> baseOrder = new();

        private readonly List<ConversationAgentProfile> runtimeOrder = new();
        private readonly List<ConversationAgentProfile> pendingAdds = new();
        private readonly HashSet<string> pendingRemovals = new();
        private readonly HashSet<string> knownIdentifiers = new();

        private int runtimeIndex = -1;

        public IReadOnlyList<ConversationAgentProfile> BaseOrder => baseOrder;

        /// <summary>
        /// Adds an agent to the roster for the next cycle.
        /// </summary>
        public void QueueAddAgent(ConversationAgentProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.EnsureIdentifier();

            if (knownIdentifiers.Contains(profile.AgentId) || ContainsInBaseOrder(profile.AgentId))
            {
                return;
            }

            pendingAdds.Add(profile);
        }

        /// <summary>
        /// Marks an agent for removal starting with the next cycle.
        /// </summary>
        public void QueueRemoveAgent(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return;
            }

            pendingRemovals.Add(agentId);
        }

        /// <summary>
        /// Applies pending changes and rebuilds the runtime order.
        /// </summary>
        public void BeginCycle()
        {
            ApplyPendingChanges();
            BuildRuntimeOrder();
            runtimeIndex = -1;
        }

        /// <summary>
        /// Retrieves the next agent in the current rotation.
        /// </summary>
        public bool TryGetNextAgent(out ConversationAgentProfile profile)
        {
            profile = null;

            if (runtimeOrder.Count == 0)
            {
                return false;
            }

            runtimeIndex++;
            if (runtimeIndex >= runtimeOrder.Count)
            {
                return false;
            }

            profile = runtimeOrder[runtimeIndex];
            return profile != null;
        }

        /// <summary>
        /// Resets the runtime cursor without rebuilding the order.
        /// </summary>
        public void ResetCursor()
        {
            runtimeIndex = -1;
        }

        /// <summary>
        /// Finds a profile by identifier.
        /// </summary>
        public bool TryGetProfile(string agentId, out ConversationAgentProfile profile)
        {
            profile = null;

            if (string.IsNullOrWhiteSpace(agentId))
            {
                return false;
            }

            for (var i = 0; i < baseOrder.Count; i++)
            {
                var candidate = baseOrder[i];
                if (candidate != null && string.Equals(candidate.AgentId, agentId, StringComparison.Ordinal))
                {
                    profile = candidate;
                    return true;
                }
            }

            return false;
        }

        private void ApplyPendingChanges()
        {
            if (pendingRemovals.Count > 0)
            {
                baseOrder.RemoveAll(profile => profile == null || pendingRemovals.Contains(profile.AgentId));
                foreach (var removedId in pendingRemovals)
                {
                    knownIdentifiers.Remove(removedId);
                }

                pendingRemovals.Clear();
            }

            if (pendingAdds.Count > 0)
            {
                foreach (var profile in pendingAdds)
                {
                    if (profile == null)
                    {
                        continue;
                    }

                    profile.EnsureIdentifier();
                    baseOrder.Add(profile);
                    knownIdentifiers.Add(profile.AgentId);
                }

                pendingAdds.Clear();
            }

            for (var i = 0; i < baseOrder.Count; i++)
            {
                var profile = baseOrder[i];
                if (profile == null)
                {
                    continue;
                }

                profile.EnsureIdentifier();
                knownIdentifiers.Add(profile.AgentId);
            }
        }

        private void BuildRuntimeOrder()
        {
            runtimeOrder.Clear();

            if (baseOrder.Count == 0)
            {
                return;
            }

            ConversationAgentProfile openingProfile = null;
            var remaining = new List<ConversationAgentProfile>();

            for (var i = 0; i < baseOrder.Count; i++)
            {
                var profile = baseOrder[i];
                if (profile == null || !profile.ParticipatesInConversation)
                {
                    continue;
                }

                if (profile.OpeningSpeaker && openingProfile == null)
                {
                    openingProfile = profile;
                }
                else
                {
                    remaining.Add(profile);
                }
            }

            if (openingProfile != null)
            {
                runtimeOrder.Add(openingProfile);
            }

            if (randomiseAfterOpening && remaining.Count > 1)
            {
                Shuffle(remaining);
            }

            runtimeOrder.AddRange(remaining);
        }

        private bool ContainsInBaseOrder(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return false;
            }

            for (var i = 0; i < baseOrder.Count; i++)
            {
                var profile = baseOrder[i];
                if (profile != null && string.Equals(profile.AgentId, agentId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Shuffle(List<ConversationAgentProfile> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Represents a single turn saved in the conversation history.
    /// </summary>
    [Serializable]
    public class ConversationTurn
    {
        [SerializeField] private string speakerId;
        [SerializeField] private string speakerName;
        [SerializeField, TextArea] private string message;
        [SerializeField] private long unixTimeMilliseconds;

        public ConversationTurn(string speakerId, string speakerName, string message)
        {
            this.speakerId = speakerId;
            this.speakerName = speakerName;
            this.message = message;
            unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public string SpeakerId => speakerId;
        public string SpeakerName => speakerName;
        public string Message => message;
        public long UnixTimeMilliseconds => unixTimeMilliseconds;
    }

    [Serializable]
    internal class ConversationLogData
    {
        public ConversationTurn[] turns;
    }

    /// <summary>
    /// Stores and persists the conversational history.
    /// </summary>
    [Serializable]
    public class ConversationLog
    {
        [SerializeField] private List<ConversationTurn> turns = new();

        public IReadOnlyList<ConversationTurn> Turns => turns;

        public int Count => turns.Count;

        public void AppendTurn(string speakerId, string speakerName, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var turn = new ConversationTurn(speakerId, speakerName, message.Trim());
            turns.Add(turn);
        }

        public void Clear()
        {
            turns.Clear();
        }

        /// <summary>
        /// Returns the most recent turns that match the predicate, excluding the provided agent identifier.
        /// </summary>
        public List<ConversationTurn> GetRecentTurns(Func<ConversationTurn, bool> predicate, int maxCount)
        {
            var results = new List<ConversationTurn>();

            if (maxCount <= 0)
            {
                return results;
            }

            for (var i = turns.Count - 1; i >= 0 && results.Count < maxCount; i--)
            {
                var turn = turns[i];
                if (predicate == null || predicate(turn))
                {
                    results.Add(turn);
                }
            }

            results.Reverse();
            return results;
        }

        /// <summary>
        /// Saves the log to disk as JSON.
        /// </summary>
        public void SaveToDisk(string directoryPath, string fileName, bool prettyPrint)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            Directory.CreateDirectory(directoryPath);

            var data = new ConversationLogData
            {
                turns = turns.ToArray()
            };

            var json = JsonUtility.ToJson(data, prettyPrint);
            var fullPath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(fullPath, json);
        }
    }
}

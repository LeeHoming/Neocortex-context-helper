using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Minimal conversation log used by the sample UI. Students can replace this with their own presentation layer.
    /// </summary>
    public class ConversationHistoryDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text userHistoryLabel;
        [SerializeField] private TMP_Text assistantHistoryLabel;
        [SerializeField, Min(1)] private int maxEntries = 10;

        private readonly List<string> userMessages = new();
        private readonly List<string> assistantMessages = new();
        private readonly StringBuilder stringBuilder = new();

        public void AddUserMessage(string message)
        {
            AppendMessage(userMessages, message);
            RefreshLabel(userHistoryLabel, userMessages);
        }

        public void AddAssistantMessage(string message)
        {
            AppendMessage(assistantMessages, message);
            RefreshLabel(assistantHistoryLabel, assistantMessages);
        }

        public void Clear()
        {
            userMessages.Clear();
            assistantMessages.Clear();
            RefreshLabel(userHistoryLabel, userMessages);
            RefreshLabel(assistantHistoryLabel, assistantMessages);
        }

        private void AppendMessage(List<string> list, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            list.Add(message.Trim());
            while (list.Count > maxEntries)
            {
                list.RemoveAt(0);
            }
        }

        private void RefreshLabel(TMP_Text label, List<string> messages)
        {
            if (label == null)
            {
                return;
            }

            stringBuilder.Clear();
            for (var i = 0; i < messages.Count; i++)
            {
                stringBuilder.Append(messages[i]);
                if (i < messages.Count - 1)
                {
                    stringBuilder.AppendLine();
                }
            }

            label.text = stringBuilder.ToString();
        }
    }
}

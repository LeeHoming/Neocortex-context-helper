using TMPro;
using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Manages the contextual input field that students can augment via buttons or manual typing.
    /// </summary>
    public class ContextInputFieldController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private string contextFormat = " [{0}]";

        public TMP_InputField InputField => inputField;

        private void Reset()
        {
            inputField = GetComponentInChildren<TMP_InputField>();
        }

        /// <summary>
        /// Appends a formatted context snippet to the input field for the current session.
        /// </summary>
        public void AppendContext(string context)
        {
            if (inputField == null || string.IsNullOrWhiteSpace(context))
            {
                return;
            }

            inputField.text += string.Format(contextFormat, context.Trim());
        }

        /// <summary>
        /// Returns the suffix that should be appended to the recognised transcript before sending to Neocortex.
        /// </summary>
        public string GetContextSuffix()
        {
            return inputField != null ? inputField.text : string.Empty;
        }

        /// <summary>
        /// Clears all contextual text after a message has been sent.
        /// </summary>
        public void Clear()
        {
            if (inputField != null)
            {
                inputField.text = string.Empty;
            }
        }
    }
}

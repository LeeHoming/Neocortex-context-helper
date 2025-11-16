using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Reads the caption from a TMP child and appends it to the context input field when triggered.
    /// </summary>
    public class ContextButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text contextLabel;
        [SerializeField] private ContextInputFieldController contextField;

        [SerializeField] private Button button;

        private void Reset()
        {
            contextLabel = GetComponentInChildren<TMP_Text>();
            contextField = FindObjectOfType<ContextInputFieldController>();
        }

        private void OnEnable()
        {
            button = GetComponent<Button>();
            
            if (button != null)
            {
                button.onClick.AddListener(ApplyContext);
            }
        }

        /// <summary>
        /// Appends the configured context text to the shared input field.
        /// </summary>
        public void ApplyContext()
        {
            if (contextField == null)
            {
                Debug.LogWarning("[ContextButton] Context input field controller reference is missing.");
                return;
            }

            var text = contextLabel != null ? contextLabel.text : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[ContextButton] Context label is empty; nothing to append.");
                return;
            }

            contextField.AppendContext(text);
        }
    }
}

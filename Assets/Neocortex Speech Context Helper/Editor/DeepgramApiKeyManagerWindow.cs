using UnityEditor;
using UnityEngine;

namespace NeocortexSpeechContext.Editor
{
    /// <summary>
    /// Simple editor window that lets developers configure the Deepgram API key without committing it to source control.
    /// </summary>
    public class DeepgramApiKeyManagerWindow : EditorWindow
    {
        private const string WindowTitle = "Deepgram API Key";
        private const float MinWindowWidth = 320f;
        private const float MinWindowHeight = 220f;

        private string cachedApiKey = string.Empty;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Neocortex Helper/Deepgram API Key...")]
        private static void ShowWindow()
        {
            var window = GetWindow<DeepgramApiKeyManagerWindow>(WindowTitle);
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        private void OnEnable()
        {
            cachedApiKey = DeepgramApiKeyStore.GetApiKey();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(12f);
            EditorGUILayout.LabelField("Deepgram API Key", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Keys are stored in PlayerPrefs per-user and will never be exported in a unitypackage or committed to git.", MessageType.Info);

            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("API Key", EditorStyles.label);
            cachedApiKey = EditorGUILayout.PasswordField(cachedApiKey);

            GUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Height(26f)))
                {
                    SaveApiKey();
                }

                if (GUILayout.Button("Clear", GUILayout.Height(26f), GUILayout.Width(80f)))
                {
                    ClearApiKey();
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10f);
            var statusMessage = DeepgramApiKeyStore.HasValidKey() ? "API key configured" : "No valid API key configured";
            var messageType = DeepgramApiKeyStore.HasValidKey() ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(statusMessage, messageType);

            GUILayout.Space(10f);
            if (GUILayout.Button("Open Deepgram Console", GUILayout.Height(24f)))
            {
                Application.OpenURL("https://console.deepgram.com/");
            }

            EditorGUILayout.EndScrollView();
        }

        private void SaveApiKey()
        {
            if (string.IsNullOrEmpty(cachedApiKey) || cachedApiKey.Length < 10)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Please enter a valid API key.", "OK");
                return;
            }

            DeepgramApiKeyStore.SetApiKey(cachedApiKey);
            EditorUtility.DisplayDialog(WindowTitle, "API key saved for this machine.", "OK");
        }

        private void ClearApiKey()
        {
            if (EditorUtility.DisplayDialog(WindowTitle, "Remove the stored API key for this machine?", "Remove", "Cancel"))
            {
                cachedApiKey = string.Empty;
                DeepgramApiKeyStore.Clear();
            }
        }
    }
}

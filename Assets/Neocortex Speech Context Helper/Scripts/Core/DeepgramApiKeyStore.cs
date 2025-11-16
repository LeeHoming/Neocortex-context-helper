using UnityEngine;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Provides secure storage for the Deepgram API key using PlayerPrefs so keys remain local to each developer.
    /// </summary>
    public static class DeepgramApiKeyStore
    {
        private const string PlayerPrefsKey = "neocortex_helper_deepgram_api_key";
        private const int MinimumKeyLength = 10;

        /// <summary>
        /// Retrieves the stored Deepgram API key or an empty string if none exists.
        /// </summary>
        public static string GetApiKey()
        {
            return PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        }

        /// <summary>
        /// Saves the provided API key. Passing null or empty will remove the stored key.
        /// </summary>
        public static void SetApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
            }
            else
            {
                PlayerPrefs.SetString(PlayerPrefsKey, apiKey);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns true when a valid looking API key has been configured.
        /// </summary>
        public static bool HasValidKey()
        {
            var key = GetApiKey();
            return !string.IsNullOrEmpty(key) && key.Length >= MinimumKeyLength;
        }

        /// <summary>
        /// Removes all stored credentials from PlayerPrefs.
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}

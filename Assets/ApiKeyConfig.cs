using UnityEngine;

/// <summary>
/// Secure API Key Configuration Manager
/// Uses PlayerPrefs for storage to ensure keys are not included in UnityPackage exports
/// </summary>
public static class ApiKeyConfig
{
    private const string DEEPGRAM_API_KEY = "DeepgramApiKey";
    private const string PLACEHOLDER_KEY = "INSERT_YOUR_API_KEY";
    
    /// <summary>
    /// Gets the Deepgram API key
    /// </summary>
    public static string DeepgramApiKey 
    {
        get 
        {
            return PlayerPrefs.GetString(DEEPGRAM_API_KEY, "");
        }
    }
    
    /// <summary>
    /// Sets the Deepgram API key
    /// </summary>
    /// <param name="key">The API key to store</param>
    public static void SetDeepgramApiKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            PlayerPrefs.DeleteKey(DEEPGRAM_API_KEY);
        }
        else
        {
            PlayerPrefs.SetString(DEEPGRAM_API_KEY, key);
        }
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Checks if a valid API key is configured
    /// </summary>
    /// <returns>True if a valid API key exists</returns>
    public static bool HasValidKey()
    {
        string key = DeepgramApiKey;
        return !string.IsNullOrEmpty(key) && 
               !key.Equals(PLACEHOLDER_KEY) && 
               key.Length > 10; // Basic length validation
    }
    
    /// <summary>
    /// Clears all stored API keys (for reset or uninstall)
    /// </summary>
    public static void ClearAllKeys()
    {
        PlayerPrefs.DeleteKey(DEEPGRAM_API_KEY);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Gets API key status information (for debugging)
    /// </summary>
    /// <returns>Description of the key status</returns>
    public static string GetKeyStatus()
    {
        if (!HasValidKey())
        {
            return "No valid API key configured";
        }
        
        string key = DeepgramApiKey;
        return $"API key configured (length: {key.Length}, starts with: {key.Substring(0, Mathf.Min(4, key.Length))}...)";
    }
}
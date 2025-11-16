using UnityEngine;
using UnityEditor;

/// <summary>
/// Secure Deepgram API Key Manager
/// Uses PlayerPrefs for storage to ensure keys are not included in UnityPackage exports
/// </summary>
public class ApiKeyManager : EditorWindow
{
    private string tempApiKey = "";
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Deepgram API Key Manager")]
    public static void ShowWindow()
    {
        ApiKeyManager window = GetWindow<ApiKeyManager>("Deepgram API Key Manager");
        window.minSize = new Vector2(300, 280);
        window.Show();
    }
    
    private void OnEnable()
    {
        // Load currently configured API key
        tempApiKey = ApiKeyConfig.DeepgramApiKey;
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(15);
        
        // Title
        EditorGUILayout.LabelField("Deepgram API Key Configuration", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // Deepgram API Key Section
        // Deepgram API Key Section
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("API Key:", EditorStyles.label);
        tempApiKey = EditorGUILayout.PasswordField(tempApiKey);
        
        GUILayout.Space(10);
        
        // Status Display
        if (ApiKeyConfig.HasValidKey())
        {
            EditorGUILayout.HelpBox("✓ API Key is configured", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ API Key is not configured", MessageType.Warning);
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        
        // Save Button
        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            SaveApiKey();
        }
        
        // Clear Button
        if (GUILayout.Button("Clear", GUILayout.Height(30), GUILayout.Width(80)))
        {
            ClearApiKey();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(15);
        
        // Get API Key Button
        if (GUILayout.Button("Get Deepgram API Key", GUILayout.Height(25)))
        {
            Application.OpenURL("https://console.deepgram.com/");
        }
        
        GUILayout.Space(10);
        
        // Instructions
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Note: API keys are stored locally and will NOT be included in UnityPackage exports.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SaveApiKey()
    {
        if (string.IsNullOrEmpty(tempApiKey))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a valid API key", "OK");
            return;
        }
        
        if (tempApiKey.Length < 10)
        {
            EditorUtility.DisplayDialog("Warning", "The API key seems too short. Please verify it's correct.", "OK");
            return;
        }
        
        ApiKeyConfig.SetDeepgramApiKey(tempApiKey);
        EditorUtility.DisplayDialog("Success", "API Key saved successfully!", "OK");
    }
    
    private void ClearApiKey()
    {
        if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear the API key?", "Yes", "No"))
        {
            tempApiKey = "";
            ApiKeyConfig.SetDeepgramApiKey("");
        }
    }
}
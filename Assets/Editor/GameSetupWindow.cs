#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameSetupWindow : EditorWindow
{
    private enum SetupTab { Scene, Prefabs, Networking, Lighting }
    private SetupTab currentTab = SetupTab.Scene;
    
    private GameObject playerPrefab;
    private GameObject arrowPrefab;
    private Material skyboxMaterial;
    private bool setupNetworking = true;
    private bool setupLighting = true;
    private bool setupPostProcessing = true;
    private bool createUI = true;
    private string sceneName = "DuelArena";
    
    [MenuItem("Archery Duel/Game Setup")]
    public static void ShowWindow()
    {
        GetWindow<GameSetupWindow>("Archery Duel Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Archery Duel Game Setup", EditorStyles.boldLabel);
        
        // Draw tabs
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == SetupTab.Scene, "Scene", EditorStyles.toolbarButton))
            currentTab = SetupTab.Scene;
        if (GUILayout.Toggle(currentTab == SetupTab.Prefabs, "Prefabs", EditorStyles.toolbarButton))
            currentTab = SetupTab.Prefabs;
        if (GUILayout.Toggle(currentTab == SetupTab.Networking, "Network", EditorStyles.toolbarButton))
            currentTab = SetupTab.Networking;
        if (GUILayout.Toggle(currentTab == SetupTab.Lighting, "Lighting", EditorStyles.toolbarButton))
            currentTab = SetupTab.Lighting;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Draw current tab
        switch (currentTab)
        {
            case SetupTab.Scene:
                DrawSceneTab();
                break;
            case SetupTab.Prefabs:
                DrawPrefabsTab();
                break;
            case SetupTab.Networking:
                DrawNetworkingTab();
                break;
            case SetupTab.Lighting:
                DrawLightingTab();
                break;
        }
        
        EditorGUILayout.Space(20);
        
        if (GUILayout.Button("Setup Game Scene"))
        {
            SetupGameScene();
        }
    }
    
    void DrawSceneTab()
    {
        GUILayout.Label("Scene Settings", EditorStyles.boldLabel);
        sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
        createUI = EditorGUILayout.Toggle("Create UI", createUI);
        
        EditorGUILayout.HelpBox("This will create a new scene with all required game components", MessageType.Info);
    }
    
    void DrawPrefabsTab()
    {
        GUILayout.Label("Game Prefabs", EditorStyles.boldLabel);
        playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false);
        arrowPrefab = (GameObject)EditorGUILayout.ObjectField("Arrow Prefab", arrowPrefab, typeof(GameObject), false);
        skyboxMaterial = (Material)EditorGUILayout.ObjectField("Skybox Material", skyboxMaterial, typeof(Material), false);
    }
    
    void DrawNetworkingTab()
    {
        GUILayout.Label("Networking", EditorStyles.boldLabel);
        setupNetworking = EditorGUILayout.Toggle("Setup Fusion Network", setupNetworking);
    }
    
    void DrawLightingTab()
    {
        GUILayout.Label("Visual Settings", EditorStyles.boldLabel);
        setupLighting = EditorGUILayout.Toggle("Setup Lighting", setupLighting);
        setupPostProcessing = EditorGUILayout.Toggle("Setup Post Processing", setupPostProcessing);
    }
    
    void SetupGameScene()
    {
        // Create new scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        
        // Set skybox
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;
            
        // Setup lighting
        if (setupLighting)
        {
            SetupSceneLighting();
        }
        
        // Setup post-processing
        if (setupPostProcessing)
        {
            SetupPostProcessing();
        }
        
        // Setup network
        if (setupNetworking)
        {
            SetupNetworking();
        }
        
        // Create UI
        if (createUI)
        {
            SetupGameUI();
        }
        
        // Create spawn points
        CreateSpawnPoints();
        
        // Create game manager
        CreateGameManager();
        
        // Save scene
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/" + sceneName + ".unity");
        
        EditorUtility.DisplayDialog("Setup Complete", "Game scene has been set up successfully!", "OK");
    }
    
    void SetupSceneLighting()
    {
        // Create directional light
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        light.color = new Color(1.0f, 0.95f, 0.9f);
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // Create ambient light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 1.0f;
    }
    
    void SetupPostProcessing()
    {
        // Create post-processing volume
        GameObject ppVolumeGO = new GameObject("Post Processing Volume");
        var volume = ppVolumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        
        // Create profile asset
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        string profilePath = "Assets/Settings/PostProcessingProfile.asset";
        AssetDatabase.CreateAsset(profile, profilePath);
        
        // Add effects to profile
        profile.Add<Bloom>();
        profile.Add<ColorAdjustments>();
        profile.Add<Vignette>();
        profile.Add<ChromaticAberration>();
        
        // Assign profile
        volume.profile = profile;
        
        // Save changes
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }
    
    void SetupNetworking()
    {
        // Create network manager
        GameObject networkManagerGO = new GameObject("Network Manager");
        networkManagerGO.AddComponent<NetworkLauncher>();
        
        // Create player spawner
        GameObject spawnerGO = new GameObject("Player Spawner");
        var spawner = spawnerGO.AddComponent<PlayerSpawner>();
        
        // Set player prefab if available
        if (playerPrefab != null)
        {
            var serializedObject = new SerializedObject(spawner);
            SerializedProperty playerPrefabProp = serializedObject.FindProperty("playerPrefab");
            playerPrefabProp.objectReferenceValue = playerPrefab;
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    void SetupGameUI()
    {
        // Create main UI canvas
        GameObject canvasGO = new GameObject("UI Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create UI panels
        CreateUIPanel(canvasGO.transform, "Main Menu Panel");
        CreateUIPanel(canvasGO.transform, "Game UI Panel");
        CreateUIPanel(canvasGO.transform, "Game Over Panel");
        
        // Create UI manager
        GameObject uiManagerGO = new GameObject("UI Manager");
        uiManagerGO.AddComponent<UIManager>();
    }
    
    void CreateUIPanel(Transform parent, string name)
    {
        GameObject panelGO = new GameObject(name);
        panelGO.transform.SetParent(parent, false);
        var rectTransform = panelGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        panelGO.AddComponent<CanvasGroup>();
        
        // Add basic panel background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(panelGO.transform, false);
        var bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var image = background.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);
    }
    
    void CreateSpawnPoints()
    {
        // Create spawn points container
        GameObject spawnPointsGO = new GameObject("Spawn Points");
        
        // Create 4 spawn points
        for (int i = 0; i < 4; i++)
        {
            GameObject spawnPoint = new GameObject("Spawn Point " + (i+1));
            spawnPoint.transform.SetParent(spawnPointsGO.transform);
            
            // Position in 4 corners
            float x = (i % 2 == 0) ? -10 : 10;
            float z = (i < 2) ? -10 : 10;
            spawnPoint.transform.position = new Vector3(x, 1, z);
        }
    }
    
    void CreateGameManager()
    {
        // Create game manager
        GameObject gameManagerGO = new GameObject("Game Manager");
        gameManagerGO.AddComponent<GameManager>();
        
        // Create other managers
        GameObject feedbackManagerGO = new GameObject("Feedback Manager");
        feedbackManagerGO.AddComponent<GameFeedbackManager>();
        
        GameObject visualManagerGO = new GameObject("Visual Quality Manager");
        visualManagerGO.AddComponent<VisualQualityManager>();
    }
}
#endif

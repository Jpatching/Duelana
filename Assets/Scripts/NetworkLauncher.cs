using UnityEngine;
using Fusion;
using System;
using System.Threading.Tasks;

public class NetworkLauncher : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private string sessionNameBase = "duelana-";
    [SerializeField] private int connectionAttempts = 3;
    [SerializeField] private float retryDelay = 2f;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    
    private NetworkRunner runner;
    private string currentSessionName;
    
    public static NetworkLauncher Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public async Task<bool> LaunchGame(GameManager.GameMode gameMode, string customSessionName = null)
    {
        if (loadingUI != null)
            loadingUI.SetActive(true);
            
        // Generate a session name if not provided
        currentSessionName = customSessionName ?? 
            sessionNameBase + DateTime.UtcNow.ToFileTimeUtc().ToString();
            
        // Determine game mode
        GameMode fusionGameMode = gameMode == GameManager.GameMode.SoloArena ? 
            GameMode.Single : GameMode.AutoHostOrClient;
            
        Debug.Log($"Starting game in {gameMode} mode with session: {currentSessionName}");
        
        bool success = await StartNetworkRunner(fusionGameMode);
        
        if (loadingUI != null)
            loadingUI.SetActive(false);
            
        return success;
    }
    
    private async Task<bool> StartNetworkRunner(GameMode gameMode)
    {
        // Create the runner if it doesn't exist
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }
        
        runner.ProvideInput = true;
        
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        StartGameResult result = default;
        
        try
        {
            for (int attempt = 0; attempt < connectionAttempts; attempt++)
            {
                result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = gameMode,
                    SessionName = currentSessionName,
                    SceneManager = sceneManager
                });
                
                if (result.Ok)
                    break;
                    
                Debug.LogWarning($"Connection attempt {attempt+1} failed: {result.ShutdownReason}");
                
                if (attempt < connectionAttempts - 1)
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay));
            }
            
            if (result.Ok)
            {
                Debug.Log("Network connection successful!");
                SpawnPlayer();
                return true;
            }
            else
            {
                Debug.LogError($"Failed to connect after {connectionAttempts} attempts: {result.ShutdownReason}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting network runner: {e.Message}");
            return false;
        }
    }
    
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }
        
        // Get a spawn position
        Vector3 spawnPosition = GetSpawnPosition();
        
        // Spawn the player
        runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, runner.LocalPlayer);
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Random spawn point
            int index = UnityEngine.Random.Range(0, spawnPoints.Length);
            return spawnPoints[index].position;
        }
        
        // Default spawn if no points specified
        return Vector3.up * 2; // Spawn slightly above the ground
    }
    
    public void Disconnect()
    {
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();
        }
    }

    public void RestartCurrentMatch()
    {
        if (runner != null && runner.IsRunning && GameManager.Instance != null)
        {
            // Reset the game state through the GameManager
            if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            {
                // Create a temporary implementation of the restart logic
                foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                {
                    if (player.Object != null)
                    {
                        // Respawn player
                        Vector3 spawnPosition = GetSpawnPosition();
                        player.transform.position = spawnPosition;
                        
                        // Reset player state
                        PlayerRagdoll ragdoll = player.GetComponent<PlayerRagdoll>();
                        if (ragdoll != null)
                        {
                            // Reset ragdoll state
                            ragdoll.ResetRagdoll();
                        }
                    }
                }
                
                // Reset game state in GameManager
                if (runner.IsServer)
                {
                    // This will restart the game countdown
                    GameManager.Instance.ResetGameState();
                }
            }
        }
    }
}

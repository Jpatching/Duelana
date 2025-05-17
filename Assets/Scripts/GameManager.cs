using UnityEngine;
using Fusion;
using TMPro;
using System.Collections.Generic;
using System.Linq; // For Count() extension method

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private float matchDuration = 300f; // 5 minutes
    [SerializeField] private int scoreToWin = 5;
    
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject gameplayUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI rewardText;
    
    [Header("Duelana Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private NetworkObject playerPrefab;
    
    private Dictionary<PlayerRef, int> playerScores = new Dictionary<PlayerRef, int>();
    private bool isMatchActive = false;
    private float matchTimer = 0f;
    private float solStake = 0.01f; // Default stake amount
    
    [Networked]
    private TickTimer matchTimerTick { get; set; }
      [Networked]
    public GameState CurrentState { get; private set; }
    
    public enum GameState
    {
        WaitingForPlayers,
        Countdown,
        Playing,
        GameOver
    }
    
    public enum GameMode
    {
        SoloArena,
        StakedDuel
    }
    
    private GameMode currentGameMode;
    
    // Make Runner accessible publicly with 'new' keyword
    public new NetworkRunner Runner { get; private set; }
    
    public override void Spawned()
    {
        // Store the runner reference
        Runner = Object.Runner;
        
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        CurrentState = GameState.WaitingForPlayers;
    }
    
    public void StartSoloArena()
    {
        currentGameMode = GameMode.SoloArena;
        StartGame(0.0f); // No stake for solo arena
    }
    
    public void StartStakedDuel(float stakeAmount)
    {
        currentGameMode = GameMode.StakedDuel;
        StartGame(stakeAmount);
    }
    
    private void StartGame(float stakeAmount)
    {
        solStake = stakeAmount;
        
        // Hide main menu, show loading
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (loadingUI != null) loadingUI.SetActive(true);
        
        // This would connect to the network in a real implementation
        // For the MVP, we'll just simulate it
        NetworkLauncher launcher = FindFirstObjectByType<NetworkLauncher>();
        if (launcher != null)
        {
            // The launcher will handle the connection
        }
        else
        {
            // Directly transition to gameplay for testing
            if (loadingUI != null) loadingUI.SetActive(false);
            if (gameplayUI != null) gameplayUI.SetActive(true);
            
            // Set initial game state
            if (Object.HasStateAuthority)
            {
                CurrentState = GameState.Countdown;
                matchTimerTick = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3 second countdown
            }
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;
            
        // Handle game state
        switch (CurrentState)
        {
            case GameState.WaitingForPlayers:
                // Check if we have enough players
                if (Runner.ActivePlayers.Count() >= (currentGameMode == GameMode.StakedDuel ? 2 : 1))
                {
                    CurrentState = GameState.Countdown;
                    matchTimerTick = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3 second countdown
                }
                break;
                
            case GameState.Countdown:
                if (matchTimerTick.Expired(Runner))
                {
                    CurrentState = GameState.Playing;
                    matchTimerTick = TickTimer.CreateFromSeconds(Runner, matchDuration);
                }
                break;
                
            case GameState.Playing:
                // Check win conditions
                foreach (var score in playerScores)
                {
                    if (score.Value >= scoreToWin)
                    {
                        DeclareWinner(score.Key);
                        return;
                    }
                }
                
                // Check time limit
                if (matchTimerTick.Expired(Runner))
                {
                    DetermineWinnerByScore();
                }
                break;
                
            case GameState.GameOver:
                // Wait for player to restart or exit
                break;
        }
    }
    
    void Update()
    {
        // Update UI
        if (timerText != null && CurrentState == GameState.Playing)
        {
            float remainingTime = matchTimerTick.RemainingTime(Runner) ?? 0;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        
        // Update score UI
        UpdateScoreUI();
    }
    
    private void UpdateScoreUI()
    {
        if (playerScoreText != null && opponentScoreText != null)
        {
            int localPlayerScore = 0;
            int opponentScore = 0;
            
            // Get local player's score
            if (Runner != null && Runner.LocalPlayer != null)
            {
                playerScores.TryGetValue(Runner.LocalPlayer, out localPlayerScore);
                
                // Get opponent score (first player that's not local)
                foreach (var player in Runner.ActivePlayers)
                {
                    if (player != Runner.LocalPlayer)
                    {
                        playerScores.TryGetValue(player, out opponentScore);
                        break;
                    }
                }
            }
            
            playerScoreText.text = localPlayerScore.ToString();
            opponentScoreText.text = opponentScore.ToString();
        }
    }
    
    public void RegisterKill(PlayerRef killer)
    {
        if (!Object.HasStateAuthority)
            return;
            
        // Add score to the killer
        if (!playerScores.ContainsKey(killer))
        {
            playerScores[killer] = 0;
        }
        
        playerScores[killer]++;
        
        // Check if game is over
        if (playerScores[killer] >= scoreToWin)
        {
            DeclareWinner(killer);
        }
    }
    
    // Add this method to handle target hit scoring
    public void RegisterTargetHit(PlayerRef player, int points)
    {
        if (!Object.HasStateAuthority)
            return;
            
        // Add score to the player
        if (!playerScores.ContainsKey(player))
        {
            playerScores[player] = 0;
        }
        
        playerScores[player] += points;
        
        // Check if game is over
        if (playerScores[player] >= scoreToWin)
        {
            DeclareWinner(player);
        }
    }
    
    private void DeclareWinner(PlayerRef winner)
    {
        if (!Object.HasStateAuthority)
            return;
            
        CurrentState = GameState.GameOver;
        RPC_GameOver(winner);
    }
    
    private void DetermineWinnerByScore()
    {
        if (!Object.HasStateAuthority)
            return;
            
        // Find player with highest score
        PlayerRef winner = default;
        int highestScore = -1;
        
        foreach (var score in playerScores)
        {
            if (score.Value > highestScore)
            {
                highestScore = score.Value;
                winner = score.Key;
            }
        }
        
        DeclareWinner(winner);
    }
      
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameOver(PlayerRef winner)
    {
        // Show game over UI
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);
        
        bool isLocalPlayerWinner = (Runner != null && winner == Runner.LocalPlayer);
        
        // Set game over text
        if (gameOverText != null)
        {
            gameOverText.text = isLocalPlayerWinner ? "YOU WIN!" : "YOU LOSE";
        }
        
        // Update win streaks
        if (WinStreakTracker.Instance != null)
        {
            if (isLocalPlayerWinner)
            {
                WinStreakTracker.Instance.RecordWin(Runner.LocalPlayer);
            }
            else
            {
                WinStreakTracker.Instance.RecordLoss(Runner.LocalPlayer);
            }
        }
        
        // Set reward text
        if (rewardText != null && currentGameMode == GameMode.StakedDuel)
        {
            if (isLocalPlayerWinner)
            {
                float winAmount = solStake * 2;
                
                // Add streak bonus if applicable
                if (WinStreakTracker.Instance != null)
                {
                    float baseReward = solStake * 2;
                    winAmount = WinStreakTracker.Instance.CalculateReward(baseReward, Runner.LocalPlayer);
                    float streakBonus = WinStreakTracker.Instance.GetStreakBonusPercent(Runner.LocalPlayer);
                    
                    if (streakBonus > 0)
                    {
                        rewardText.text = $"You won {winAmount:F2} SOL! (+{streakBonus}% streak bonus)";
                    }
                    else
                    {
                        rewardText.text = $"You won {winAmount:F2} SOL!";
                    }
                }
                else
                {
                    rewardText.text = $"You won {winAmount:F2} SOL!";
                }
            }
            else
            {
                rewardText.text = $"You lost {solStake} SOL";
            }
        }
        else if (rewardText != null)
        {
            if (isLocalPlayerWinner)
            {
                rewardText.text = "You earned 10 $DUELO tokens!";
            }
            else
            {
                rewardText.text = "You earned 2 $DUELO tokens";
            }
        }
    }
    
    public void ReturnToMainMenu()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        
        // Reset game state
        playerScores.Clear();
        
        // Disconnect from network (in a real implementation)
        if (Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown();
        }
    }
    
    public void RestartGame()
    {
        // Hide game over UI
        if (gameOverUI != null) gameOverUI.SetActive(false);
        
        // Reset scores
        playerScores.Clear();
        
        // Set game state back to countdown
        if (Object.HasStateAuthority)
        {
            CurrentState = GameState.Countdown;
            matchTimerTick = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3 second countdown
        }
        
        // Respawn players if needed
        RespawnPlayers();
        
        // Show gameplay UI
        if (gameplayUI != null) gameplayUI.SetActive(true);
    }
    
    public void ResetGameState()
    {
        if (!Object.HasStateAuthority)
            return;

        // Reset scores
        playerScores.Clear();
        
        // Reset game state to countdown
        CurrentState = GameState.Countdown;
        matchTimerTick = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3 second countdown
        
        // Notify clients
        RPC_GameRestarted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameRestarted()
    {
        // Hide game over UI, show gameplay UI
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(true);
        
        Debug.Log("Game has been restarted");
    }
    
    private void RespawnPlayers()
    {
        // Implementation depends on your specific player spawning system
        // This is a placeholder that you'll need to customize
        if (Runner != null && Runner.IsServer && playerPrefab != null)
        {
            foreach (var playerRef in Runner.ActivePlayers)
            {
                // Find an available spawn point
                Vector3 spawnPosition = GetRandomSpawnPosition();
                
                // Spawn player
                Runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, playerRef);
            }
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }
        return Vector3.up * 2; // Default spawn position
    }
      
    private void OnChanged()
    {
        OnGameStateChanged();
    }
    
    private void OnGameStateChanged()
    {
        // Handle UI changes based on state
        switch (CurrentState)
        {
            case GameState.WaitingForPlayers:
                Debug.Log("Waiting for players...");
                break;
                
            case GameState.Countdown:
                Debug.Log("Countdown started...");
                break;
                
            case GameState.Playing:
                Debug.Log("Match started!");
                // Hide loading screen, show gameplay UI
                if (loadingUI != null) loadingUI.SetActive(false);
                if (gameplayUI != null) gameplayUI.SetActive(true);
                break;
                
            case GameState.GameOver:
                Debug.Log("Game Over!");
                break;
        }
    }

    // Add this method to properly initialize all systems on startup
    public void InitializeGame()
    {
        // Ensure singleton is set up correctly
        if (Instance == null)
            Instance = this;
            
        // Set up initial references
        Runner = Object.Runner;
        
        // Initialize UI properly
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (loadingUI != null) loadingUI.SetActive(false);
        
        // Pre-load assets that might be needed
        Resources.PreloadAsync("Effects/ImpactEffect");
        
        Debug.Log("Game initialized successfully");
    }
}

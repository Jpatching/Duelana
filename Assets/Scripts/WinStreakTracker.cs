using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class WinStreakTracker : NetworkBehaviour
{
    [Header("Streak Settings")]
    [SerializeField] private float baseStreakBonusPercent = 2f; // 2% per win streak
    [SerializeField] private int maxStreakBonus = 10; // Cap bonus at 10 wins (20%)
    [SerializeField] private GameObject streakEffectPrefab;
    
    // Dictionary to track player win streaks
    private Dictionary<string, int> playerStreaks = new Dictionary<string, int>();
    
    // Dictionary to store player wallet addresses
    private Dictionary<PlayerRef, string> playerWallets = new Dictionary<PlayerRef, string>();
    
    // Static instance for easy access
    public static WinStreakTracker Instance { get; private set; }
    
    private void Awake()
    {
        // Simple singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Register a player wallet when they connect
    public void RegisterPlayerWallet(PlayerRef player, string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
            return;
            
        playerWallets[player] = walletAddress;
        
        // Initialize streak if needed
        if (!playerStreaks.ContainsKey(walletAddress))
        {
            playerStreaks[walletAddress] = 0;
        }
        
        Debug.Log($"Registered player {player} with wallet {walletAddress}, current streak: {playerStreaks[walletAddress]}");
    }
    
    // Record a win for the player
    public void RecordWin(PlayerRef winner)
    {
        if (!playerWallets.ContainsKey(winner))
            return;
            
        string walletAddress = playerWallets[winner];
        
        // Increment streak
        if (!playerStreaks.ContainsKey(walletAddress))
        {
            playerStreaks[walletAddress] = 1;
        }
        else
        {
            playerStreaks[walletAddress]++;
        }
        
        Debug.Log($"Player {walletAddress} now has a {playerStreaks[walletAddress]} win streak!");
        
        // Show streak effect if significant
        if (playerStreaks[walletAddress] >= 3 && streakEffectPrefab != null && Object.HasStateAuthority)
        {
            PlayerController playerController = GetPlayerByRef(winner);
            if (playerController != null)
            {
                // Spawn streak effect at player position
                Runner.Spawn(
                    streakEffectPrefab, 
                    playerController.transform.position + Vector3.up * 2f, 
                    Quaternion.identity, 
                    winner
                );
            }
        }
    }
    
    // Record a loss for the player
    public void RecordLoss(PlayerRef loser)
    {
        if (!playerWallets.ContainsKey(loser))
            return;
            
        string walletAddress = playerWallets[loser];
        
        // Reset streak
        if (playerStreaks.ContainsKey(walletAddress))
        {
            playerStreaks[walletAddress] = 0;
            Debug.Log($"Player {walletAddress} win streak reset to 0");
        }
    }
    
    // Get the current streak for a player
    public int GetStreak(PlayerRef player)
    {
        if (!playerWallets.ContainsKey(player))
            return 0;
            
        string walletAddress = playerWallets[player];
        
        if (playerStreaks.ContainsKey(walletAddress))
        {
            return playerStreaks[walletAddress];
        }
        
        return 0;
    }
    
    // Calculate bonus percentage based on streak
    public float GetStreakBonusPercent(PlayerRef player)
    {
        int streak = GetStreak(player);
        
        // Apply streak bonus (capped at max)
        int bonusStreak = Mathf.Min(streak, maxStreakBonus);
        return bonusStreak * baseStreakBonusPercent;
    }
    
    // Calculate actual SOL reward with streak bonus
    public float CalculateReward(float baseReward, PlayerRef player)
    {
        float bonusPercent = GetStreakBonusPercent(player);
        float bonusMultiplier = 1f + (bonusPercent / 100f);
        
        return baseReward * bonusMultiplier;
    }
    
    // Helper to find player object from PlayerRef
    private PlayerController GetPlayerByRef(PlayerRef playerRef)
    {
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.Object != null && player.Object.InputAuthority == playerRef)
            {
                return player;
            }
        }
        
        return null;
    }
}

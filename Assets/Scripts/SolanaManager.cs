using UnityEngine;
using System;
using TMPro;

public class SolanaManager : MonoBehaviour
{
    public static SolanaManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject walletPanel;
    [SerializeField] private TextMeshProUGUI walletAddressText;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private GameObject connectingIndicator;
    [SerializeField] private GameObject loginButton;
    
    // Mock wallet data
    private string walletAddress = "";
    private float solBalance = 0f;
    private float dueloBalance = 0f;
    
    // Wallet connection events
    public event Action<bool> OnWalletConnected;
    public event Action<float> OnBalanceUpdated;
    
    private void Awake()
    {
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
    }
    
    public bool IsWalletConnected => !string.IsNullOrEmpty(walletAddress);
    
    public float GetSolBalance()
    {
        return solBalance;
    }
    
    public float GetDueloBalance()
    {
        return dueloBalance;
    }
    
    public string GetWalletAddress()
    {
        return walletAddress;
    }
    
    public void ConnectWallet()
    {
        if (connectingIndicator != null)
            connectingIndicator.SetActive(true);
            
        if (loginButton != null)
            loginButton.SetActive(false);
            
        // Simulate connection delay
        Invoke(nameof(MockConnectWallet), 2f);
    }
    
    private void MockConnectWallet()
    {
        // Set mock data
        walletAddress = "DuE" + UnityEngine.Random.Range(100000, 999999).ToString() + "...an4x";
        solBalance = UnityEngine.Random.Range(1f, 10f);
        dueloBalance = UnityEngine.Random.Range(10f, 500f);
        
        // Update UI
        if (walletAddressText != null)
            walletAddressText.text = walletAddress;
            
        if (balanceText != null)
            balanceText.text = $"{solBalance.ToString("F2")} SOL";
            
        if (connectingIndicator != null)
            connectingIndicator.SetActive(false);
            
        // Fire events
        OnWalletConnected?.Invoke(true);
        OnBalanceUpdated?.Invoke(solBalance);
        
        Debug.Log("Wallet connected: " + walletAddress);
    }
    
    public void DisconnectWallet()
    {
        walletAddress = "";
        solBalance = 0f;
        dueloBalance = 0f;
        
        if (loginButton != null)
            loginButton.SetActive(true);
            
        // Fire events
        OnWalletConnected?.Invoke(false);
        OnBalanceUpdated?.Invoke(0f);
        
        Debug.Log("Wallet disconnected");
    }
    
    // Simulate staking SOL
    public bool StakeSOL(float amount)
    {
        if (!IsWalletConnected || amount <= 0 || amount > solBalance)
        {
            Debug.LogWarning("Cannot stake: Invalid amount or insufficient balance");
            return false;
        }
        
        // Deduct the stake amount
        solBalance -= amount;
        OnBalanceUpdated?.Invoke(solBalance);
        
        Debug.Log($"Staked {amount} SOL");
        return true;
    }
    
    // Simulate receiving SOL
    public void ReceiveSOL(float amount)
    {
        if (!IsWalletConnected || amount <= 0)
        {
            Debug.LogWarning("Cannot receive: Invalid amount or wallet not connected");
            return;
        }
        
        // Add the amount
        solBalance += amount;
        OnBalanceUpdated?.Invoke(solBalance);
        
        Debug.Log($"Received {amount} SOL");
    }
    
    // Simulate receiving DUELO tokens
    public void ReceiveDUELO(float amount)
    {
        if (!IsWalletConnected || amount <= 0)
        {
            Debug.LogWarning("Cannot receive: Invalid amount or wallet not connected");
            return;
        }
        
        // Add the amount
        dueloBalance += amount;
        
        Debug.Log($"Received {amount} DUELO tokens");
    }
}

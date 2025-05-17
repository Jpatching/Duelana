using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WalletManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject walletPanel;
    [SerializeField] private TextMeshProUGUI walletAddressText;
    [SerializeField] private TextMeshProUGUI solBalanceText;
    [SerializeField] private TextMeshProUGUI dueloBalanceText;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject successIndicator;
    
    [Header("Mock Wallet")]
    [SerializeField] private string[] mockAddressPrefixes = { "DueL", "SOLa", "FnCy", "Arch" };
    [SerializeField] private bool autoConnectWallet = false;
    
    // Events
    public event Action<bool> OnWalletConnected;
    public event Action<float> OnSolBalanceChanged;
    public event Action<float> OnDueloBalanceChanged;
    public event Action<bool> OnTransactionComplete;
    
    // Wallet state
    private bool isConnected = false;
    private string walletAddress = "";
    private float solBalance = 0f;
    private float dueloBalance = 0f;
    private bool isTransactionInProgress = false;
    
    public bool IsConnected => isConnected;
    public string WalletAddress => walletAddress;
    public float SolBalance => solBalance;
    public float DueloBalance => dueloBalance;
    
    private static WalletManager _instance;
    public static WalletManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<WalletManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("WalletManager");
                    _instance = obj.AddComponent<WalletManager>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // Set up UI
        if (connectButton != null)
            connectButton.onClick.AddListener(ConnectWallet);
            
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectWallet);
            
        // Reset UI state
        UpdateWalletUI();
        
        // Auto-connect wallet if enabled
        if (autoConnectWallet)
        {
            Invoke(nameof(ConnectWallet), 1f);
        }
    }
    
    public void ConnectWallet()
    {
        if (isConnected)
            return;
            
        StartCoroutine(ConnectWalletRoutine());
    }
    
    private System.Collections.IEnumerator ConnectWalletRoutine()
    {
        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        if (connectButton != null)
            connectButton.interactable = false;
            
        // Mock connection delay
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2.5f));
        
        // Generate mock wallet data
        GenerateMockWalletData();
        
        // Update UI
        isConnected = true;
        UpdateWalletUI();
        
        // Hide loading, show success
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        if (successIndicator != null)
        {
            successIndicator.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            successIndicator.SetActive(false);
        }
        
        // Notify listeners
        OnWalletConnected?.Invoke(true);
        OnSolBalanceChanged?.Invoke(solBalance);
        OnDueloBalanceChanged?.Invoke(dueloBalance);
        
        Debug.Log($"Wallet connected: {walletAddress} with {solBalance} SOL and {dueloBalance} DUELO");
    }
    
    public void DisconnectWallet()
    {
        if (!isConnected)
            return;
            
        // Reset wallet data
        isConnected = false;
        walletAddress = "";
        solBalance = 0f;
        dueloBalance = 0f;
        
        // Update UI
        UpdateWalletUI();
        
        // Notify listeners
        OnWalletConnected?.Invoke(false);
        
        Debug.Log("Wallet disconnected");
    }
    
    private void GenerateMockWalletData()
    {
        // Generate random wallet address
        string prefix = mockAddressPrefixes[UnityEngine.Random.Range(0, mockAddressPrefixes.Length)];
        string randomChars = UnityEngine.Random.Range(1000, 9999).ToString();
        walletAddress = prefix + randomChars + "..." + UnityEngine.Random.Range(10, 99).ToString() + "xD";
        
        // Generate random balances
        solBalance = Mathf.Round(UnityEngine.Random.Range(0.1f, 10f) * 100f) / 100f; // 2 decimal places
        dueloBalance = Mathf.Round(UnityEngine.Random.Range(10f, 1000f));
    }
    
    private void UpdateWalletUI()
    {
        if (walletAddressText != null)
            walletAddressText.text = isConnected ? walletAddress : "Not Connected";
            
        if (solBalanceText != null)
            solBalanceText.text = isConnected ? solBalance.ToString("F2") + " SOL" : "0.00 SOL";
            
        if (dueloBalanceText != null)
            dueloBalanceText.text = isConnected ? dueloBalance.ToString("F0") + " DUELO" : "0 DUELO";
            
        if (connectButton != null)
            connectButton.gameObject.SetActive(!isConnected);
            
        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(isConnected);
    }
    
    // Stake SOL for a duel
    public bool StakeSOL(float amount, Action<bool> callback = null)
    {
        if (!isConnected || amount <= 0 || amount > solBalance || isTransactionInProgress)
        {
            callback?.Invoke(false);
            return false;
        }
        
        isTransactionInProgress = true;
        StartCoroutine(ProcessStakeRoutine(amount, callback));
        return true;
    }
    
    private System.Collections.IEnumerator ProcessStakeRoutine(float amount, Action<bool> callback)
    {
        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        // Simulate network delay
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
        
        // 95% chance of success
        bool success = UnityEngine.Random.value < 0.95f;
        
        if (success)
        {
            // Deduct SOL
            solBalance -= amount;
            UpdateWalletUI();
            
            // Notify listeners
            OnSolBalanceChanged?.Invoke(solBalance);
            
            Debug.Log($"Staked {amount} SOL successfully");
        }
        else
        {
            Debug.LogWarning($"Failed to stake {amount} SOL");
        }
        
        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        // Transaction complete
        isTransactionInProgress = false;
        OnTransactionComplete?.Invoke(success);
        callback?.Invoke(success);
    }
    
    // Receive SOL or DUELO rewards
    public bool ReceiveReward(float amount, bool isDuelo, Action<bool> callback = null)
    {
        if (!isConnected || amount <= 0 || isTransactionInProgress)
        {
            callback?.Invoke(false);
            return false;
        }
        
        isTransactionInProgress = true;
        StartCoroutine(ProcessRewardRoutine(amount, isDuelo, callback));
        return true;
    }
    
    private System.Collections.IEnumerator ProcessRewardRoutine(float amount, bool isDuelo, Action<bool> callback)
    {
        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        // Simulate network delay
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f));
        
        // 98% chance of success for rewards
        bool success = UnityEngine.Random.value < 0.98f;
        
        if (success)
        {
            if (isDuelo)
            {
                // Add DUELO tokens
                dueloBalance += amount;
                OnDueloBalanceChanged?.Invoke(dueloBalance);
                Debug.Log($"Received {amount} DUELO tokens");
            }
            else
            {
                // Add SOL
                solBalance += amount;
                OnSolBalanceChanged?.Invoke(solBalance);
                Debug.Log($"Received {amount} SOL");
            }
            
            UpdateWalletUI();
        }
        else
        {
            Debug.LogWarning($"Failed to receive {amount} {(isDuelo ? "DUELO" : "SOL")}");
        }
        
        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        // Transaction complete
        isTransactionInProgress = false;
        OnTransactionComplete?.Invoke(success);
        callback?.Invoke(success);
    }
}

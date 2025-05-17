using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Main Menu Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject soloPanel;
    [SerializeField] private GameObject duelPanel;
    [SerializeField] private GameObject walletPanel;
    
    [Header("Wallet UI")]
    [SerializeField] private TextMeshProUGUI walletAddressText;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private Slider stakeSlider;
    [SerializeField] private TextMeshProUGUI stakeAmountText;
    
    [Header("Settings")]
    [SerializeField] private float minStake = 0.01f;
    [SerializeField] private float maxStake = 1.0f;
    
    // Mock wallet data
    private string mockWalletAddress = "DuEL...an4x";
    private float mockWalletBalance = 3.25f;
    private float currentStake = 0.01f;
    
    void Start()
    {
        // Show main panel initially
        ShowPanel(mainPanel);
        
        // Setup wallet UI
        UpdateWalletUI();
        
        // Setup slider
        if (stakeSlider != null)
        {
            stakeSlider.minValue = minStake;
            stakeSlider.maxValue = Mathf.Min(maxStake, mockWalletBalance);
            stakeSlider.value = currentStake;
            stakeSlider.onValueChanged.AddListener(OnStakeValueChanged);
        }
    }
    
    private void UpdateWalletUI()
    {
        if (walletAddressText != null)
            walletAddressText.text = mockWalletAddress;
            
        if (balanceText != null)
            balanceText.text = $"{mockWalletBalance} SOL";
            
        if (stakeAmountText != null)
            stakeAmountText.text = $"{currentStake} SOL";
    }
    
    private void OnStakeValueChanged(float value)
    {
        currentStake = value;
        UpdateWalletUI();
    }
    
    // Button handlers
    public void OnSoloArenaClicked()
    {
        ShowPanel(soloPanel);
    }
    
    public void OnStakedDuelClicked()
    {
        ShowPanel(duelPanel);
    }
    
    public void OnBackClicked()
    {
        ShowPanel(mainPanel);
    }
    
    public void OnConnectWalletClicked()
    {
        ShowPanel(walletPanel);
    }
    
    public void OnStartSoloClicked()
    {
        // Start solo arena mode
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartSoloArena();
        }
    }
    
    public void OnStartDuelClicked()
    {
        // Start staked duel mode
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartStakedDuel(currentStake);
        }
    }
    
    public void OnMockConnectClicked()
    {
        // Simulate successful wallet connection
        Debug.Log("Wallet connected successfully!");
        ShowPanel(duelPanel);
    }
    
    private void ShowPanel(GameObject panel)
    {
        // Hide all panels
        if (mainPanel != null) mainPanel.SetActive(false);
        if (soloPanel != null) soloPanel.SetActive(false);
        if (duelPanel != null) duelPanel.SetActive(false);
        if (walletPanel != null) walletPanel.SetActive(false);
        
        // Show the selected panel
        if (panel != null) panel.SetActive(true);
    }
}

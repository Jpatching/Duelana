using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button soloArenaButton;
    [SerializeField] private Button stakedDuelButton;
    [SerializeField] private Button settingsButton;
    
    [Header("Game UI")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject aimReticle;
    
    [Header("Stake UI")]
    [SerializeField] private GameObject stakePanel;
    [SerializeField] private Slider stakeSlider;
    [SerializeField] private TextMeshProUGUI stakeAmountText;
    [SerializeField] private Button stakeConfirmButton;
    [SerializeField] private Button stakeCancelButton;
    
    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image loadingProgressBar;
    
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI gameOverStatsText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Neon UI")]
    [SerializeField] private Color neonBlueColor = new Color(0.25f, 0.76f, 0.98f);
    [SerializeField] private Color neonPurpleColor = new Color(0.69f, 0.35f, 0.98f);
    [SerializeField] private float pulsateSpeed = 2f;
    [SerializeField] private float pulsateIntensity = 0.2f;
    
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Setup UI event handlers
        if (soloArenaButton != null)
            soloArenaButton.onClick.AddListener(OnSoloArenaClicked);
            
        if (stakedDuelButton != null)
            stakedDuelButton.onClick.AddListener(OnStakedDuelClicked);
            
        if (stakeConfirmButton != null)
            stakeConfirmButton.onClick.AddListener(OnStakeConfirmClicked);
            
        if (stakeCancelButton != null)
            stakeCancelButton.onClick.AddListener(OnStakeCancelClicked);
            
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            
        // Setup stake slider
        if (stakeSlider != null)
            stakeSlider.onValueChanged.AddListener(OnStakeValueChanged);
    }
    
    private void Start()
    {
        // Initialize UI
        ShowMainMenu();
        
        // Start neon effects
        StartCoroutine(NeonPulsateEffect());
    }
    
    private IEnumerator NeonPulsateEffect()
    {
        // Get all neon UI elements that should pulsate
        TextMeshProUGUI[] neonTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        
        while (true)
        {
            float intensity = 1f + Mathf.Sin(Time.time * pulsateSpeed) * pulsateIntensity;
            
            foreach (TextMeshProUGUI text in neonTexts)
            {
                if (text.name.Contains("Neon"))
                {
                    Color adjustedColor = text.color;
                    adjustedColor.r *= intensity;
                    adjustedColor.g *= intensity;
                    adjustedColor.b *= intensity;
                    text.color = adjustedColor;
                }
            }
            
            yield return null;
        }
    }
    
    // Show/hide UI panels
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
    
    public void ShowGameUI()
    {
        HideAllPanels();
        if (gameUI != null)
            gameUI.SetActive(true);
    }
    
    public void ShowStakePanel()
    {
        HideAllPanels();
        if (stakePanel != null)
            stakePanel.SetActive(true);
            
        // Update stake slider
        if (stakeSlider != null && SolanaManager.Instance != null)
        {
            stakeSlider.maxValue = Mathf.Min(1.0f, SolanaManager.Instance.GetSolBalance());
            stakeSlider.value = 0.01f;
            UpdateStakeText(0.01f);
        }
    }
    
    public void ShowLoadingScreen(string message = "Loading Duelana")
    {
        HideAllPanels();
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
            
        if (loadingText != null)
            loadingText.text = message;
            
        if (loadingProgressBar != null)
            loadingProgressBar.fillAmount = 0f;
            
        StartCoroutine(AnimateLoadingBar());
    }
    
    public void ShowGameOver(bool isWinner, int score, float reward)
    {
        HideAllPanels();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
            
        if (gameOverTitleText != null)
            gameOverTitleText.text = isWinner ? "VICTORY" : "DEFEAT";
            
        if (gameOverStatsText != null)
            gameOverStatsText.text = $"Score: {score}";
            
        if (rewardsText != null)
        {
            if (reward > 0)
            {
                string rewardType = reward < 1 ? "SOL" : "DUELO";
                rewardsText.text = $"Reward: {reward} {rewardType}";
            }
            else
            {
                rewardsText.text = "No rewards this time";
            }
        }
    }
    
    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (stakePanel != null) stakePanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    
    // Update UI elements
    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
    
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    public void UpdateHealth(float healthPercent)
    {
        if (healthBar != null)
            healthBar.fillAmount = Mathf.Clamp01(healthPercent);
    }
    
    public void UpdateAmmo(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"{current}/{max}";
    }
    
    public void SetAimReticleVisible(bool visible)
    {
        if (aimReticle != null)
            aimReticle.SetActive(visible);
    }
    
    private void UpdateStakeText(float value)
    {
        if (stakeAmountText != null)
            stakeAmountText.text = $"{value:F2} SOL";
    }
    
    private IEnumerator AnimateLoadingBar()
    {
        if (loadingProgressBar == null)
            yield break;
            
        loadingProgressBar.fillAmount = 0f;
        
        float duration = Random.Range(3f, 6f);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Progress follows a slight curve to look more natural
            float t = elapsed / duration;
            loadingProgressBar.fillAmount = Mathf.SmoothStep(0, 0.9f, t);
            
            yield return null;
        }
        
        // Wait at 90% to simulate final loading
        yield return new WaitForSeconds(1f);
        
        // Complete loading
        loadingProgressBar.fillAmount = 1f;
        
        yield return new WaitForSeconds(0.5f);
    }
    
    // Button event handlers
    private void OnSoloArenaClicked()
    {
        if (GameManager.Instance != null)
        {
            ShowLoadingScreen("Starting Solo Arena");
            GameManager.Instance.StartSoloArena();
        }
    }
    
    private void OnStakedDuelClicked()
    {
        if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected)
        {
            ShowStakePanel();
        }
        else
        {
            // Prompt to connect wallet
            // Code to show wallet connection UI
        }
    }
    
    private void OnStakeValueChanged(float value)
    {
        UpdateStakeText(value);
    }
    
    private void OnStakeConfirmClicked()
    {
        float stakeAmount = stakeSlider.value;
        
        if (SolanaManager.Instance != null && SolanaManager.Instance.StakeSOL(stakeAmount))
        {
            ShowLoadingScreen("Finding a challenger");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartStakedDuel(stakeAmount);
            }
        }
        else
        {
            // Show error
            // Code to show error UI
        }
    }
    
    private void OnStakeCancelClicked()
    {
        ShowMainMenu();
    }
    
    private void OnPlayAgainClicked()
    {
        if (GameManager.Instance != null)
        {
            // Show loading screen temporarily
            ShowLoadingScreen("Restarting match...");
            
            // Add a simple restart implementation if the method doesn't exist
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            if (gameUI != null)
                gameUI.SetActive(true);
                
            // Call to server to reset the game state
            NetworkLauncher launcher = FindAnyObjectByType<NetworkLauncher>();
            if (launcher != null)
            {
                launcher.RestartCurrentMatch();
            }
        }
    }
    
    private void OnMainMenuClicked()
    {
        ShowMainMenu();
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

public class DuelStakeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stakeAmountText;
    [SerializeField] private TextMeshProUGUI winAmountText;
    [SerializeField] private TextMeshProUGUI streakBonusText;
    [SerializeField] private Image stakeIcon;
    [SerializeField] private Slider stakeSlider;
    
    [Header("UI Animation")]
    [SerializeField] private float pulseDuration = 1.5f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    
    private float stakeAmount = 0.01f;
    private float minStake = 0.01f;
    private float maxStake = 1.0f;
    private int currentStreak = 0;
    private float pulseTimer = 0f;
    
    private void Start()
    {
        // Initialize UI
        UpdateUI();
        
        // Add listener to slider if assigned
        if (stakeSlider != null)
        {
            stakeSlider.minValue = minStake;
            stakeSlider.maxValue = maxStake;
            stakeSlider.value = stakeAmount;
            stakeSlider.onValueChanged.AddListener(OnStakeChanged);
        }
        
        // Check for player streak with proper null checks
        if (WinStreakTracker.Instance != null && GameManager.Instance != null && 
            GameManager.Instance.Runner != null && GameManager.Instance.Runner.LocalPlayer != default)
        {
            var player = GameManager.Instance.Runner.LocalPlayer;
            currentStreak = WinStreakTracker.Instance.GetStreak(player);
        }
    }
    
    private void Update()
    {
        // Animate UI elements
        PulseEffect();
    }
    
    public void SetStakeRange(float min, float max)
    {
        minStake = min;
        maxStake = max;
        
        if (stakeSlider != null)
        {
            stakeSlider.minValue = minStake;
            stakeSlider.maxValue = maxStake;
        }
        
        // Clamp current stake to new range
        stakeAmount = Mathf.Clamp(stakeAmount, minStake, maxStake);
        
        UpdateUI();
    }
    
    public void SetCurrentStreak(int streak)
    {
        currentStreak = streak;
        UpdateUI();
    }
    
    public float GetStakeAmount()
    {
        return stakeAmount;
    }
    
    private void OnStakeChanged(float value)
    {
        stakeAmount = value;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update stake amount text
        if (stakeAmountText != null)
        {
            stakeAmountText.text = $"{stakeAmount:F2} SOL";
        }
        
        // Calculate potential winnings
        float potentialWin = stakeAmount * 2;
        float streakBonus = 0;
        
        // Add streak bonus if applicable with proper null checks
        if (currentStreak > 0 && WinStreakTracker.Instance != null && 
            GameManager.Instance != null && GameManager.Instance.Runner != null &&
            GameManager.Instance.Runner.LocalPlayer != default)
        {
            streakBonus = WinStreakTracker.Instance.GetStreakBonusPercent(GameManager.Instance.Runner.LocalPlayer);
            potentialWin = stakeAmount * 2 * (1 + (streakBonus / 100f));
        }
        
        // Update win amount text
        if (winAmountText != null)
        {
            winAmountText.text = $"Win: {potentialWin:F2} SOL";
        }
        
        // Update streak bonus text
        if (streakBonusText != null)
        {
            if (streakBonus > 0)
            {
                streakBonusText.gameObject.SetActive(true);
                streakBonusText.text = $"+{streakBonus}% Streak Bonus!";
            }
            else
            {
                streakBonusText.gameObject.SetActive(false);
            }
        }
    }
    
    private void PulseEffect()
    {
        // Only pulse the streak bonus text if applicable
        if (streakBonusText != null && streakBonusText.gameObject.activeInHierarchy)
        {
            pulseTimer += Time.deltaTime;
            
            // Pulse size
            float pulse = 1 + Mathf.Sin((pulseTimer / pulseDuration) * Mathf.PI * 2) * pulseAmount;
            streakBonusText.transform.localScale = Vector3.one * pulse;
            
            // Pulse color
            float colorLerp = (Mathf.Sin((pulseTimer / pulseDuration) * Mathf.PI * 2) + 1) * 0.5f;
            streakBonusText.color = Color.Lerp(defaultColor, highlightColor, colorLerp);
        }
    }
}

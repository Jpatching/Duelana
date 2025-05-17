using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DueloTokenDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI tokenAmountText;
    [SerializeField] private Image tokenIcon;
    [SerializeField] private RectTransform earnedTokenEffect;
    [SerializeField] private TextMeshProUGUI earnedTokenText;
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private Color earnedTokenColor = Color.yellow;
    
    private float currentTokens = 0;
    private bool isAnimating = false;
    
    void Start()
    {
        // Get initial token amount from SolanaManager or other source
        SolanaManager solanaManager = SolanaManager.Instance;
        if (solanaManager != null)
        {
            currentTokens = solanaManager.GetDueloBalance();
        }
        
        UpdateTokenText();
        
        // Hide the earned effect initially
        if (earnedTokenEffect != null)
        {
            earnedTokenEffect.gameObject.SetActive(false);
        }
    }
    
    public void UpdateTokenAmount(float amount)
    {
        currentTokens = amount;
        UpdateTokenText();
    }
    
    public void ShowTokensEarned(float amount)
    {
        if (isAnimating || amount <= 0)
            return;
            
        // Update text
        if (tokenAmountText != null)
        {
            float newTotal = currentTokens + amount;
            tokenAmountText.text = newTotal.ToString("N0");
        }
        
        // Show animation
        if (earnedTokenEffect != null && earnedTokenText != null)
        {
            earnedTokenEffect.gameObject.SetActive(true);
            earnedTokenText.text = "+" + amount.ToString("N0");
            
            // Start animation
            isAnimating = true;
            StartCoroutine(AnimateTokenEarned());
        }
        
        // Update actual token count
        currentTokens += amount;
    }
    
    private void UpdateTokenText()
    {
        if (tokenAmountText != null)
        {
            tokenAmountText.text = currentTokens.ToString("N0");
        }
    }
    
    private System.Collections.IEnumerator AnimateTokenEarned()
    {
        float startTime = Time.time;
        Vector3 startScale = Vector3.one;
        Vector3 startPosition = earnedTokenEffect.anchoredPosition;
        Vector3 endPosition = startPosition + Vector3.up * 50; // Move up
        
        while (Time.time < startTime + animationDuration)
        {
            float elapsed = Time.time - startTime;
            float normalizedTime = elapsed / animationDuration;
            float curveValue = animationCurve.Evaluate(normalizedTime);
            
            // Move up
            earnedTokenEffect.anchoredPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
            
            // Pulse scale
            float pulse = 1 + Mathf.Sin(normalizedTime * Mathf.PI * 4) * pulseAmount * (1 - normalizedTime);
            earnedTokenEffect.localScale = startScale * pulse;
            
            // Fade out near the end
            if (normalizedTime > 0.7f)
            {
                float alpha = 1 - ((normalizedTime - 0.7f) / 0.3f);
                Color c = earnedTokenText.color;
                earnedTokenText.color = new Color(c.r, c.g, c.b, alpha);
            }
            
            yield return null;
        }
        
        // Reset for next use
        earnedTokenEffect.gameObject.SetActive(false);
        earnedTokenEffect.anchoredPosition = startPosition;
        earnedTokenEffect.localScale = startScale;
        
        // Reset color
        Color originalColor = earnedTokenText.color;
        earnedTokenText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        
        isAnimating = false;
    }
}

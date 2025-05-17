using UnityEngine;
using TMPro;

public class LoadingSpinner : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private Vector3 rotationAxis = new Vector3(0, 0, 1);
    
    [Header("Visual Effects")]
    [SerializeField] private float pulseIntensity = 0.2f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private string[] loadingMessages = {
        "Loading Duelana",
        "Warming up arrows",
        "Preparing ragdolls",
        "Setting up arenas",
        "Loading neon skybox",
        "Connecting to Solana",
        "Preparing stake pools"
    };
    
    private Vector3 originalScale;
    private float messageTimer = 0;
    private int currentMessageIndex = 0;
    private float dotTimer = 0;
    private int dotCount = 0;
    
    void Start()
    {
        originalScale = transform.localScale;
        
        if (loadingText != null)
        {
            UpdateLoadingText();
        }
    }
    
    void Update()
    {
        // Rotate the spinner
        transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime));
        
        // Pulse effect
        float pulse = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        transform.localScale = originalScale * pulse;
        
        // Update loading text with dots animation
        if (loadingText != null)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= 0.5f)
            {
                dotTimer = 0;
                dotCount = (dotCount + 1) % 4;
                UpdateLoadingText();
            }
            
            // Change loading message occasionally
            messageTimer += Time.deltaTime;
            if (messageTimer >= 3f)
            {
                messageTimer = 0;
                currentMessageIndex = (currentMessageIndex + 1) % loadingMessages.Length;
                UpdateLoadingText();
            }
        }
    }
    
    private void UpdateLoadingText()
    {
        string dots = new string('.', dotCount);
        loadingText.text = loadingMessages[currentMessageIndex] + dots;
    }
}
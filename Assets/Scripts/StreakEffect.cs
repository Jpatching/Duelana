using UnityEngine;
using Fusion;
using TMPro;

public class StreakEffect : NetworkBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float duration = 3.5f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float floatSpeed = 0.5f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private Color[] streakColors; // Different colors for different streak levels
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;
    
    private Vector3 startScale;
    private float startTime;
    private int streakCount;
    
    public override void Spawned()
    {
        startTime = Time.time;
        startScale = transform.localScale;
        
        // Get streak count from player
        if (Object.HasInputAuthority && WinStreakTracker.Instance != null)
        {
            streakCount = WinStreakTracker.Instance.GetStreak(Object.InputAuthority);
            
            // Update UI
            UpdateVisuals();
        }
    }
    
    private void UpdateVisuals()
    {
        // Set streak text
        if (streakText != null)
        {
            streakText.text = streakCount + "X STREAK!";
            
            // Pick color based on streak level
            if (streakColors != null && streakColors.Length > 0)
            {
                int colorIndex = Mathf.Min(streakCount - 3, streakColors.Length - 1);
                if (colorIndex >= 0)
                {
                    streakText.color = streakColors[colorIndex];
                }
            }
        }
        
        // Adjust particle size/color for higher streaks
        if (particles != null)
        {
            var main = particles.main;
            
            // Make particles more intense for higher streaks
            main.startSize = Mathf.Min(1f + (streakCount * 0.1f), 2f);
            
            // Set particle color if we have colors defined
            if (streakColors != null && streakColors.Length > 0)
            {
                int colorIndex = Mathf.Min(streakCount - 3, streakColors.Length - 1);
                if (colorIndex >= 0)
                {
                    main.startColor = streakColors[colorIndex];
                }
            }
            
            particles.Play();
        }
        
        // Play audio if available
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
    
    void Update()
    {
        // Rotate effect
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        
        // Pulse effect
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale * pulse;
        
        // Destroy after duration
        if (Time.time - startTime > duration)
        {
            if (Runner != null && Object.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

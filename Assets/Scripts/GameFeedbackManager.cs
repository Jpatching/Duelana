using UnityEngine;
using Fusion;

public class GameFeedbackManager : NetworkBehaviour
{
    [Header("Screen Shake")]
    [SerializeField] private float hitShakeIntensity = 0.3f;
    [SerializeField] private float hitShakeDuration = 0.3f;
    
    [Header("Time Effects")]
    [SerializeField] private float hitSlowdownFactor = 0.4f;
    [SerializeField] private float hitSlowdownDuration = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip criticalHitSound;
    [SerializeField] private AudioClip killSound;
    [SerializeField] private AudioClip ambienceSound;
    
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private float shakeTimeRemaining;
    private AudioSource audioSource;
    private float defaultTimeScale;
    private float timeScaleResetTime;
    
    public static GameFeedbackManager Instance { get; private set; }
    
    public override void Spawned()
    {
        // Set singleton
        Instance = this;
        
        // Initialize
        mainCamera = Camera.main;
        if (mainCamera != null)
            originalCameraPos = mainCamera.transform.localPosition;
            
        audioSource = GetComponent<AudioSource>();
        defaultTimeScale = Time.timeScale;
        
        // Play ambient sound
        if (audioSource != null && ambienceSound != null)
        {
            audioSource.clip = ambienceSound;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
    }
    
    public void RegisterHit(Vector3 position, bool isCritical = false, bool isKill = false)
    {
        // Screen shake
        if (mainCamera != null)
            StartCoroutine(ShakeCamera(isCritical ? hitShakeIntensity * 1.5f : hitShakeIntensity));
            
        // Time effect
        if (isKill || isCritical)
            StartCoroutine(SlowTimeEffect());
            
        // Play sound
        if (audioSource != null)
        {
            if (isKill && killSound != null)
                audioSource.PlayOneShot(killSound);
            else if (isCritical && criticalHitSound != null)
                audioSource.PlayOneShot(criticalHitSound);
            else if (hitSound != null)
                audioSource.PlayOneShot(hitSound);
        }
    }
    
    private System.Collections.IEnumerator ShakeCamera(float intensity)
    {
        shakeTimeRemaining = hitShakeDuration;
        
        while (shakeTimeRemaining > 0)
        {
            // Reduce shake over time
            float percentComplete = 1.0f - (shakeTimeRemaining / hitShakeDuration);
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
            
            // Generate random shake
            float x = Random.Range(-1f, 1f) * intensity * damper;
            float y = Random.Range(-1f, 1f) * intensity * damper;
            
            // Apply shake
            if (mainCamera != null)
                mainCamera.transform.localPosition = originalCameraPos + new Vector3(x, y, 0);
            
            shakeTimeRemaining -= Time.deltaTime;
            yield return null;
        }
        
        // Reset position
        if (mainCamera != null)
            mainCamera.transform.localPosition = originalCameraPos;
    }
    
    private System.Collections.IEnumerator SlowTimeEffect()
    {
        // Slow down time
        Time.timeScale = hitSlowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
        
        // Wait
        yield return new WaitForSecondsRealtime(hitSlowdownDuration);
        
        // Return to normal
        Time.timeScale = defaultTimeScale;
        Time.fixedDeltaTime = 0.02f;
    }
    
    private void OnDestroy()
    {
        // Make sure time scale is reset
        Time.timeScale = 1f;
    }
}

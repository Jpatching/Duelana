using UnityEngine;
using Fusion;
using System.Collections;
using UnityEngine.Rendering;

public class KillCamManager : NetworkBehaviour
{
    public static KillCamManager Instance { get; private set; }
    
    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.25f;
    [SerializeField] private float slowMotionDuration = 2.5f;
    [SerializeField] private AnimationCurve slowMotionCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
      [Header("Camera Effects")]
    [SerializeField] private VolumeProfile killCamProfile;
    [SerializeField] private float zoomFactor = 1.5f;
    [SerializeField] private float cameraTiltAngle = 5f;
    [SerializeField] private float shakeIntensity = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip killCamSound;
    [SerializeField] private float killCamVolume = 0.8f;
      private float defaultFixedDeltaTime;
    private bool isKillCamActive = false;
    private Camera mainCamera;
    private Volume postProcessVolume;
    private AudioSource audioSource;
    
    private float originalFOV;
    private Quaternion originalRotation;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }
    
    public override void Spawned()
    {
        mainCamera = Camera.main;
      
        // Setup post-processing volume with proper error checking
        if (killCamProfile != null)
        {
            try {
                GameObject volumeObj = new GameObject("KillCam Post Process Volume");
                postProcessVolume = volumeObj.AddComponent<Volume>();
                postProcessVolume.profile = killCamProfile;
                postProcessVolume.isGlobal = true;
                postProcessVolume.weight = 0;
                
                // Initially disabled
                volumeObj.SetActive(false);
            }
            catch (System.Exception e) {
                Debug.LogError($"Error setting up KillCam post processing: {e.Message}");
            }
        }
        
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; // 2D sound
    }
    
    public void ActivateKillCam(Vector3 killerPosition, Vector3 victimPosition)
    {
        if (isKillCamActive)
            return;
            
        isKillCamActive = true;
        
        // Store original camera settings
        if (mainCamera != null)
        {
            originalFOV = mainCamera.fieldOfView;
            originalRotation = mainCamera.transform.rotation;
        }
        
        // Play kill cam sound
        if (audioSource != null && killCamSound != null)
        {
            audioSource.clip = killCamSound;
            audioSource.volume = killCamVolume;
            audioSource.Play();
        }
        
        // Activate post-processing
        if (postProcessVolume != null)
        {
            postProcessVolume.gameObject.SetActive(true);
            StartCoroutine(AnimatePostProcessWeight(0, 1, 0.3f));
        }
        
        // Start slow motion
        StartCoroutine(SlowMotionEffect());
        
        // Point camera in the right direction
        if (mainCamera != null)
        {
            Vector3 killDirection = (victimPosition - killerPosition).normalized;
            mainCamera.transform.rotation = Quaternion.LookRotation(killDirection);
            
            // Apply tilt
            mainCamera.transform.Rotate(0, 0, cameraTiltAngle);
            
            // Zoom in
            StartCoroutine(AnimateFOV(originalFOV, originalFOV / zoomFactor, 0.3f));
            
            // Add camera shake
            StartCoroutine(CameraShake());
        }
    }
    
    private IEnumerator SlowMotionEffect()
    {
        float startTime = Time.unscaledTime;
        float endTime = startTime + slowMotionDuration;
        
        // Slow down time
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = defaultFixedDeltaTime * slowMotionFactor;
        
        while (Time.unscaledTime < endTime)
        {
            float progress = (Time.unscaledTime - startTime) / slowMotionDuration;
            float curveValue = slowMotionCurve.Evaluate(progress);
            
            // Gradually return to normal time toward the end
            if (progress > 0.7f)
            {
                float normalizeProgress = (progress - 0.7f) / 0.3f; // Normalize from 0.7-1.0 to 0-1
                float newTimeScale = Mathf.Lerp(slowMotionFactor, 1f, normalizeProgress);
                Time.timeScale = newTimeScale;
                Time.fixedDeltaTime = defaultFixedDeltaTime * newTimeScale;
            }
            
            yield return null;
        }
        
        EndKillCam();
    }
    
    private IEnumerator AnimatePostProcessWeight(float from, float to, float duration)
    {
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;
        
        while (Time.unscaledTime < endTime)
        {
            float progress = (Time.unscaledTime - startTime) / duration;
            postProcessVolume.weight = Mathf.Lerp(from, to, progress);
            
            yield return null;
        }
        
        postProcessVolume.weight = to;
    }
    
    private IEnumerator AnimateFOV(float from, float to, float duration)
    {
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;
        
        while (Time.unscaledTime < endTime)
        {
            float progress = (Time.unscaledTime - startTime) / duration;
            mainCamera.fieldOfView = Mathf.Lerp(from, to, progress);
            
            yield return null;
        }
        
        mainCamera.fieldOfView = to;
    }
    
    private IEnumerator CameraShake()
    {
        float endTime = Time.unscaledTime + slowMotionDuration * 0.7f;
        Vector3 originalPosition = mainCamera.transform.localPosition;
        
        while (Time.unscaledTime < endTime)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            mainCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalPosition;
    }
    
    private void EndKillCam()
    {
        // Reset time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        
        // Reset camera
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = originalFOV;
            mainCamera.transform.rotation = originalRotation;
        }
        
        // Disable post-processing
        if (postProcessVolume != null)
        {
            StartCoroutine(AnimatePostProcessWeight(1, 0, 0.5f));
            StartCoroutine(DisablePostProcessAfterDelay(0.5f));
        }
        
        isKillCamActive = false;
    }
    
    private IEnumerator DisablePostProcessAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (postProcessVolume != null)
        {
            postProcessVolume.gameObject.SetActive(false);
        }
    }
}

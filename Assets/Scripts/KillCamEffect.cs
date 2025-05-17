using UnityEngine;
using Fusion;
using System.Collections;

public class KillCamEffect : NetworkBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float slowMotionFactor = 0.2f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private float slowMoDuration = 3f;
    
    [Header("Visual Effects")]
    [SerializeField] private Color killCamColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private AnimationCurve saturationCurve;
    [SerializeField] private AnimationCurve vignetteCurve;
    
    [Header("Audio")]
    [SerializeField] private AudioClip killCamSound;
    [SerializeField] private float audioSlowPitch = 0.5f;
    
    private Material killCamMaterial;
    private float defaultFixedDeltaTime;
    private bool isActive = false;
    private AudioSource audioSource;
    
    // Shader property IDs
    private int colorOverlayId;
    private int saturationId;
    private int vignetteId;
    
    private static readonly int MAX_ACTIVE_KILLCAMS = 1;
    private static int activeKillCams = 0;
    
    private void Awake()
    {
        // Cache the default fixed delta time
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        
        // Set up audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D sound
        
        // Create the kill cam material for post-processing
        Shader shader = Shader.Find("Hidden/KillCamEffect");
        if (shader != null)
        {
            killCamMaterial = new Material(shader);
            
            // Cache property IDs
            colorOverlayId = Shader.PropertyToID("_ColorOverlay");
            saturationId = Shader.PropertyToID("_Saturation");
            vignetteId = Shader.PropertyToID("_VignetteIntensity");
        }
        else
        {
            Debug.LogError("KillCam shader not found!");
        }
    }
    
    public void ActivateKillCam(Vector3 killPosition)
    {
        if (isActive || activeKillCams >= MAX_ACTIVE_KILLCAMS)
            return;
            
        StartCoroutine(KillCamSequence(killPosition));
    }
    
    private IEnumerator KillCamSequence(Vector3 killPosition)
    {
        isActive = true;
        activeKillCams++;
        
        // Store original values
        float originalTimeScale = Time.timeScale;
        float originalFixedDeltaTime = Time.fixedDeltaTime;
        
        // Enable post-processing effect
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        
        // Play sound
        if (killCamSound != null && audioSource != null)
        {
            audioSource.clip = killCamSound;
            audioSource.pitch = audioSlowPitch;
            audioSource.Play();
        }
        
        // Transition to slow motion
        float transitionTime = 0f;
        while (transitionTime < transitionDuration)
        {
            float t = transitionTime / transitionDuration;
            Time.timeScale = Mathf.Lerp(originalTimeScale, slowMotionFactor, t);
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
            
            // Update visual effect parameters
            if (killCamMaterial != null)
            {
                killCamMaterial.SetColor(colorOverlayId, new Color(
                    killCamColor.r, 
                    killCamColor.g, 
                    killCamColor.b, 
                    killCamColor.a * t
                ));
                killCamMaterial.SetFloat(saturationId, Mathf.Lerp(1f, 0.5f, saturationCurve.Evaluate(t)));
                killCamMaterial.SetFloat(vignetteId, vignetteCurve.Evaluate(t));
            }
            
            transitionTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Hold slow motion
        yield return new WaitForSecondsRealtime(slowMoDuration);
        
        // Transition back to normal
        transitionTime = 0f;
        while (transitionTime < transitionDuration)
        {
            float t = transitionTime / transitionDuration;
            Time.timeScale = Mathf.Lerp(slowMotionFactor, 1f, t);
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
            
            // Update visual effect parameters
            if (killCamMaterial != null)
            {
                killCamMaterial.SetColor(colorOverlayId, new Color(
                    killCamColor.r, 
                    killCamColor.g, 
                    killCamColor.b, 
                    killCamColor.a * (1f - t)
                ));
                killCamMaterial.SetFloat(saturationId, Mathf.Lerp(0.5f, 1f, t));
                killCamMaterial.SetFloat(vignetteId, vignetteCurve.Evaluate(1f - t));
            }
            
            transitionTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Reset time to normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        
        // Disable effect
        isActive = false;
        activeKillCams--;
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (isActive && killCamMaterial != null)
        {
            Graphics.Blit(source, destination, killCamMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
    
    private void OnDestroy()
    {
        // Ensure time scale is reset if destroyed during slow-mo
        if (isActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
            activeKillCams--;
        }
        
        // Clean up material
        if (killCamMaterial != null)
        {
            Destroy(killCamMaterial);
        }
    }
}

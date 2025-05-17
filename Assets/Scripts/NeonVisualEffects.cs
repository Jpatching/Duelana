using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class NeonVisualEffects : MonoBehaviour
{
    [Header("Global Effects")]
    [SerializeField] private VolumeProfile defaultProfile;
    [SerializeField] private VolumeProfile intenseBattleProfile;
    [SerializeField] private float transitionSpeed = 1.0f;
    
    [Header("Environment")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Color[] skyColors;
    [SerializeField] private float skyColorChangeInterval = 30f;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Color[] lightColors;
    
    [Header("Grid Effect")]
    [SerializeField] private Material gridMaterial;
    [SerializeField] private float gridPulseSpeed = 1f;
    [SerializeField] private float gridEmissionMin = 0.8f;
    [SerializeField] private float gridEmissionMax = 1.5f;
    
    private Volume ppVolume;
    private float currentProfileBlend = 0f;
    private int currentColorIndex = 0;
    private float lastColorChange = 0f;
    
    void Start()
    {
        // Setup post processing
        GameObject volumeObj = new GameObject("Neon Post Process Volume");
        ppVolume = volumeObj.AddComponent<Volume>();
        ppVolume.isGlobal = true;
        ppVolume.weight = 1.0f;
        ppVolume.profile = defaultProfile;
        
        // Apply skybox if provided
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            
            // Set initial skybox color
            if (skyColors != null && skyColors.Length > 0)
            {
                skyboxMaterial.SetColor("_SkyTint", skyColors[0]);
                
                // Match directional light to sky color
                if (directionalLight != null && lightColors.Length > 0)
                {
                    directionalLight.color = lightColors[0];
                }
            }
        }
    }
    
    void Update()
    {
        // Handle grid effect (if material exists)
        if (gridMaterial != null)
        {
            float emission = Mathf.Lerp(gridEmissionMin, gridEmissionMax, 
                                       (Mathf.Sin(Time.time * gridPulseSpeed) + 1) * 0.5f);
                                       
            gridMaterial.SetFloat("_EmissionIntensity", emission);
        }
        
        // Slowly change skybox colors over time
        if (skyboxMaterial != null && skyColors != null && skyColors.Length > 1)
        {
            if (Time.time - lastColorChange > skyColorChangeInterval)
            {
                lastColorChange = Time.time;
                currentColorIndex = (currentColorIndex + 1) % skyColors.Length;
                
                // Set new colors
                StartCoroutine(TransitionSkyboxColor(currentColorIndex));
            }
        }
    }
    
    // Call this when battle gets more intense
    public void SetBattleIntensity(float intensity)
    {
        // Clamp intensity between 0-1
        intensity = Mathf.Clamp01(intensity);
        
        // Set target profile blend
        StopAllCoroutines();
        StartCoroutine(TransitionProfileBlend(intensity));
    }
    
    private System.Collections.IEnumerator TransitionProfileBlend(float targetBlend)
    {
        float startBlend = currentProfileBlend;
        float startTime = Time.time;
        float duration = Mathf.Abs(targetBlend - startBlend) / transitionSpeed;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            currentProfileBlend = Mathf.Lerp(startBlend, targetBlend, t);
            
            // Apply blended profile
            ApplyBlendedProfile();
            
            yield return null;
        }
        
        currentProfileBlend = targetBlend;
        ApplyBlendedProfile();
    }
    
    private void ApplyBlendedProfile()
    {
        if (ppVolume == null || defaultProfile == null || intenseBattleProfile == null)
            return;
            
        // Create a blended profile
        VolumeProfile blendedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // Blend settings from both profiles
        BlendProfiles(defaultProfile, intenseBattleProfile, blendedProfile, currentProfileBlend);
        
        // Apply the blended profile
        ppVolume.profile = blendedProfile;
    }
    
    private void BlendProfiles(VolumeProfile profileA, VolumeProfile profileB, 
                             VolumeProfile blendedProfile, float blendFactor)
    {
        // Simply use the appropriate profile based on blend factor
        // This is a simple approach since we can't guarantee which components exist
        VolumeProfile sourceProfile = (blendFactor < 0.5f) ? profileA : profileB;
        
        // Copy all components from source profile to blended profile
        foreach (var component in sourceProfile.components)
        {
            if (component == null) continue;
            
            // Clone the component
            var clone = VolumeComponent.Instantiate(component);
            blendedProfile.components.Add(clone);
        }
    }
    
    private System.Collections.IEnumerator TransitionSkyboxColor(int targetColorIndex)
    {
        if (skyboxMaterial == null || skyColors == null || targetColorIndex >= skyColors.Length)
            yield break;
            
        Color startColor = skyboxMaterial.GetColor("_SkyTint");
        Color targetColor = skyColors[targetColorIndex];
        
        Color startLightColor = directionalLight != null ? directionalLight.color : Color.white;
        Color targetLightColor = lightColors.Length > targetColorIndex ? lightColors[targetColorIndex] : Color.white;
        
        float startTime = Time.time;
        float duration = 5.0f; // 5 seconds to transition
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            
            // Apply easing
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            
            // Set skybox color
            skyboxMaterial.SetColor("_SkyTint", Color.Lerp(startColor, targetColor, t));
            
            // Set light color
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            }
            
            yield return null;
        }
        
        // Ensure we end at exact target color
        skyboxMaterial.SetColor("_SkyTint", targetColor);
        
        if (directionalLight != null)
        {
            directionalLight.color = targetLightColor;
        }
    }
}

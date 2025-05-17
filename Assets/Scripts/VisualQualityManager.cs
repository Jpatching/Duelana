using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualQualityManager : MonoBehaviour
{
    [SerializeField] private Volume globalVolume;
    [SerializeField] private VolumeProfile highQualityProfile;
    [SerializeField] private VolumeProfile mediumQualityProfile;
    [SerializeField] private VolumeProfile lowQualityProfile;
    
    [Header("Quality Settings")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private bool enableShadows = true;
    [SerializeField] private int shadowResolution = 2048;
    [SerializeField] private Material skyboxMaterial;
    
    public enum QualityLevel { Low, Medium, High }
    
    void Start()
    {
        // Default to medium quality
        SetQualityLevel(QualityLevel.Medium);
    }
    
    public void SetQualityLevel(QualityLevel level)
    {
        // Apply volume profile
        if (globalVolume != null)
        {
            switch (level)
            {
                case QualityLevel.High:
                    globalVolume.profile = highQualityProfile;
                    SetShadowQuality(true, shadowResolution);
                    SetAntiAliasing(8);
                    break;
                    
                case QualityLevel.Medium:
                    globalVolume.profile = mediumQualityProfile;
                    SetShadowQuality(enableShadows, shadowResolution / 2);
                    SetAntiAliasing(4);
                    break;
                    
                case QualityLevel.Low:
                    globalVolume.profile = lowQualityProfile;
                    SetShadowQuality(false, shadowResolution / 4);
                    SetAntiAliasing(2);
                    break;
            }
        }
        
        // Set quality settings
        QualitySettings.shadows = level == QualityLevel.Low ? 
                                ShadowQuality.Disable : ShadowQuality.All;
                                
        QualitySettings.softParticles = level != QualityLevel.Low;
        
        // Apply skybox settings
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Glossiness", level == QualityLevel.Low ? 0.3f : 0.7f);
            skyboxMaterial.SetFloat("_Intensity", level == QualityLevel.Low ? 0.5f : 1.0f);
        }
    }
    
    private void SetShadowQuality(bool enabled, int resolution)
    {
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.shadows = enabled ? LightShadows.Soft : LightShadows.None;
            mainDirectionalLight.shadowResolution = 
                (UnityEngine.Rendering.LightShadowResolution)(resolution / 512);
        }
    }
    
    private void SetAntiAliasing(int level)
    {
        QualitySettings.antiAliasing = level;
    }
}

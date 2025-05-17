using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class NeonPostProcess : MonoBehaviour
{
    [Header("Neon Glow")]
    [SerializeField] private Shader neonShader;
    [SerializeField] private Color neonColor = new Color(0.6f, 0.2f, 1.0f, 1.0f);
    [Range(0f, 1f)]
    [SerializeField] private float neonStrength = 0.5f;
    [Range(0f, 5f)]
    [SerializeField] private float bloomIntensity = 1.5f;
    [Range(0f, 10f)]
    [SerializeField] private float bloomThreshold = 0.85f;
    [Range(1, 8)]
    [SerializeField] private int bloomIterations = 4;
    
    [Header("Color Grading")]
    [SerializeField] private Color shadowsTint = new Color(0.1f, 0.0f, 0.2f);
    [SerializeField] private Color highlightsTint = new Color(0.8f, 0.5f, 1.0f);
    [Range(0f, 2f)]
    [SerializeField] private float contrast = 1.1f;
    [Range(0f, 2f)]
    [SerializeField] private float saturation = 1.2f;
    
    [Header("Chromatic Aberration")]
    [Range(0f, 5f)]
    [SerializeField] private float chromaticAberrationStrength = 1.0f;
    [SerializeField] private bool enableChromaticAberration = true;
    
    [Header("Scan Lines")]
    [SerializeField] private bool enableScanLines = true;
    [Range(0f, 1f)]
    [SerializeField] private float scanLineIntensity = 0.2f;
    [Range(50f, 500f)]
    [SerializeField] private float scanLineDensity = 200f;
    
    private Material neonMaterial;
    
    // Shader property IDs
    private int neonColorId;
    private int neonStrengthId;
    private int bloomIntensityId;
    private int bloomThresholdId;
    private int shadowsTintId;
    private int highlightsTintId;
    private int contrastId;
    private int saturationId;
    private int chromaticStrengthId;
    private int enableChromaticId;
    private int enableScanLinesId;
    private int scanLineIntensityId;
    private int scanLineDensityId;
    
    private void OnEnable()
    {
        if (neonShader == null)
        {
            neonShader = Shader.Find("Hidden/NeonEffect");
        }
        
        if (neonShader != null && neonMaterial == null)
        {
            neonMaterial = new Material(neonShader);
            neonMaterial.hideFlags = HideFlags.HideAndDontSave;
            
            // Cache property IDs
            neonColorId = Shader.PropertyToID("_NeonColor");
            neonStrengthId = Shader.PropertyToID("_NeonStrength");
            bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
            bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
            shadowsTintId = Shader.PropertyToID("_ShadowsTint");
            highlightsTintId = Shader.PropertyToID("_HighlightsTint");
            contrastId = Shader.PropertyToID("_Contrast");
            saturationId = Shader.PropertyToID("_Saturation");
            chromaticStrengthId = Shader.PropertyToID("_ChromaticStrength");
            enableChromaticId = Shader.PropertyToID("_EnableChromatic");
            enableScanLinesId = Shader.PropertyToID("_EnableScanLines");
            scanLineIntensityId = Shader.PropertyToID("_ScanLineIntensity");
            scanLineDensityId = Shader.PropertyToID("_ScanLineDensity");
        }
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (neonMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Update material properties
        neonMaterial.SetColor(neonColorId, neonColor);
        neonMaterial.SetFloat(neonStrengthId, neonStrength);
        neonMaterial.SetFloat(bloomIntensityId, bloomIntensity);
        neonMaterial.SetFloat(bloomThresholdId, bloomThreshold);
        neonMaterial.SetInt("_BloomIterations", bloomIterations);
        neonMaterial.SetColor(shadowsTintId, shadowsTint);
        neonMaterial.SetColor(highlightsTintId, highlightsTint);
        neonMaterial.SetFloat(contrastId, contrast);
        neonMaterial.SetFloat(saturationId, saturation);
        neonMaterial.SetFloat(chromaticStrengthId, chromaticAberrationStrength);
        neonMaterial.SetInt(enableChromaticId, enableChromaticAberration ? 1 : 0);
        neonMaterial.SetInt(enableScanLinesId, enableScanLines ? 1 : 0);
        neonMaterial.SetFloat(scanLineIntensityId, scanLineIntensity);
        neonMaterial.SetFloat(scanLineDensityId, scanLineDensity);
        
        // Apply effect
        Graphics.Blit(source, destination, neonMaterial);
    }
    
    private void OnDisable()
    {
        if (neonMaterial != null)
        {
            DestroyImmediate(neonMaterial);
            neonMaterial = null;
        }
    }
}

using UnityEngine;

public class PlayerVisualEnhancer : MonoBehaviour
{
    [Header("Material Effects")]
    [SerializeField] private Renderer[] playerRenderers;
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private float emissionIntensity = 0.4f;
    
    [Header("Trail Effects")]
    [SerializeField] private TrailRenderer movementTrail;
    [SerializeField] private float trailActiveSpeed = 4f;
    [SerializeField] private Color trailColor = Color.cyan;
    
    [Header("Footsteps")]
    [SerializeField] private ParticleSystem footstepParticles;
    [SerializeField] private Transform footstepSource;
    
    private CharacterController characterController;
    private Vector3 lastPosition;
    private float currentSpeed;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        lastPosition = transform.position;
        
        // Set player material color
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                // Set base color
                renderer.material.color = playerColor;
                
                // Enable emission
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", playerColor * emissionIntensity);
            }
        }
        
        // Configure trail
        if (movementTrail != null)
        {
            movementTrail.startColor = trailColor;
            movementTrail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0);
            movementTrail.emitting = false;
        }
    }
    
    void Update()
    {
        // Calculate speed
        currentSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        
        // Update trail based on movement
        if (movementTrail != null)
        {
            movementTrail.emitting = currentSpeed > trailActiveSpeed;
        }
        
        // Spawn footstep effects when moving on ground
        if (footstepParticles != null && footstepSource != null && 
            characterController != null && characterController.isGrounded && 
            currentSpeed > 1.0f)
        {
            if (!footstepParticles.isPlaying)
            {
                footstepParticles.Play();
            }
            
            footstepSource.position = transform.position - new Vector3(0, characterController.height/2, 0);
        }
        else if (footstepParticles != null && footstepParticles.isPlaying)
        {
            footstepParticles.Stop();
        }
    }
    
    public void PlayHitFlash()
    {
        StartCoroutine(FlashCoroutine());
    }
    
    private System.Collections.IEnumerator FlashCoroutine()
    {
        Color flashColor = Color.white;
        
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetColor("_EmissionColor", flashColor * 2);
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetColor("_EmissionColor", playerColor * emissionIntensity);
            }
        }
    }
}

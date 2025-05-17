using UnityEngine;
using Fusion;

public class Arrow : NetworkBehaviour
{
    [Header("Arrow Properties")]
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float impactForce = 10f;
    [SerializeField] private float damageAmount = 25f;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GameObject impactEffectPrefab;
    
    [Header("Killcam")]
    [SerializeField] private float killcamSlowMotionFactor = 0.3f;
    [SerializeField] private float killcamDuration = 3f;
    
    private Rigidbody rb;
    private Collider arrowCollider;
    private float spawnTime;
    private bool hasHit = false;
    private float defaultFixedDeltaTime;
    
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        arrowCollider = GetComponent<Collider>();
        spawnTime = Time.time;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        
        if (!Object.HasStateAuthority)
        {
            return;
        }
    }
    
    void Update()
    {
        if (!hasHit && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            // Align arrow with velocity direction
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
        
        // Destroy arrow after lifetime
        if (Time.time - spawnTime > lifetime && Runner != null && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // Stop arrow movement
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Disable collider
        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }
        
        // Stop trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
        
        // Attach to hit object
        transform.SetParent(collision.transform);
        
        // Check if hit a player
        PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            // Apply damage and ragdoll physics
            ApplyDamageAndRagdoll(hitPlayer, collision.contacts[0].point);
        }
        
        // Spawn impact effect
        if (impactEffectPrefab != null && Runner != null && Object.HasStateAuthority)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Quaternion hitRotation = Quaternion.LookRotation(collision.contacts[0].normal);
            
            Runner.Spawn(impactEffectPrefab, hitPoint, hitRotation, Object.InputAuthority);
        }
    }
    
    private void ApplyDamageAndRagdoll(PlayerController hitPlayer, Vector3 hitPoint)
    {
        if (Runner == null || !Object.HasStateAuthority) return;
        
        // Check if we have ragdoll component
        PlayerRagdoll ragdoll = hitPlayer.GetComponent<PlayerRagdoll>();
        if (ragdoll != null)
        {
            Vector3 forceDirection = rb.linearVelocity.normalized;
            ragdoll.ActivateRagdoll(hitPoint, forceDirection * impactForce);
            
            // Trigger kill cam
            StartKillCam();
            
            // Award score to shooter
            GameManager.Instance.RegisterKill(Object.InputAuthority);
        }
    }
      private void StartKillCam()
    {
        // Check if we have a KillCamManager
        KillCamManager killCamManager = KillCamManager.Instance;
        if (killCamManager != null)
        {
            // Get positions for killer and victim
            Vector3 shooterPosition = Vector3.zero;
            Vector3 victimPosition = transform.position;
            
            // Try to get the shooter's position
            PlayerController shooter = GetShooterPlayer();
            if (shooter != null)
            {
                shooterPosition = shooter.transform.position;
            }
            
            // Activate the kill cam
            killCamManager.ActivateKillCam(shooterPosition, victimPosition);
        }
        else
        {
            // Fall back to simple slow-mo if no kill cam manager
            Time.timeScale = killcamSlowMotionFactor;
            Time.fixedDeltaTime = defaultFixedDeltaTime * killcamSlowMotionFactor;
            
            // Return to normal speed after duration
            Invoke(nameof(EndKillCam), killcamDuration * killcamSlowMotionFactor);
        }
    }
    
    private PlayerController GetShooterPlayer()
    {
        if (Runner == null)
            return null;
            
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.Object != null && player.Object.InputAuthority == Object.InputAuthority)
            {
                return player;
            }
        }
        
        return null;
    }
    
    private void EndKillCam()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}

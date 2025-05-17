using UnityEngine;
using Fusion;

public class ArrowController : NetworkBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private float lifetime = 8f; // This field is now defined
    [SerializeField] private float stickForce = 10f;
    [SerializeField] private LayerMask stickLayers;
    [SerializeField] private GameObject arrowImpactFX;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip flybySound;
    
    // Damage is used when applying to targets/players
    [SerializeField, Tooltip("Amount of damage this arrow deals")] 
    private float damage = 100f;
    
    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private float nearMissDistance = 0.5f;
    
    private Rigidbody rb;
    private Collider arrowCollider;
    private bool hasHit = false;
    private float spawnTime;
    private AudioSource audioSource;
    
    [Networked]
    private NetworkBool Stuck { get; set; }
    
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        arrowCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        spawnTime = Time.time;
        
        // Initialize arrow
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
    }
    
    public override void FixedUpdateNetwork()
    {
        if (Stuck) return;
        
        // Orient arrow with velocity
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
        
        // Expire arrow after lifetime - make sure to use the class field
        if (Runner.IsServer && Time.time - spawnTime > this.lifetime)
        {
            Runner.Despawn(Object);
        }
        
        // Check for near misses
        if (Object.HasInputAuthority && !hasHit)
        {
            CheckForNearMiss();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (Stuck) return;
        
        // Check if we hit a target we can stick to
        bool canStickTo = ((1 << collision.gameObject.layer) & stickLayers) != 0;
        
        if (canStickTo)
        {
            // Stick the arrow
            StickArrow(collision);
        }
        
        // Check if we hit a player
        if (collision.gameObject.TryGetComponent<PlayerController>(out var player) && 
            Runner.IsServer)
        {
            // Apply damage
            if (player.TryGetComponent<PlayerRagdoll>(out var ragdoll))
            {
                Vector3 hitPoint = collision.contacts[0].point;
                Vector3 hitDirection = collision.contacts[0].normal;
                
                // Activate ragdoll with force
                ragdoll.RPC_ActivateRagdoll(hitPoint, -hitDirection * stickForce);
                
                // Register kill with game manager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterKill(Object.InputAuthority);
                }
            }
        }
        
        // Play impact effects
        PlayImpactEffects(collision);
    }
    
    private void StickArrow(Collision collision)
    {
        if (Runner.IsServer)
        {
            Stuck = true;
        }
        
        hasHit = true;
        
        // Disable physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Disable collider
        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }
        
        // Disable trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
        
        // Attach to hit object
        transform.SetParent(collision.transform);
    }
    
    private void PlayImpactEffects(Collision collision)
    {
        // Play impact sound
        if (audioSource != null && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
        
        // Play particles
        if (impactParticles != null)
        {
            impactParticles.transform.position = collision.contacts[0].point;
            impactParticles.transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);
            impactParticles.Play();
        }
        
        // Spawn impact FX
        if (arrowImpactFX != null && Runner.IsServer)
        {
            Runner.Spawn(
                arrowImpactFX, 
                collision.contacts[0].point, 
                Quaternion.LookRotation(collision.contacts[0].normal),
                Object.InputAuthority);
        }
    }
    
    private void CheckForNearMiss()
    {
        // Only check for near misses on local player's arrows
        if (audioSource == null || flybySound == null) return;
        
        // Find any players nearby using the non-obsolete method
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        
        foreach (PlayerController player in players)
        {
            // Skip if it's our player
            if (player.Object.InputAuthority == Object.InputAuthority)
                continue;
                
            // Calculate distance from arrow to player (avoid using sqrMagnitude for precision)
            Vector3 playerPos = player.transform.position + Vector3.up; // Aim for upper body
            Vector3 arrowPos = transform.position;
            Vector3 arrowVelocity = rb.linearVelocity;
            
            // Calculate closest point of approach
            Vector3 playerToArrow = arrowPos - playerPos;
            float dot = Vector3.Dot(playerToArrow, arrowVelocity.normalized);
            Vector3 closestPoint = arrowPos - arrowVelocity.normalized * dot;
            
            float distance = Vector3.Distance(playerPos, closestPoint);
            
            // If we're close enough and moving fast, play flyby sound
            if (distance < nearMissDistance && arrowVelocity.magnitude > 5f)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(flybySound);
                
                // Only trigger once
                return;
            }
        }
    }
}

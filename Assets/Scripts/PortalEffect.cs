using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class PortalEffect : NetworkBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private Transform exitPortal;
    [SerializeField] private float teleportCooldown = 2f;
    [SerializeField] private bool preserveMomentum = true;
    [SerializeField] private bool rotatePlayer = true;
    [SerializeField] private float momentumMultiplier = 1.0f;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem portalParticles;
    [SerializeField] private float particleBoostOnTeleport = 2f;
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private Light portalLight;
    [SerializeField] private Color activeColor = Color.blue;
    [SerializeField] private Color cooldownColor = Color.red;
    [SerializeField] private float lightIntensityPulse = 1.5f;
    [SerializeField] private float pulseSpeed = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip portalHumSound;
    [SerializeField] private AudioClip teleportSound;
    
    [Networked]
    private NetworkBool IsOnCooldown { get; set; }
    
    private NetworkBool _previousCooldownState;
    private AudioSource audioSource;
    private float defaultLightIntensity;
    private float baseParticleEmissionRate;
    private ParticleSystem.EmissionModule emission;
    private float cooldownEndTime;
    
    // Track players who have recently teleported
    private static readonly float TELEPORT_IMMUNITY_TIME = 0.5f;
    private System.Collections.Generic.Dictionary<PlayerRef, float> recentlyTeleported = 
        new System.Collections.Generic.Dictionary<PlayerRef, float>();
    
    public override void Spawned()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Initialize particles
        if (portalParticles != null)
        {
            emission = portalParticles.emission;
            baseParticleEmissionRate = emission.rateOverTimeMultiplier;
        }
        
        // Initialize light
        if (portalLight != null)
        {
            defaultLightIntensity = portalLight.intensity;
            portalLight.color = activeColor;
        }
        
        // Play ambient sound
        if (audioSource != null && portalHumSound != null)
        {
            audioSource.clip = portalHumSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        _previousCooldownState = IsOnCooldown;
    }
    
    public override void FixedUpdateNetwork()
    {
        // Check for changes in cooldown state
        if (IsOnCooldown != _previousCooldownState)
        {
            UpdatePortalState();
            _previousCooldownState = IsOnCooldown;
        }
        
        // Check if cooldown has expired
        if (IsOnCooldown && Time.time >= cooldownEndTime)
        {
            IsOnCooldown = false;
        }
        
        // Clean up old entries in the teleported players dictionary
        System.Collections.Generic.List<PlayerRef> keysToRemove = new System.Collections.Generic.List<PlayerRef>();
        foreach (var entry in recentlyTeleported)
        {
            if (Time.time - entry.Value > TELEPORT_IMMUNITY_TIME)
            {
                keysToRemove.Add(entry.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            recentlyTeleported.Remove(key);
        }
    }
    
    private void Update()
    {
        if (portalLight != null)
        {
            // Pulse the light
            float pulseIntensity = defaultLightIntensity + 
                Mathf.Sin(Time.time * pulseSpeed) * lightIntensityPulse;
            
            portalLight.intensity = pulseIntensity;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (IsOnCooldown || exitPortal == null)
            return;
            
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Object != null)
        {
            // Check if this player recently teleported (to prevent loop teleports)
            if (recentlyTeleported.ContainsKey(player.Object.InputAuthority))
                return;
                
            TeleportPlayer(player);
        }
    }
    
    private void TeleportPlayer(PlayerController player)
    {
        if (!Object.HasStateAuthority)
            return;
            
        // Set cooldown
        IsOnCooldown = true;
        cooldownEndTime = Time.time + teleportCooldown;
        
        // Track this player as recently teleported
        recentlyTeleported[player.Object.InputAuthority] = Time.time;
        
        // Get current velocity and move to exit position
        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 velocity = Vector3.zero;
        
        if (controller != null)
        {
            // Cache velocity before teleport
            velocity = controller.velocity;
            
            // Disable controller to allow teleport
            controller.enabled = false;
            
            // Set player position at exit portal
            player.transform.position = exitPortal.position + Vector3.up * controller.height/2;
            
            // Optionally rotate player to match exit portal orientation
            if (rotatePlayer)
            {
                player.transform.rotation = exitPortal.rotation;
            }
            
            // Re-enable controller
            controller.enabled = true;
            
            // Apply preserved momentum in the forward direction of exit portal
            if (preserveMomentum && velocity.magnitude > 0.1f)
            {
                // Project velocity onto forward direction of exit portal
                Vector3 newVelocity = exitPortal.forward * velocity.magnitude * momentumMultiplier;
                
                if (player.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = newVelocity;
                }
            }
        }
        else
        {
            // Fallback if no controller is found
            player.transform.position = exitPortal.position;
            if (rotatePlayer)
            {
                player.transform.rotation = exitPortal.rotation;
            }
        }
        
        // Spawn teleport effect at exit portal
        if (teleportEffect != null)
        {
            Runner.Spawn(teleportEffect, exitPortal.position, exitPortal.rotation);
        }
        
        // Play teleport sound at exit
        RPC_PlayTeleportSound(exitPortal.position);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTeleportSound(Vector3 position)
    {
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, position);
        }
        
        // Boost particles temporarily
        if (portalParticles != null)
        {
            var emission = portalParticles.emission;
            StartCoroutine(BoostParticles());
        }
    }
    
    private System.Collections.IEnumerator BoostParticles()
    {
        emission.rateOverTimeMultiplier = baseParticleEmissionRate * particleBoostOnTeleport;
        yield return new WaitForSeconds(0.5f);
        emission.rateOverTimeMultiplier = baseParticleEmissionRate;
    }
    
    private void UpdatePortalState()
    {
        if (portalLight != null)
        {
            portalLight.color = IsOnCooldown ? cooldownColor : activeColor;
        }
    }
}

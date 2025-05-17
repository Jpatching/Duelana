using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PlayerRagdoll : NetworkBehaviour
{
    [Header("Ragdoll Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody mainRigidbody;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionForce = 1500f;
    [SerializeField] private float upwardModifier = 1f;
    [SerializeField] private GameObject explosionEffectPrefab;
    
    [Tooltip("All rigidbodies in the ragdoll")]
    [SerializeField] private List<Rigidbody> ragdollRigidbodies = new List<Rigidbody>();
    
    [Tooltip("All colliders in the ragdoll")]
    [SerializeField] private List<Collider> ragdollColliders = new List<Collider>();
    
    private bool isRagdolled = false;
    
    public override void Spawned()
    {
        // Ensure ragdoll is initially disabled
        SetRagdollState(false);
    }
    
    private void OnValidate()
    {
        // Auto-populate rigidbodies and colliders if empty
        if (ragdollRigidbodies.Count == 0 || ragdollColliders.Count == 0)
        {
            PopulateRagdollParts();
        }
    }
    
    private void PopulateRagdollParts()
    {
        ragdollRigidbodies.Clear();
        ragdollColliders.Clear();
        
        // Skip the main rigidbody (the one on the root)
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodies)
        {
            if (rb != mainRigidbody)
            {
                ragdollRigidbodies.Add(rb);
            }
        }
        
        // Get all colliders except the character controller
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            if (!(col is CharacterController))
            {
                ragdollColliders.Add(col);
            }
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ActivateRagdoll(Vector3 hitPoint, Vector3 force)
    {
        ActivateRagdoll(hitPoint, force);
    }
    
    public void ActivateRagdoll(Vector3 hitPoint, Vector3 force)
    {
        if (isRagdolled) return;
        
        // Set state
        isRagdolled = true;
        
        // Disable character controller and animator
        if (characterController != null)
            characterController.enabled = false;
            
        if (animator != null)
            animator.enabled = false;
            
        // Disable main rigidbody
        if (mainRigidbody != null)
            mainRigidbody.isKinematic = true;
            
        // Enable ragdoll physics
        SetRagdollState(true);
        
        // Apply force at impact point
        ApplyForceAtPoint(hitPoint, force);
        
        // Spawn explosion effect
        SpawnExplosionEffect(hitPoint);
        
        // Disable player controller script
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;
    }
    
    public void ResetRagdoll()
    {
        // Disable ragdoll physics and reset state
        if (isRagdolled)
        {
            isRagdolled = false;
            
            // Enable character controller
            if (characterController != null)
                characterController.enabled = true;
                
            // Enable animator
            if (animator != null)
                animator.enabled = true;
                
            // Enable main rigidbody
            if (mainRigidbody != null)
                mainRigidbody.isKinematic = false;
                
            // Disable ragdoll physics
            SetRagdollState(false);
            
            // Enable player controller script
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = true;
        }
    }
    
    private void SetRagdollState(bool active)
    {
        // Set rigidbodies
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active;
            rb.useGravity = active;
        }
        
        // Set colliders
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = active;
        }
    }
    
    private void ApplyForceAtPoint(Vector3 hitPoint, Vector3 initialForce)
    {
        // Apply direct force first
        Rigidbody closestRb = FindClosestRigidbody(hitPoint);
        if (closestRb != null)
        {
            closestRb.AddForce(initialForce, ForceMode.Impulse);
        }
        
        // Then add explosion force
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius, upwardModifier, ForceMode.Impulse);
        }
    }
    
    private Rigidbody FindClosestRigidbody(Vector3 position)
    {
        Rigidbody closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            float distance = Vector3.Distance(position, rb.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = rb;
            }
        }
        
        return closest;
    }
    
    private void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null && Runner != null && Object.HasStateAuthority)
        {
            Runner.Spawn(explosionEffectPrefab, position, Quaternion.identity, Object.InputAuthority);
        }
    }
}

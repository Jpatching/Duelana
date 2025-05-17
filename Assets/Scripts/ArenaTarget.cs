using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class ArenaTarget : NetworkBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private int pointValue = 10;
    [SerializeField] private float resetDelay = 3f;
    [SerializeField] private GameObject visualModel;
    [SerializeField] private GameObject hitEffect;
    
    [Header("Movement")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1f;
    
    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    private bool isActive = true;
    private bool isWaiting = false;
    
    private Material targetMaterial;
    private Color originalColor;
    
    [Networked]
    private NetworkBool IsHit { get; set; }
    
    private NetworkBool _previousHitState;
    
    public override void Spawned()
    {
        if (visualModel != null && visualModel.TryGetComponent<Renderer>(out var renderer))
        {
            targetMaterial = renderer.material;
            originalColor = targetMaterial.color;
        }
        
        ResetTarget();
        _previousHitState = IsHit;
    }
    
    public override void FixedUpdateNetwork()
    {
        // Check for changes in hit state
        if (IsHit != _previousHitState)
        {
            OnHitStateChanged();
            _previousHitState = IsHit;
        }
        
        if (!isActive || IsHit || !isMoving || waypoints == null || waypoints.Length < 2)
            return;
            
        if (isWaiting)
        {
            waitTimer += Runner.DeltaTime;
            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                isWaiting = false;
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            }
            return;
        }
        
        // Move toward next waypoint
        Transform targetWaypoint = waypoints[currentWaypoint];
        if (targetWaypoint != null)
        {
            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetWaypoint.position);
            
            if (distance > 0.1f)
            {
                // Move toward waypoint
                transform.position += direction * moveSpeed * Runner.DeltaTime;
                
                // Optional: rotate to face movement direction
                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }
            }
            else
            {
                // Reached waypoint, start waiting
                isWaiting = true;
                waitTimer = 0f;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (IsHit || !Object.HasStateAuthority)
            return;
            
        Arrow arrow = other.GetComponent<Arrow>();
        if (arrow != null)
        {
            IsHit = true;
            
            // Register the score with the game manager
            if (GameManager.Instance != null)
            {
                PlayerRef playerRef = arrow.Object.InputAuthority;
                GameManager.Instance.RegisterTargetHit(playerRef, pointValue);
            }
        }
    }
    
    private void ResetTarget()
    {
        IsHit = false;
        isActive = true;
        
        if (visualModel != null)
            visualModel.SetActive(true);
            
        if (targetMaterial != null)
            targetMaterial.color = originalColor;
    }
    
    private void OnHitStateChanged()
    {
        if (IsHit)
        {
            // Visual feedback for hit
            if (visualModel != null)
                visualModel.SetActive(false);
                
            // Show hit effect
            if (hitEffect != null)
                hitEffect.SetActive(true);
                
            // Start reset timer (only on server)
            if (Object.HasStateAuthority)
            {
                TickTimer resetTimer = TickTimer.CreateFromSeconds(Runner, resetDelay);
                Runner.StartCoroutine(ResetAfterDelay(resetTimer));
            }
        }
        else
        {
            // Hide hit effect when reset
            if (hitEffect != null)
                hitEffect.SetActive(false);
        }
    }
    
    private System.Collections.IEnumerator ResetAfterDelay(TickTimer timer)
    {
        yield return timer;
        ResetTarget();
    }
}

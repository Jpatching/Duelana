using UnityEngine;
using Fusion;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -19.62f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float airControl = 0.5f;
    
    [Header("Combat Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private NetworkPrefabRef arrowPrefab;
    [SerializeField] private float arrowSpeed = 25f;
    [SerializeField] private float shootCooldown = 0.7f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject bowModel;
    [SerializeField] private Transform aimTransform;
    [SerializeField] private GameObject[] playerVisuals;
    
    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip bowDrawSound;
    [SerializeField] private AudioClip arrowReleaseSound;
    
    [Networked]
    private bool isAiming { get; set; }
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 currentVelocity;
    private AudioSource audioSource;
    private float lastFootstepTime;
    private float footstepRate = 0.3f;
    private bool canShoot = true;
    private float footstepThreshold = 0.1f;
    
    // Animation parameter hashes for efficiency
    private int animIsGroundedHash;
    private int animIsMovingHash;
    private int animMoveSpeedHash;
    private int animIsAimingHash;
    private int animShootTriggerHash;
    private int animJumpTriggerHash;
    
    public override void Spawned()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        // Initialize animation hashes if animator exists
        if (animator != null)
        {
            animIsGroundedHash = Animator.StringToHash("IsGrounded");
            animIsMovingHash = Animator.StringToHash("IsMoving");
            animMoveSpeedHash = Animator.StringToHash("MoveSpeed");
            animIsAimingHash = Animator.StringToHash("IsAiming");
            animShootTriggerHash = Animator.StringToHash("Shoot");
            animJumpTriggerHash = Animator.StringToHash("Jump");
        }
        
        if (!Object.HasInputAuthority)
        {
            gameObject.layer = LayerMask.NameToLayer("RemotePlayer");
            // Don't need to setup camera for remote players
            return;
        }
        
        // Camera is now handled by CameraController
    }
    
    void Update()
    {
        if (!Object.HasInputAuthority || controller == null)
            return;
            
        HandleAiming();
        
        // Only shoot if not aiming (quick shot) or if aiming and clicked
        if (Input.GetMouseButtonDown(0) && canShoot)
        {
            ShootArrow();
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (controller == null) return;
        
        if (Object.HasInputAuthority)
        {
            // Handle local player input and movement
            HandleMovement();
            HandleJump();
        }
        
        // Apply gravity for all (local and remote players)
        ApplyGravity();
        
        // Update animation state
        UpdateAnimations();
    }
    
    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
            
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(x, 0f, z).normalized;
        
        if (inputDir.magnitude >= 0.1f)
        {
            // Get camera's forward direction (horizontal only)
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            
            // Create direction relative to camera
            Vector3 moveDirection = cameraForward * inputDir.z + cameraRight * inputDir.x;
            
            // Apply speed (slower when aiming)
            float currentSpeed = isAiming ? moveSpeed * 0.5f : 
                                (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed);
            
            // Apply air control reduction if not grounded
            if (!isGrounded)
                currentSpeed *= airControl;
                
            Vector3 targetVelocity = moveDirection * currentSpeed;
            Vector3 move = Vector3.SmoothDamp(new Vector3(controller.velocity.x, 0, controller.velocity.z), 
                                             targetVelocity, ref currentVelocity, smoothTime);
                                             
            controller.Move(move * Runner.DeltaTime);
            
            // Handle footstep sounds
            if (isGrounded && move.magnitude > footstepThreshold)
            {
                if (Time.time - lastFootstepTime > footstepRate / move.magnitude)
                {
                    PlayFootstepSound();
                    lastFootstepTime = Time.time;
                }
            }
            
            // Rotate player to match movement direction when not aiming
            if (!isAiming && moveDirection != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, moveDirection, 10f * Runner.DeltaTime);
            }
        }
    }
    
    private void HandleJump()
    {
        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            // Play jump animation
            if (animator != null)
                animator.SetTrigger(animJumpTriggerHash);
                
            // Play jump sound
            if (audioSource != null && jumpSound != null)
                audioSource.PlayOneShot(jumpSound);
        }
    }
    
    private void ApplyGravity()
    {
        velocity.y += gravity * Runner.DeltaTime;
        controller.Move(velocity * Runner.DeltaTime);
    }
    
    private void HandleAiming()
    {
        // Toggle aim with right mouse button
        bool wasAiming = isAiming;
        isAiming = Input.GetMouseButton(1);
        
        // If we just started aiming
        if (!wasAiming && isAiming)
        {
            if (audioSource != null && bowDrawSound != null)
                audioSource.PlayOneShot(bowDrawSound);
        }
        
        // When aiming, rotate player to match camera forward direction
        if (isAiming)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            if (cameraForward != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, cameraForward, 15f * Time.deltaTime);
            }
            
            // Align aim transform with camera when aiming
            if (aimTransform != null)
            {
                aimTransform.forward = Camera.main.transform.forward;
            }
        }
        
        // Update bow model visibility based on aiming status
        if (bowModel != null)
            bowModel.SetActive(true); // Always visible now, animation will handle bow position
    }
    
    void ShootArrow()
    {
        if (arrowPrefab == null || shootPoint == null || Runner == null) 
            return;
            
        // Play shoot animation
        if (animator != null)
            animator.SetTrigger(animShootTriggerHash);
            
        // Play shoot sound
        if (audioSource != null && arrowReleaseSound != null)
            audioSource.PlayOneShot(arrowReleaseSound);
            
        // Use aim direction when aiming, otherwise use player forward
        Vector3 shootDirection = isAiming && aimTransform != null ? 
                               aimTransform.forward : shootPoint.forward;
                               
        // Spawn the arrow
        Runner.Spawn(arrowPrefab, shootPoint.position, Quaternion.LookRotation(shootDirection), Object.InputAuthority,
            (runner, obj) => {
                if (obj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = shootDirection * arrowSpeed;
                }
            });
            
        // Apply cooldown
        StartCoroutine(ShootCooldown());
    }
    
    private IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Update animation parameters
        animator.SetBool(animIsGroundedHash, isGrounded);
        animator.SetBool(animIsMovingHash, controller.velocity.magnitude > 0.1f);
        animator.SetFloat(animMoveSpeedHash, controller.velocity.magnitude / moveSpeed);
        animator.SetBool(animIsAimingHash, isAiming);
    }
    
    private void PlayFootstepSound()
    {
        if (audioSource == null || footstepSounds == null || footstepSounds.Length == 0)
            return;
            
        int randomIndex = Random.Range(0, footstepSounds.Length);
        audioSource.PlayOneShot(footstepSounds[randomIndex], 0.7f);
    }
}

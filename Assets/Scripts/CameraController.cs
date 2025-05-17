using UnityEngine;
using Fusion;

public class CameraController : NetworkBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Vector3 normalOffset = new Vector3(0, 2f, -5f);
    [SerializeField] private Vector3 aimOffset = new Vector3(1f, 1.5f, -3f);
    [SerializeField] private float damping = 5f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 8f;
    
    private Transform cameraTransform;
    private float currentZoomDistance;
    private float currentYRotation;
    private float currentXRotation;
    private Vector3 currentOffset;
    
    public override void Spawned()
    {
        // Only run on local player
        if (!Object.HasInputAuthority)
        {
            enabled = false;
            return;
        }
        
        // Get camera reference
        cameraTransform = Camera.main.transform;
        cameraTransform.SetParent(null); // Detach from player to follow manually
        
        // Initialize
        currentZoomDistance = Vector3.Distance(normalOffset, Vector3.zero);
        currentOffset = normalOffset;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void LateUpdate()
    {
        if (cameraTransform == null || !Object.HasInputAuthority)
            return;
            
        // Handle zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoomDistance -= scrollInput * zoomSpeed;
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
        
        // Handle rotation
        if (Input.GetMouseButton(1)) // Right mouse button for aiming
        {
            currentOffset = Vector3.Lerp(currentOffset, aimOffset, damping * Time.deltaTime);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, normalOffset, damping * Time.deltaTime);
            
            // Only rotate when not aiming
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
            
            currentYRotation += mouseX;
            currentXRotation -= mouseY;
            currentXRotation = Mathf.Clamp(currentXRotation, minVerticalAngle, maxVerticalAngle);
        }
        
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0);
        
        // Calculate position
        Vector3 zoomedOffset = currentOffset.normalized * currentZoomDistance;
        Vector3 targetPosition = transform.position + rotation * zoomedOffset;
        
        // Apply smooth follow
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, damping * Time.deltaTime);
        cameraTransform.LookAt(transform.position + Vector3.up * 1.5f);
    }
    
    void OnDestroy()
    {
        // Unlock cursor when destroyed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

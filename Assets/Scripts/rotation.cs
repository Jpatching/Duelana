using UnityEngine;

public class Spin : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second")]
    [SerializeField] private float speed = 180f;
    
    [Tooltip("Axis to rotate around")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    
    [Header("Effects")]
    [SerializeField] private bool pulseScale = false;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 1f;
    
    private Vector3 originalScale;
    
    void Start()
    {
        originalScale = transform.localScale;
    }
    
    void Update()
    {
        // Rotate around the specified axis
        transform.Rotate(rotationAxis.normalized * (speed * Time.deltaTime));
        
        // Apply pulse effect if enabled
        if (pulseScale)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }
}
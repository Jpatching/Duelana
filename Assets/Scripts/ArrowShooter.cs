using UnityEngine;
using Fusion;

public class ArrowShooter : NetworkBehaviour
{
    public NetworkPrefabRef arrowPrefab;
    public Transform shootPoint;
    public float arrowForce = 25f;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (Runner != null && Runner.IsForward)
            {
                Runner.Spawn(arrowPrefab, shootPoint.position, shootPoint.rotation, Object.InputAuthority, (runner, obj) =>
                {
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    rb.linearVelocity = shootPoint.forward * arrowForce;
                });
            }
        }
    }
}

using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public float interactionDistance = 2.5f;

    [Header("Held View Offset Tweaks")]
    public Vector3 holdPositionOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;

    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public bool isPickedUp = false;
    [HideInInspector] public bool isPlaced = false;

    private Rigidbody rb;
    private Collider[] colliders;
    private Transform currentHoldPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    void LateUpdate()
    {
        if (isHeld && currentHoldPoint != null)
        {
            transform.position = currentHoldPoint.TransformPoint(holdPositionOffset);
            transform.rotation = currentHoldPoint.rotation * Quaternion.Euler(holdRotationOffset);
        }
    }

    public void PickUp(Transform holdPoint)
    {
        isHeld = true;
        isPickedUp = true;
        isPlaced = false;
        currentHoldPoint = holdPoint;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                #else
                rb.velocity = Vector3.zero;
                #endif
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = false;
            }
        }

        transform.SetParent(holdPoint);
        transform.position = holdPoint.TransformPoint(holdPositionOffset);
        transform.rotation = holdPoint.rotation * Quaternion.Euler(holdRotationOffset);
    }

    public void Place(Vector3 pos, Quaternion rot)
    {
        isHeld = false;
        isPickedUp = false;
        isPlaced = true;
        currentHoldPoint = null;

        transform.SetParent(null);

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = true;
            }
        }

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                #else
                rb.velocity = Vector3.zero;
                #endif
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        transform.position = pos;
        transform.rotation = rot;
    }

    public void Drop(Vector3 throwDir, float throwForce = 3f)
    {
        isHeld = false;
        isPlaced = false;
        currentHoldPoint = null;

        transform.SetParent(null);

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = true;
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = throwDir * throwForce;
            #else
            rb.velocity = throwDir * throwForce;
            #endif
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }
    }
}

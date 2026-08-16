using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Engine & Speed Settings")]
    public float motorForce = 25f;
    public float maxForwardSpeed = 20f;
    public float maxReverseSpeed = 10f;
    public float coastingDrag = 1.5f;
    public float brakingDeceleration = 25f;

    [Header("Steering Settings")]
    public float turnSpeed = 70f;

    [Header("Camera Settings")]
    public Vector3 cameraOffset = new Vector3(0f, 2.3f, -5.5f);
    public float cameraFollowSpeed = 10f;

    [HideInInspector]
    public bool isDriven = false;

    private Rigidbody rb;
    private Transform mainCamera;
    private float currentSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = 1500f;
        rb.linearDamping = coastingDrag;
        rb.angularDamping = 5.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0f, -0.8f, 0f); // Low center of mass
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (!isDriven)
        {
            if (!rb.isKinematic) rb.isKinematic = true;
            return;
        }

        if (rb.isKinematic) rb.isKinematic = false;

        // Force pitch (X) and roll (Z) to remain flat on the ground so car cannot climb/tilt up walls
        Vector3 currentEuler = transform.rotation.eulerAngles;
        rb.MoveRotation(Quaternion.Euler(0f, currentEuler.y, 0f));
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        float accelInput = 0f;
        float steerInput = 0f;
        bool isBraking = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) accelInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) accelInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput -= 1f;
            isBraking = Keyboard.current.spaceKey.isPressed;
        }

        // Acceleration, Braking & Coasting
        if (isBraking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingDeceleration * Time.fixedDeltaTime);
        }
        else if (accelInput > 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxForwardSpeed, motorForce * Time.fixedDeltaTime);
        }
        else if (accelInput < 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, motorForce * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDrag * 3f * Time.fixedDeltaTime);
        }

        // Steering
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float steerDirection = (currentSpeed < 0f) ? -steerInput : steerInput;
            float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 3f);
            float turnAmount = steerDirection * turnSpeed * speedRatio * Time.fixedDeltaTime;

            Quaternion turnRotation = Quaternion.Euler(0f, currentEuler.y + turnAmount, 0f);
            rb.MoveRotation(turnRotation);
        }

        // Move vehicle forward/backward along current rotation
        Vector3 forwardVel = transform.forward * currentSpeed;

        #if UNITY_6000_0_OR_NEWER
        Vector3 currentVel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(forwardVel.x, currentVel.y, forwardVel.z);
        #else
        Vector3 currentVel = rb.velocity;
        rb.velocity = new Vector3(forwardVel.x, currentVel.y, forwardVel.z);
        #endif
    }

    void OnCollisionEnter(Collision collision)
    {
        // When hitting walls/obstacles, stop pushing forward so the car doesn't climb up the wall
        if (isDriven)
        {
            // Bounce slightly back and kill forward momentum
            currentSpeed = -currentSpeed * 0.25f;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // If pressing forward against a wall, prevent speed buildup
        if (isDriven)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // Check if contact normal is facing against our forward direction (wall in front)
                if (Vector3.Dot(contact.normal, transform.forward) < -0.5f && currentSpeed > 0f)
                {
                    currentSpeed = 0f;
                    break;
                }
                // Check if contact normal is facing with our forward direction (wall behind)
                if (Vector3.Dot(contact.normal, transform.forward) > 0.5f && currentSpeed < 0f)
                {
                    currentSpeed = 0f;
                    break;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isDriven && mainCamera != null)
        {
            Vector3 targetCamPos = transform.TransformPoint(cameraOffset);
            mainCamera.position = Vector3.Lerp(mainCamera.position, targetCamPos, Time.deltaTime * cameraFollowSpeed);
            mainCamera.LookAt(transform.position + Vector3.up * 1.2f);
        }
    }

    public void EnterVehicle(Transform cameraTransform)
    {
        isDriven = true;
        mainCamera = cameraTransform;
        currentSpeed = 0f;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public void ExitVehicle()
    {
        isDriven = false;
        currentSpeed = 0f;
        if (rb != null)
        {
            #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            #else
            rb.velocity = Vector3.zero;
            #endif
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}

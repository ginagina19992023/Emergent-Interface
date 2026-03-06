using UnityEngine;

/// <summary>
/// Physics-based helicopter controller with shooting.
/// Requires a Rigidbody and HelicopterInput on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HelicopterInput))]
public class HelicopterController : MonoBehaviour
{
    [Header("Thrust")]
    [Tooltip("Maximum upward force applied along the helicopter's local up axis.")]
    [SerializeField] private float maxThrust = 30f;

    [Tooltip("Minimum upward force when throttle is at zero (set to 0 to fall without input).")]
    [SerializeField] private float idleThrust = 0f;

    [Header("Rotation")]
    [Tooltip("Degrees per second of yaw rotation.")]
    [SerializeField] private float yawSpeed = 120f;

    [Tooltip("Degrees per second of pitch rotation.")]
    [SerializeField] private float pitchSpeed = 80f;

    [Tooltip("Maximum pitch angle in degrees.")]
    [SerializeField] private float maxPitchAngle = 45f;

    [Header("Stability")]
    [Tooltip("How quickly the helicopter returns to level roll (0 = never, higher = faster).")]
    [SerializeField] private float rollStabilization = 2f;

    [Tooltip("Linear drag applied by the Rigidbody (set on Start).")]
    [SerializeField] private float linearDrag = 1f;

    [Tooltip("Angular drag applied by the Rigidbody (set on Start).")]
    [SerializeField] private float angularDrag = 4f;

    [Header("Shooting (Hitscan)")]
    [Tooltip("Maximum range of the hitscan weapon.")]
    [SerializeField] private float maxRange = 500f;

    [Tooltip("Damage dealt per shot.")]
    [SerializeField] private float damage = 1f;

    [Tooltip("Transform used as the ray origin. If empty, uses the helicopter's position.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Layers the hitscan ray can hit.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    private Rigidbody rb;
    private HelicopterInput input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<HelicopterInput>();

        rb.useGravity = true;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
    }

    void FixedUpdate()
    {
        ApplyThrust();
        ApplyYaw();
        ApplyPitch();
        StabilizeRoll();
    }

    void Update()
    {
        if (input.ShootPressed)
            Shoot();
    }

    private void ApplyThrust()
    {
        // Thrust along local up. No hover force — the helicopter falls without active throttle.
        float throttle01 = Mathf.Clamp01(input.Throttle);
        float thrust = idleThrust + throttle01 * maxThrust;

        Vector3 thrustVector = transform.up * thrust;
        rb.AddForce(thrustVector, ForceMode.Acceleration);
    }

    private void ApplyYaw()
    {
        float yawTorque = input.Yaw * yawSpeed * Time.fixedDeltaTime;
        Quaternion yawRotation = Quaternion.Euler(0f, yawTorque, 0f);
        rb.MoveRotation(rb.rotation * yawRotation);
    }

    private void ApplyPitch()
    {
        float pitchTorque = -input.Pitch * pitchSpeed * Time.fixedDeltaTime;

        // Clamp pitch so the helicopter can't flip over
        Quaternion desiredRotation = rb.rotation * Quaternion.Euler(pitchTorque, 0f, 0f);
        Vector3 desiredEuler = desiredRotation.eulerAngles;

        // Normalize pitch to -180..180 range for clamping
        float normalizedPitch = desiredEuler.x;
        if (normalizedPitch > 180f) normalizedPitch -= 360f;
        normalizedPitch = Mathf.Clamp(normalizedPitch, -maxPitchAngle, maxPitchAngle);

        desiredEuler.x = normalizedPitch;
        rb.MoveRotation(Quaternion.Euler(desiredEuler));
    }

    private void StabilizeRoll()
    {
        // Gradually zero out roll so the helicopter stays level on the Z axis
        Vector3 euler = rb.rotation.eulerAngles;
        float roll = euler.z;
        if (roll > 180f) roll -= 360f;

        float correction = -roll * rollStabilization * Time.fixedDeltaTime;
        Quaternion rollCorrection = Quaternion.Euler(0f, 0f, correction);
        rb.MoveRotation(rb.rotation * rollCorrection);
    }

    private void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + transform.forward * 2f;
        Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers))
        {
            FloatingTarget target = hit.collider.GetComponentInParent<FloatingTarget>();
            if (target != null)
                target.TakeHit(damage);
        }
    }
}

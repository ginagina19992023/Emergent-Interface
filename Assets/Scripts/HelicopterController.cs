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

    [Tooltip("Force needed to counteract gravity when throttle is neutral (auto-hover).")]
    [SerializeField] private float hoverForce = 9.81f;

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

    [Header("Shooting")]
    [Tooltip("Bullet prefab to instantiate. Must have a Rigidbody and Bullet component.")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Transform used as the bullet spawn point. If empty, uses the helicopter's position.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Speed at which bullets are launched.")]
    [SerializeField] private float bulletSpeed = 80f;

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
        // Thrust always points along the helicopter's local up axis,
        // so tilting the helicopter redirects the thrust vector.
        float thrust = hoverForce + input.Throttle * maxThrust;
        thrust = Mathf.Max(thrust, 0f);

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
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward * 2f;
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = rb.linearVelocity;
            bulletRb.AddForce(spawnRot * Vector3.forward * bulletSpeed, ForceMode.VelocityChange);
        }
    }
}

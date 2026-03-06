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

    [Header("Shoot Effects")]
    [Tooltip("AudioSource used for shooting sounds (should be on the helicopter).")]
    [SerializeField] private AudioSource shootAudioSource;

    [Tooltip("Sound played when the weapon fires.")]
    [SerializeField] private AudioClip shootSound;

    [Tooltip("Sound played when a target is hit (but not destroyed).")]
    [SerializeField] private AudioClip hitMarkerSound;

    [Tooltip("Prefab with a LineRenderer for the bullet trail. Spawned per shot.")]
    [SerializeField] private LineRenderer bulletTrailPrefab;

    [Tooltip("How long the bullet trail is visible (seconds).")]
    [SerializeField] private float trailDuration = 0.08f;

    [Header("Gravity")]
    [Tooltip("Extra downward acceleration when not thrusting, on top of normal gravity.")]
    [SerializeField] private float fallAcceleration = 15f;

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
        float throttle01 = Mathf.Clamp01(input.Throttle);
        float thrust = idleThrust + throttle01 * maxThrust;

        Vector3 thrustVector = transform.up * thrust;
        rb.AddForce(thrustVector, ForceMode.Acceleration);

        // Pull the helicopter down harder when not actively thrusting
        if (throttle01 < 0.01f)
            rb.AddForce(Vector3.down * fallAcceleration, ForceMode.Acceleration);
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

        // Shoot SFX
        if (shootSound != null && shootAudioSource != null)
            shootAudioSource.PlayOneShot(shootSound);

        Vector3 endPoint = origin + direction * maxRange;
        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers);

        if (didHit)
        {
            endPoint = hit.point;

            FloatingTarget target = hit.collider.GetComponentInParent<FloatingTarget>();
            if (target != null)
            {
                target.TakeHit(damage);

                // Hit marker SFX
                if (hitMarkerSound != null && shootAudioSource != null)
                    shootAudioSource.PlayOneShot(hitMarkerSound);
            }
        }

        // Bullet trail
        if (bulletTrailPrefab != null)
            SpawnTrail(origin, endPoint);
    }

    private void SpawnTrail(Vector3 start, Vector3 end)
    {
        LineRenderer trail = Instantiate(bulletTrailPrefab);
        trail.positionCount = 2;
        trail.SetPosition(0, start);
        trail.SetPosition(1, end);
        Destroy(trail.gameObject, trailDuration);
    }
}

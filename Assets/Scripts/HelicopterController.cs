using UnityEngine;
using System.Collections;

/// <summary>
/// Physics-based helicopter controller with shooting.
/// Requires a Rigidbody and HelicopterInput on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HelicopterInput))]
public class HelicopterController : MonoBehaviour
{
    [Header("Lift (press rate controls altitude)")]
    [Tooltip("Press rate (presses/sec) needed to hover. Below this = fall, above = rise.")]
    [SerializeField] private float hoverPressRate = 2f;

    [Tooltip("Maximum lift force when pressing at max rate.")]
    [SerializeField] private float maxLiftForce = 40f;

    [Tooltip("Press rate considered 'max' for full lift force.")]
    [SerializeField] private float maxPressRate = 5f;

    [Header("Forward Movement")]
    [Tooltip("Constant forward speed applied to the helicopter.")]
    [SerializeField] private float forwardSpeed = 5f;

    [Tooltip("Default downward pitch angle in degrees (helicopter nose points slightly down).")]
    [SerializeField] private float defaultPitchAngle = 5f;

    [Header("Shake")]
    [Tooltip("Enable movement shake for a more dynamic feel.")]
    [SerializeField] private bool enableShake = true;

    [Tooltip("Intensity of position shake.")]
    [SerializeField] private float positionShakeIntensity = 0.05f;

    [Tooltip("Intensity of rotation shake in degrees.")]
    [SerializeField] private float rotationShakeIntensity = 0.5f;

    [Tooltip("Speed of the shake oscillation.")]
    [SerializeField] private float shakeSpeed = 15f;

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

    [Tooltip("How quickly the helicopter returns to default pitch when not pitching (0 = never).")]
    [SerializeField] private float pitchStabilization = 1f;

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

    [Tooltip("Layers the hitscan ray can hit. Default excludes Ignore Raycast (e.g. gates).")]
    [SerializeField] private LayerMask hitLayers = ~(1 << 2);

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

    [Header("Muzzle Flash")]
    [Tooltip("Particle system for muzzle flash effect (optional).")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;

    [Tooltip("Light component for muzzle flash (optional, will be created if null).")]
    [SerializeField] private Light muzzleFlashLight;

    [Tooltip("Intensity of the muzzle flash light.")]
    [SerializeField] private float muzzleFlashIntensity = 3f;

    [Tooltip("Duration of muzzle flash light.")]
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    [Header("Gravity")]
    [Tooltip("Extra downward acceleration on top of normal gravity (helicopter falls when not tapping jump).")]
    [SerializeField] private float fallAcceleration = 15f;

    private Rigidbody rb;
    private HelicopterInput input;

    private float noiseOffsetX;
    private float noiseOffsetY;
    private float noiseOffsetZ;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<HelicopterInput>();

        rb.useGravity = true;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);
    }

    void FixedUpdate()
    {
        ApplyLift();
        ApplyFall();
        ApplyForwardMovement();
        ApplyYaw();
        ApplyPitch();
        StabilizeRoll();
        StabilizePitch();
        ApplyShake();
    }

    private void ApplyLift()
    {
        float pressRate = input.PressRate;

        float liftNormalized = Mathf.InverseLerp(0f, maxPressRate, pressRate);
        float hoverNormalized = Mathf.InverseLerp(0f, maxPressRate, hoverPressRate);

        float liftForce = Mathf.Lerp(-fallAcceleration, maxLiftForce, liftNormalized);

        float hoverForce = Mathf.Lerp(-fallAcceleration, maxLiftForce, hoverNormalized);

        float netForce = liftForce - hoverForce;

        rb.AddForce(transform.up * netForce, ForceMode.Acceleration);
    }

    void Update()
    {
        if (input.ShootPressed)
            Shoot();
    }

    private void ApplyFall()
    {
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
        Vector3 euler = rb.rotation.eulerAngles;
        float roll = euler.z;
        if (roll > 180f) roll -= 360f;

        float correction = -roll * rollStabilization * Time.fixedDeltaTime;
        Quaternion rollCorrection = Quaternion.Euler(0f, 0f, correction);
        rb.MoveRotation(rb.rotation * rollCorrection);
    }

    private void ApplyForwardMovement()
    {
        if (forwardSpeed > 0f)
            rb.AddForce(transform.forward * forwardSpeed, ForceMode.Acceleration);
    }

    /// <summary>
    /// Instant kick plus short sustained acceleration along a world direction (e.g. helicopter forward from a boost gate).
    /// Uses FixedUpdate-aligned forces so it stays consistent with physics.
    /// </summary>
    public void ApplyDirectionalBoost(Vector3 worldDirection, float velocityImpulse, float sustainedAcceleration, float duration)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (worldDirection.sqrMagnitude < 1e-6f)
            worldDirection = transform.forward;
        else
            worldDirection.Normalize();

        if (velocityImpulse != 0f)
            rb.AddForce(worldDirection * velocityImpulse, ForceMode.VelocityChange);
        if (sustainedAcceleration > 0f && duration > 0f)
            StartCoroutine(DirectionalBoostRoutine(worldDirection, sustainedAcceleration, duration));
    }

    IEnumerator DirectionalBoostRoutine(Vector3 direction, float acceleration, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.AddForce(direction * acceleration, ForceMode.Acceleration);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private void StabilizePitch()
    {
        if (pitchStabilization <= 0f) return;
        if (Mathf.Abs(input.Pitch) > 0.1f) return;

        Vector3 euler = rb.rotation.eulerAngles;
        float currentPitch = euler.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        float pitchDiff = defaultPitchAngle - currentPitch;
        float correction = pitchDiff * pitchStabilization * Time.fixedDeltaTime;
        Quaternion pitchCorrection = Quaternion.Euler(correction, 0f, 0f);
        rb.MoveRotation(rb.rotation * pitchCorrection);
    }

    private void ApplyShake()
    {
        if (!enableShake) return;

        float time = Time.time * shakeSpeed;

        float shakeX = (Mathf.PerlinNoise(time, noiseOffsetX) - 0.5f) * 2f;
        float shakeY = (Mathf.PerlinNoise(time, noiseOffsetY) - 0.5f) * 2f;
        float shakeZ = (Mathf.PerlinNoise(time, noiseOffsetZ) - 0.5f) * 2f;

        Vector3 positionShake = new Vector3(shakeX, shakeY, shakeZ) * positionShakeIntensity;
        rb.AddForce(positionShake, ForceMode.VelocityChange);

        Vector3 rotationShake = new Vector3(shakeX, shakeY, shakeZ) * rotationShakeIntensity;
        Quaternion shakeRotation = Quaternion.Euler(rotationShake * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * shakeRotation);
    }

    private void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + transform.forward * 2f;
        Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;

        // Shoot SFX
        if (shootSound != null && shootAudioSource != null)
            shootAudioSource.PlayOneShot(shootSound);

        // Muzzle flash
        TriggerMuzzleFlash(origin);

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

            SpawnHitEffect(hit.point, hit.normal);
        }

        // Bullet trail
        if (bulletTrailPrefab != null)
            SpawnTrail(origin, endPoint);
    }

    private void TriggerMuzzleFlash(Vector3 position)
    {
        if (muzzleFlashParticles != null)
            muzzleFlashParticles.Play();

        if (muzzleFlashLight != null)
            StartCoroutine(FlashLightRoutine());
        else
            StartCoroutine(CreateTemporaryFlash(position));
    }

    private IEnumerator FlashLightRoutine()
    {
        muzzleFlashLight.intensity = muzzleFlashIntensity;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleFlashLight.intensity = 0f;
    }

    private IEnumerator CreateTemporaryFlash(Vector3 position)
    {
        GameObject flashObj = new GameObject("MuzzleFlash");
        flashObj.transform.position = position;
        Light flash = flashObj.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.8f, 0.4f);
        flash.intensity = muzzleFlashIntensity;
        flash.range = 10f;

        yield return new WaitForSeconds(muzzleFlashDuration);
        Destroy(flashObj);
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        GameObject hitObj = new GameObject("HitSpark");
        hitObj.transform.position = position;

        Light hitLight = hitObj.AddComponent<Light>();
        hitLight.type = LightType.Point;
        hitLight.color = Color.yellow;
        hitLight.intensity = 2f;
        hitLight.range = 5f;

        Destroy(hitObj, 0.05f);
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

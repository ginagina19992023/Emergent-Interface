using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Physics-based helicopter controller with shooting.
/// Requires a Rigidbody and HelicopterInput on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HelicopterInput))]
public class HelicopterController : MonoBehaviour
{
    public Vector3 LastRespawnPointPosition { get; private set; }
    public Quaternion LastRespawnPointRotation { get; private set; }
    [Header("Respawn")]
    [Tooltip("Player health source used to trigger teleport-to-checkpoint when damage is taken.")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Seconds to fade from clear to full black before teleport.")]
    [SerializeField] private float respawnFadeToBlackSeconds = 0.3f;
    [Tooltip("Seconds to fade from black back to gameplay while showing countdown.")]
    [SerializeField] private float respawnCountdownSeconds = 3f;
    [Tooltip("Seconds to keep a full black screen with crash message before countdown starts.")]
    [SerializeField] private float respawnMessageHoldSeconds = 2f;
    [Tooltip("Color used for the respawn fullscreen fade.")]
    [SerializeField] private Color respawnFadeColor = Color.black;

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

    [Tooltip("Pitch angle in degrees that is always enforced (locks nose up/down).")]
    [SerializeField] private float lockedPitchAngle = 5f;

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

    [Tooltip("Linear drag applied by the Rigidbody (set on Start).")]
    [SerializeField] private float linearDrag = 1f;

    [Tooltip("Angular drag applied by the Rigidbody (set on Start).")]
    [SerializeField] private float angularDrag = 4f;

    [Header("Upright (collision stability)")]
    [Tooltip("Each physics step, force world roll (euler Z) to zero so bumps cannot bank the craft.")]
    [SerializeField] private bool enforceUprightNoRoll = true;

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

    [Tooltip("Volume for the gun sound.")]
    [Range(0f, 3f)]
    [SerializeField] private float shootSoundVolume = 1.25f;

    [Tooltip("Sound played when a target is hit (but not destroyed).")]
    [SerializeField] private AudioClip hitMarkerSound;

    [Tooltip("Volume for the hit marker sound.")]
    [Range(0f, 3f)]
    [SerializeField] private float hitMarkerSoundVolume = 1.25f;

    [Tooltip("UI image shown briefly whenever the weapon fires.")]
    [SerializeField] private Image shootExplosionImage;

    [Tooltip("How long the ShootExplosion image stays visible per shot (seconds).")]
    [SerializeField] private float shootExplosionDuration = 0.08f;

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
    private bool hasHealthSnapshot;
    private int previousHealth;
    private bool isRespawning;
    private Canvas respawnCanvas;
    private Image respawnFadeImage;
    private Text respawnCountdownText;
    private Text respawnMessageText;
    private bool defaultUseGravity = true;
    private bool verticalMotionLocked;
    private Coroutine shootExplosionRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<HelicopterInput>();
        LastRespawnPointPosition = transform.position;
        LastRespawnPointRotation = transform.rotation;
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;

        rb.useGravity = true;
        defaultUseGravity = rb.useGravity;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);

        if (shootExplosionImage != null)
            shootExplosionImage.enabled = false;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        if (isRespawning)
            Time.timeScale = 1f;
    }

    public void SetInitialSpawnPoint(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        LastRespawnPointPosition = spawnPosition;
        LastRespawnPointRotation = ConstrainRotationForFlight(spawnRotation);
    }

    public void SetLastRespawnPoint(Vector3 respawnPointCenter, Quaternion respawnRotation)
    {
        LastRespawnPointPosition = respawnPointCenter;
        LastRespawnPointRotation = ConstrainRotationForFlight(respawnRotation);
    }

    void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (!hasHealthSnapshot)
        {
            previousHealth = currentHealth;
            hasHealthSnapshot = true;
            return;
        }

        if (currentHealth < previousHealth && currentHealth > 0 && !isRespawning)
            StartCoroutine(RespawnSequenceRoutine());

        previousHealth = currentHealth;
    }

    public void TeleportToLastRespawnPoint()
    {
        Quaternion respawnRotation = ConstrainRotationForFlight(LastRespawnPointRotation);
        LastRespawnPointRotation = respawnRotation;
        transform.SetPositionAndRotation(LastRespawnPointPosition, respawnRotation);
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = LastRespawnPointPosition;
            rb.rotation = respawnRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public Quaternion GetFlightAlignedRotation(Quaternion rawRotation)
    {
        return ConstrainRotationForFlight(rawRotation);
    }

    public void SetVerticalMotionLocked(bool locked)
    {
        verticalMotionLocked = locked;
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.useGravity = locked ? false : defaultUseGravity;
        if (locked)
        {
            Vector3 v = rb.linearVelocity;
            if (v.y > 0f)
                v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    Quaternion ConstrainRotationForFlight(Quaternion rawRotation)
    {
        Vector3 e = rawRotation.eulerAngles;
        e.x = lockedPitchAngle;
        if (enforceUprightNoRoll)
            e.z = 0f;
        return Quaternion.Euler(e);
    }

    IEnumerator RespawnSequenceRoutine()
    {
        isRespawning = true;
        EnsureRespawnOverlayExists();
        respawnCanvas.gameObject.SetActive(true);

        float fadeIn = Mathf.Max(0.01f, respawnFadeToBlackSeconds);
        float countdownDuration = Mathf.Max(0.01f, respawnCountdownSeconds);
        float holdDuration = Mathf.Max(0f, respawnMessageHoldSeconds);

        yield return FadeOverlayAlpha(0f, 1f, fadeIn);

        TeleportToLastRespawnPoint();
        Time.timeScale = 0f;
        respawnMessageText.text = "You crashed, respawning...";
        respawnMessageText.enabled = true;
        respawnCountdownText.text = string.Empty;
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        respawnMessageText.enabled = false;

        float elapsed = 0f;
        while (elapsed < countdownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / countdownDuration);
            SetOverlayAlpha(Mathf.Lerp(1f, 0f, t));

            int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(countdownDuration - elapsed), 1, Mathf.CeilToInt(countdownDuration));
            respawnCountdownText.text = secondsLeft.ToString();
            yield return null;
        }

        SetOverlayAlpha(0f);
        respawnCountdownText.text = string.Empty;
        respawnMessageText.enabled = true;
        respawnCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
        isRespawning = false;
    }

    IEnumerator FadeOverlayAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetOverlayAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetOverlayAlpha(to);
    }

    void SetOverlayAlpha(float alpha)
    {
        if (respawnFadeImage == null)
            return;
        Color c = respawnFadeColor;
        c.a = alpha;
        respawnFadeImage.color = c;
    }

    void EnsureRespawnOverlayExists()
    {
        if (respawnCanvas != null && respawnFadeImage != null && respawnCountdownText != null && respawnMessageText != null)
            return;

        GameObject canvasGo = new GameObject("RespawnTransitionUI");
        respawnCanvas = canvasGo.AddComponent<Canvas>();
        respawnCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        respawnCanvas.sortingOrder = 10000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject fadeGo = new GameObject("Fade");
        fadeGo.transform.SetParent(canvasGo.transform, false);
        RectTransform fadeRt = fadeGo.AddComponent<RectTransform>();
        StretchFull(fadeRt);
        respawnFadeImage = fadeGo.AddComponent<Image>();
        respawnFadeImage.raycastTarget = false;

        GameObject textGo = new GameObject("Countdown");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(400f, 200f);
        textRt.anchoredPosition = Vector2.zero;
        respawnCountdownText = textGo.AddComponent<Text>();
        respawnCountdownText.alignment = TextAnchor.MiddleCenter;
        respawnCountdownText.fontSize = 96;
        respawnCountdownText.fontStyle = FontStyle.Bold;
        respawnCountdownText.color = new Color(1f, 1f, 1f, 0.95f);
        respawnCountdownText.raycastTarget = false;
        respawnCountdownText.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, 96);
        if (respawnCountdownText.font == null)
            respawnCountdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject messageGo = new GameObject("Message");
        messageGo.transform.SetParent(canvasGo.transform, false);
        RectTransform messageRt = messageGo.AddComponent<RectTransform>();
        messageRt.anchorMin = new Vector2(0.5f, 0.5f);
        messageRt.anchorMax = new Vector2(0.5f, 0.5f);
        messageRt.pivot = new Vector2(0.5f, 0.5f);
        messageRt.sizeDelta = new Vector2(900f, 80f);
        messageRt.anchoredPosition = new Vector2(0f, 90f);
        respawnMessageText = messageGo.AddComponent<Text>();
        respawnMessageText.alignment = TextAnchor.MiddleCenter;
        respawnMessageText.fontSize = 44;
        respawnMessageText.fontStyle = FontStyle.Bold;
        respawnMessageText.color = new Color(1f, 1f, 1f, 0.95f);
        respawnMessageText.raycastTarget = false;
        respawnMessageText.text = "You crashed, respawning...";
        respawnMessageText.font = respawnCountdownText.font;

        canvasGo.SetActive(false);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!verticalMotionLocked)
        {
            ApplyLift();
            ApplyFall();
        }
        ApplyForwardMovement();
        ApplyYaw();
        ApplyShake();
        EnforceUprightStability();
        EnforceLockedPitchAndZeroWorldRoll();
        if (verticalMotionLocked)
        {
            Vector3 v = rb.linearVelocity;
            if (v.y > 0f)
                v.y = 0f;
            rb.linearVelocity = v;
        }
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
        if (Time.timeScale <= 0f)
            return;

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

    private void ApplyForwardMovement()
    {
        if (forwardSpeed <= 0f)
            return;
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

    private void ApplyShake()
    {
        if (!enableShake) return;

        float time = Time.time * shakeSpeed;

        float shakeX = (Mathf.PerlinNoise(time, noiseOffsetX) - 0.5f) * 2f;
        float shakeY = (Mathf.PerlinNoise(time, noiseOffsetY) - 0.5f) * 2f;
        float shakeZ = (Mathf.PerlinNoise(time, noiseOffsetZ) - 0.5f) * 2f;

        Vector3 positionShake = new Vector3(shakeX, shakeY, shakeZ) * positionShakeIntensity;
        rb.AddForce(positionShake, ForceMode.VelocityChange);

        // No roll component — keeps the body level with freezeWorldRoll / upright feel
        Vector3 rotationShake = new Vector3(shakeX, shakeY, 0f) * rotationShakeIntensity;
        Quaternion shakeRotation = Quaternion.Euler(rotationShake * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * shakeRotation);
    }

    private void EnforceUprightStability()
    {
        Vector3 av = rb.angularVelocity;
        av.z = 0f;
        av.x = 0f;
        rb.angularVelocity = av;
    }

    private void EnforceLockedPitchAndZeroWorldRoll()
    {
        Vector3 e = rb.rotation.eulerAngles;
        e.x = lockedPitchAngle;
        if (enforceUprightNoRoll)
            e.z = 0f;
        rb.MoveRotation(Quaternion.Euler(e));
    }

    private void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + transform.forward * 2f;
        Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;

        // Shoot SFX
        if (shootSound != null && shootAudioSource != null)
            shootAudioSource.PlayOneShot(shootSound, shootSoundVolume);

        // Muzzle flash
        TriggerMuzzleFlash(origin);
        TriggerShootExplosionFlash();

        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers);

        if (didHit)
        {
            FloatingTarget target = hit.collider.GetComponentInParent<FloatingTarget>();
            if (target != null)
            {
                target.TakeHit(damage);

                // Hit marker SFX
                if (hitMarkerSound != null && shootAudioSource != null)
                    shootAudioSource.PlayOneShot(hitMarkerSound, hitMarkerSoundVolume);
            }

            SpawnHitEffect(hit.point, hit.normal);
        }
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

    private void TriggerShootExplosionFlash()
    {
        if (shootExplosionImage == null)
            return;

        if (shootExplosionRoutine != null)
            StopCoroutine(shootExplosionRoutine);
        shootExplosionRoutine = StartCoroutine(ShootExplosionFlashRoutine());
    }

    private IEnumerator ShootExplosionFlashRoutine()
    {
        shootExplosionImage.enabled = true;
        yield return new WaitForSeconds(Mathf.Max(0f, shootExplosionDuration));
        shootExplosionImage.enabled = false;
        shootExplosionRoutine = null;
    }
}

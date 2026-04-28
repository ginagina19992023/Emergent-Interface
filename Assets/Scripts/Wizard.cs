using UnityEngine;

public class Wizard : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Magic Ball prefab to spawn every interval.")]
    [SerializeField] private GameObject magicBallPrefab;

    [Tooltip("Seconds between spawn attempts.")]
    [SerializeField] private float spawnIntervalSeconds = 10f;

    [Tooltip("Optional custom spawn point. If empty, wizard transform is used.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Target")]
    [Tooltip("Optional helicopter target. If empty, the first HelicopterController is used.")]
    [SerializeField] private Transform helicopterTransform;

    [Header("Health")]
    [Tooltip("How many hit points the wizard has.")]
    [SerializeField] private float health = 10f;

    [Header("Score")]
    [Tooltip("Points added to the player score when the wizard dies.")]
    [SerializeField] private int pointsOnDeath = 100;

    [Header("Audio")]
    [Tooltip("Sound played when the wizard is hit but not killed.")]
    [SerializeField] private AudioClip hitSound;

    [Tooltip("Volume for the hit sound.")]
    [Range(0f, 3f)]
    [SerializeField] private float hitSoundVolume = 1f;

    [Tooltip("Sound played when the wizard dies.")]
    [SerializeField] private AudioClip deathSound;

    [Tooltip("Volume for the death sound.")]
    [Range(0f, 3f)]
    [SerializeField] private float deathSoundVolume = 1.5f;

    [Tooltip("Optional AudioSource for wizard SFX. If empty, one is created and configured as 2D.")]
    [SerializeField] private AudioSource wizardAudioSource;

    [Header("Blood VFX")]
    [Tooltip("Optional one-shot effect spawned at the bullet impact (e.g. Particle System prefab). If empty, procedural blood spheres are used.")]
    [SerializeField] private GameObject bloodBurstPrefab;

    [Tooltip("Number of blood spheres on a non-lethal hit (ignored if Blood Burst Prefab is assigned).")]
    [SerializeField] private int bloodDropletsPerHit = 12;

    [Tooltip("Extra blood spheres when this hit kills the wizard.")]
    [SerializeField] private int bloodDropletsOnKill = 10;

    [Tooltip("Minimum sphere scale for procedural blood.")]
    [SerializeField] private float bloodSphereScaleMin = 0.04f;

    [Tooltip("Maximum sphere scale for procedural blood.")]
    [SerializeField] private float bloodSphereScaleMax = 0.11f;

    [Tooltip("Impulse strength for procedural blood spheres.")]
    [SerializeField] private float bloodBurstForce = 9f;

    [Tooltip("Seconds before procedural blood spheres are destroyed.")]
    [SerializeField] private float bloodLifetime = 1.4f;

    [Tooltip("Color of procedural blood spheres.")]
    [SerializeField] private Color bloodColor = new Color(0.5f, 0.02f, 0.04f, 1f);

    private float spawnTimer;
    private bool warnedMissingPrefab;
    private bool isDead;

    void Awake()
    {
        spawnTimer = Mathf.Max(0.01f, spawnIntervalSeconds);
        ResolveHelicopterTransformIfNeeded();

        if (wizardAudioSource == null)
            wizardAudioSource = GetComponent<AudioSource>();
        if (wizardAudioSource == null)
            wizardAudioSource = gameObject.AddComponent<AudioSource>();

        wizardAudioSource.playOnAwake = false;
        wizardAudioSource.spatialBlend = 0f;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = Mathf.Max(0.01f, spawnIntervalSeconds);
        TrySpawnMagicBall();
    }

    void TrySpawnMagicBall()
    {
        if (magicBallPrefab == null)
        {
            if (!warnedMissingPrefab)
            {
                Debug.LogWarning("Wizard: Magic Ball prefab is not assigned.", this);
                warnedMissingPrefab = true;
            }
            return;
        }

        ResolveHelicopterTransformIfNeeded();
        if (helicopterTransform == null)
            return;

        Vector3 toHelicopter = helicopterTransform.position - transform.position;
        if (toHelicopter.sqrMagnitude <= 0.0001f)
            return;

        bool helicopterIsInFront = Vector3.Dot(transform.forward, toHelicopter.normalized) > 0f;
        if (!helicopterIsInFront)
            return;

        Transform spawnSource = spawnPoint != null ? spawnPoint : transform;
        Instantiate(magicBallPrefab, spawnSource.position, spawnSource.rotation);
    }

    void ResolveHelicopterTransformIfNeeded()
    {
        if (helicopterTransform != null)
            return;

        HelicopterController controller = FindFirstObjectByType<HelicopterController>();
        if (controller != null)
            helicopterTransform = controller.transform;
    }

    public void TakeHit(float damageAmount)
    {
        Vector3 fallbackImpact = transform.position + Vector3.up * 0.5f;
        TakeHit(damageAmount, fallbackImpact, Vector3.up);
    }

    public void TakeHit(float damageAmount, Vector3 impactWorldPosition, Vector3 impactWorldNormal)
    {
        if (isDead || damageAmount <= 0f)
            return;

        bool willKill = health - damageAmount <= 0f;
        SpawnBloodAtImpact(impactWorldPosition, impactWorldNormal, willKill);

        health -= damageAmount;
        if (health > 0f)
        {
            if (hitSound != null && wizardAudioSource != null)
                wizardAudioSource.PlayOneShot(hitSound, hitSoundVolume);
            return;
        }

        isDead = true;

        if (pointsOnDeath != 0 && PlayerScore.Instance != null)
            PlayerScore.Instance.AddScore(pointsOnDeath);

        if (deathSound != null)
            PlayDeathSound();

        Destroy(gameObject);
    }

    private void SpawnBloodAtImpact(Vector3 position, Vector3 normal, bool lethalHit)
    {
        if (bloodBurstPrefab != null)
        {
            Quaternion rot = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;
            GameObject fx = Instantiate(bloodBurstPrefab, position, rot);
            Destroy(fx, 4f);
            return;
        }

        Vector3 outward = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        int count = bloodDropletsPerHit + (lethalHit ? bloodDropletsOnKill : 0);
        count = Mathf.Max(0, count);

        float scaleMin = Mathf.Min(bloodSphereScaleMin, bloodSphereScaleMax);
        float scaleMax = Mathf.Max(bloodSphereScaleMin, bloodSphereScaleMax);

        for (int i = 0; i < count; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "WizardBlood";
            drop.transform.position = position + Random.insideUnitSphere * 0.06f;
            drop.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);

            Renderer rend = drop.GetComponent<Renderer>();
            rend.material.color = bloodColor;

            Rigidbody rb = drop.AddComponent<Rigidbody>();
            rb.mass = 0.02f;
            rb.linearDamping = 0.5f;
            Vector3 dir = (outward * 1.2f + Random.insideUnitSphere).normalized;
            rb.AddForce(dir * Random.Range(bloodBurstForce * 0.45f, bloodBurstForce), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            Destroy(drop, bloodLifetime);
        }
    }

    private void PlayDeathSound()
    {
        if (wizardAudioSource != null && wizardAudioSource.gameObject != gameObject)
        {
            wizardAudioSource.PlayOneShot(deathSound, deathSoundVolume);
            return;
        }

        GameObject deathSfxObject = new GameObject("Wizard_DeathSFX");
        deathSfxObject.transform.position = transform.position;

        AudioSource detachedSource = deathSfxObject.AddComponent<AudioSource>();
        detachedSource.playOnAwake = false;
        detachedSource.clip = deathSound;
        detachedSource.volume = deathSoundVolume;
        detachedSource.spatialBlend = wizardAudioSource != null ? wizardAudioSource.spatialBlend : 0f;
        if (wizardAudioSource != null)
            detachedSource.outputAudioMixerGroup = wizardAudioSource.outputAudioMixerGroup;

        detachedSource.Play();
        Destroy(deathSfxObject, deathSound.length + 0.1f);
    }
}

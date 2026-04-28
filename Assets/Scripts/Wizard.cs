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
        if (isDead || damageAmount <= 0f)
            return;

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

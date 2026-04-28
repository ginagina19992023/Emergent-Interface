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

    private float spawnTimer;
    private bool warnedMissingPrefab;

    void Awake()
    {
        spawnTimer = Mathf.Max(0.01f, spawnIntervalSeconds);
        ResolveHelicopterTransformIfNeeded();
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
}

using UnityEngine;

/// <summary>
/// Spawns FloatingTarget prefabs across the map.
/// When enough targets have been destroyed, spawns a new wave.
/// </summary>
public class TargetSpawner : MonoBehaviour
{
  [Tooltip("The FloatingTarget prefab to spawn.")]
  [SerializeField] private GameObject targetPrefab;

  [Tooltip("Maximum number of targets alive at once.")]
  [SerializeField] private int maxTargets = 10;

  [Tooltip("A new wave spawns when alive count drops to this fraction of max (0-1).")]
  [SerializeField] private float respawnThreshold = 0.3f;

  [Tooltip("How many targets to spawn per wave.")]
  [SerializeField] private int targetsPerWave = 5;

  [Header("Spawn Area")]
  [Tooltip("Centre of the spawn area (world space). Uses this transform's position if left at zero.")]
  [SerializeField] private Vector3 spawnCenter;

  [Tooltip("Half-size of the horizontal spawn area.")]
  [SerializeField] private float spawnRadius = 100f;

  [Tooltip("Minimum spawn height.")]
  [SerializeField] private float minHeight = 20f;

  [Tooltip("Maximum spawn height.")]
  [SerializeField] private float maxHeight = 60f;

  private int aliveCount;

  void Start()
  {
    if (spawnCenter == Vector3.zero)
      spawnCenter = transform.position;

    // Initial wave fills to max
    SpawnWave(maxTargets);
  }

  void Update()
  {
    if (aliveCount <= Mathf.FloorToInt(maxTargets * respawnThreshold))
    {
      int toSpawn = Mathf.Min(targetsPerWave, maxTargets - aliveCount);
      if (toSpawn > 0)
        SpawnWave(toSpawn);
    }
  }

  private void SpawnWave(int count)
  {
    for (int i = 0; i < count; i++)
    {
      Vector3 pos = spawnCenter + new Vector3(
          Random.Range(-spawnRadius, spawnRadius),
          Random.Range(minHeight, maxHeight),
          Random.Range(-spawnRadius, spawnRadius)
      );

      GameObject target = Instantiate(targetPrefab, pos, Quaternion.identity);
      aliveCount++;

      // Listen for destruction so we can track the count
      var ft = target.GetComponent<FloatingTarget>();
      if (ft != null)
        ft.OnDestroyed += () => aliveCount--;
    }
  }
}

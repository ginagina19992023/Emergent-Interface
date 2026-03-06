using System;
using UnityEngine;

/// <summary>
/// A floating target that drifts around slowly like a hot-air balloon.
/// It bobs up and down with a sine wave and wanders horizontally.
/// Destroyed when it has taken enough hits.
/// </summary>
public class FloatingTarget : MonoBehaviour
{
  [Header("Floating Motion")]
  [Tooltip("Amplitude of the vertical bobbing in units.")]
  [SerializeField] private float bobAmplitude = 3f;

  [Tooltip("Speed of the vertical bobbing cycle.")]
  [SerializeField] private float bobSpeed = 0.5f;

  [Tooltip("Maximum horizontal drift speed.")]
  [SerializeField] private float driftSpeed = 2f;

  [Tooltip("How often the drift direction changes (seconds).")]
  [SerializeField] private float driftChangeInterval = 4f;

  [Header("Health")]
  [Tooltip("How many hit points the target has.")]
  [SerializeField] private float health = 3f;

  [Header("Audio")]
  [Tooltip("Sound played when the target is hit but not destroyed.")]
  [SerializeField] private AudioClip hitSound;

  [Tooltip("Sound played when the target is destroyed.")]
  [SerializeField] private AudioClip destroySound;

  [Header("VFX")]
  [Tooltip("Particle system prefab spawned on destruction (explosion).")]
  [SerializeField] private GameObject explosionPrefab;

  /// <summary>Invoked just before the target is destroyed.</summary>
  public event Action OnDestroyed;

  private Vector3 startPosition;
  private Vector3 driftDirection;
  private float driftTimer;
  private float timeOffset;

  void Start()
  {
    startPosition = transform.position;
    timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    PickNewDriftDirection();
  }

  void Update()
  {
    // Vertical bobbing
    float yOffset = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmplitude;

    // Horizontal drift
    driftTimer -= Time.deltaTime;
    if (driftTimer <= 0f)
      PickNewDriftDirection();

    startPosition += driftDirection * driftSpeed * Time.deltaTime;

    transform.position = startPosition + Vector3.up * yOffset;
  }

  private void PickNewDriftDirection()
  {
    float angle = UnityEngine.Random.Range(0f, 360f);
    driftDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    driftTimer = driftChangeInterval;
  }

  public void TakeHit(float damageAmount)
  {
    health -= damageAmount;

    if (health <= 0f)
    {
      // Explosion VFX
      if (explosionPrefab != null)
      {
        GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(fx, 3f);
      }

      // Destroy sound (played at position so it outlives this object)
      if (destroySound != null)
        AudioSource.PlayClipAtPoint(destroySound, transform.position);

      OnDestroyed?.Invoke();
      Destroy(gameObject);
    }
    else
    {
      // Hit sound
      if (hitSound != null)
        AudioSource.PlayClipAtPoint(hitSound, transform.position);
    }
  }
}

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

  private Vector3 startPosition;
  private Vector3 driftDirection;
  private float driftTimer;
  private float timeOffset;

  void Start()
  {
    startPosition = transform.position;
    timeOffset = Random.Range(0f, Mathf.PI * 2f);
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
    float angle = Random.Range(0f, 360f);
    driftDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    driftTimer = driftChangeInterval;
  }

  public void TakeHit(float damageAmount)
  {
    health -= damageAmount;
    if (health <= 0f)
      Destroy(gameObject);
  }
}

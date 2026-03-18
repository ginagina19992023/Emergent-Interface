using System;
using System.Collections;
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

  [Header("Hit Flash")]
  [Tooltip("Color to flash when hit.")]
  [SerializeField] private Color flashColor = Color.red;

  [Tooltip("Duration of the hit flash in seconds.")]
  [SerializeField] private float flashDuration = 0.1f;

  [Header("Debris")]
  [Tooltip("Number of debris pieces to spawn on death.")]
  [SerializeField] private int debrisCount = 8;

  [Tooltip("Force applied to debris pieces.")]
  [SerializeField] private float debrisForce = 15f;

  [Tooltip("How long debris pieces last before being destroyed.")]
  [SerializeField] private float debrisLifetime = 3f;

  /// <summary>Invoked just before the target is destroyed.</summary>
  public event Action OnDestroyed;

  private Vector3 startPosition;
  private Vector3 driftDirection;
  private float driftTimer;
  private float timeOffset;

  private Renderer[] renderers;
  private Color[] originalColors;
  private Coroutine flashCoroutine;

  void Start()
  {
    startPosition = transform.position;
    timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    PickNewDriftDirection();
    CacheRenderers();
  }

  private void CacheRenderers()
  {
    renderers = GetComponentsInChildren<Renderer>();
    originalColors = new Color[renderers.Length];
    for (int i = 0; i < renderers.Length; i++)
    {
      if (renderers[i].material.HasProperty("_Color"))
        originalColors[i] = renderers[i].material.color;
      else if (renderers[i].material.HasProperty("_BaseColor"))
        originalColors[i] = renderers[i].material.GetColor("_BaseColor");
      else
        originalColors[i] = Color.white;
    }
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

      SpawnDebris();

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

      TriggerFlash();
    }
  }

  private void TriggerFlash()
  {
    if (flashCoroutine != null)
      StopCoroutine(flashCoroutine);
    flashCoroutine = StartCoroutine(FlashRoutine());
  }

  private IEnumerator FlashRoutine()
  {
    SetRenderersColor(flashColor);
    yield return new WaitForSeconds(flashDuration);
    RestoreRenderersColor();
    flashCoroutine = null;
  }

  private void SetRenderersColor(Color color)
  {
    foreach (var rend in renderers)
    {
      if (rend == null) continue;
      if (rend.material.HasProperty("_Color"))
        rend.material.color = color;
      else if (rend.material.HasProperty("_BaseColor"))
        rend.material.SetColor("_BaseColor", color);
    }
  }

  private void RestoreRenderersColor()
  {
    for (int i = 0; i < renderers.Length; i++)
    {
      if (renderers[i] == null) continue;
      if (renderers[i].material.HasProperty("_Color"))
        renderers[i].material.color = originalColors[i];
      else if (renderers[i].material.HasProperty("_BaseColor"))
        renderers[i].material.SetColor("_BaseColor", originalColors[i]);
    }
  }

  private void SpawnDebris()
  {
    for (int i = 0; i < debrisCount; i++)
    {
      GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
      debris.transform.position = transform.position + UnityEngine.Random.insideUnitSphere * 0.5f;
      debris.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.3f, 0.8f);
      debris.transform.rotation = UnityEngine.Random.rotation;

      // Copy color from original
      Renderer debrisRend = debris.GetComponent<Renderer>();
      if (renderers.Length > 0 && renderers[0] != null)
        debrisRend.material.color = originalColors[0];

      Rigidbody rb = debris.AddComponent<Rigidbody>();
      Vector3 explosionDir = (debris.transform.position - transform.position).normalized;
      if (explosionDir.sqrMagnitude < 0.01f)
        explosionDir = UnityEngine.Random.onUnitSphere;
      rb.AddForce(explosionDir * debrisForce + Vector3.up * debrisForce * 0.5f, ForceMode.Impulse);
      rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);

      Destroy(debris, debrisLifetime);
    }
  }
}

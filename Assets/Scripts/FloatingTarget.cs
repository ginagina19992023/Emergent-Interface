using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Target that can stay still or move back and forth between two points (offsets from its spawn position) at a set speed.
/// Destroyed when it has taken enough hits.
/// </summary>
public class FloatingTarget : MonoBehaviour
{
  [Header("Movement")]
  [Tooltip("If true, the target does not move.")]
  [SerializeField] private bool stationary = true;

  [Tooltip("Offset from the target's starting position to the first path endpoint.")]
  [SerializeField] private Vector3 pointA;

  [Tooltip("Offset from the target's starting position to the second path endpoint.")]
  [SerializeField] private Vector3 pointB;

  [Tooltip("Travel speed along the segment, in units per second.")]
  [SerializeField] private float moveSpeed = 2f;

  [Header("Health")]
  [Tooltip("How many hit points the target has.")]
  [SerializeField] private float health = 3f;

  [Header("Score")]
  [Tooltip("Points added to the player score when this target is destroyed.")]
  [SerializeField] private int pointsOnDestroy = 100;

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

  private Renderer[] renderers;
  private Color[] originalColors;
  private Coroutine flashCoroutine;

  /// <summary>Accumulated motion along the path; PingPong(phase, 1) is the Lerp t between A and B.</summary>
  private float pathPhase;

  /// <summary>World position where the target was at spawn (movement path is relative to this).</summary>
  private Vector3 pathOrigin;

  void Awake()
  {
    pathOrigin = transform.position;
  }

  void Start()
  {
    CacheRenderers();
    if (!stationary && moveSpeed > 0f)
      pathPhase = GetPhaseForCurrentPosition();
  }

  private Vector3 WorldPointA => pathOrigin + pointA;
  private Vector3 WorldPointB => pathOrigin + pointB;

  /// <summary>Phase value such that Lerp(world A, world B, PingPong(phase,1)) matches the closest point on the segment to this transform.</summary>
  private float GetPhaseForCurrentPosition()
  {
    Vector3 a = WorldPointA;
    Vector3 b = WorldPointB;
    Vector3 ab = b - a;
    float spanSq = ab.sqrMagnitude;
    if (spanSq < 0.0001f)
      return 0f;
    float t = Mathf.Clamp01(Vector3.Dot(transform.position - a, ab) / spanSq);
    return t;
  }

  void OnValidate()
  {
    if (moveSpeed < 0f)
      moveSpeed = 0f;
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
    if (stationary || moveSpeed <= 0f)
      return;

    Vector3 wA = WorldPointA;
    Vector3 wB = WorldPointB;
    float span = Vector3.Distance(wA, wB);
    if (span < 0.0001f)
      return;

    pathPhase += (moveSpeed / span) * Time.deltaTime;
    float t = Mathf.PingPong(pathPhase, 1f);
    transform.position = Vector3.Lerp(wA, wB, t);
  }

  public void TakeHit(float damageAmount)
  {
    health -= damageAmount;

    if (health <= 0f)
    {
      if (pointsOnDestroy != 0 && PlayerScore.Instance != null)
        PlayerScore.Instance.AddScore(pointsOnDestroy);

      if (explosionPrefab != null)
      {
        GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(fx, 3f);
      }

      SpawnDebris();

      if (destroySound != null)
        AudioSource.PlayClipAtPoint(destroySound, transform.position);

      OnDestroyed?.Invoke();
      Destroy(gameObject);
    }
    else
    {
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

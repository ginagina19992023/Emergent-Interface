using UnityEngine;

/// <summary>
/// Attach to a bullet prefab. Destroys itself after a set lifetime
/// and handles collision with other objects.
/// The prefab needs a Rigidbody and a Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
  [Tooltip("Seconds before the bullet auto-destroys.")]
  [SerializeField] private float lifetime = 5f;

  void Start()
  {
    Destroy(gameObject, lifetime);
  }

  void OnCollisionEnter(Collision collision)
  {
    // Ignore the helicopter that fired us (same layer or tag check can be added)
    Destroy(gameObject);
  }
}

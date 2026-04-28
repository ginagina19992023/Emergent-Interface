using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MagicBall : MonoBehaviour
{
    [Tooltip("Straight-line travel speed in units per second.")]
    [SerializeField] private float speed = 18f;

    [Tooltip("Seconds before this projectile is auto-destroyed.")]
    [SerializeField] private float lifetime = 6f;

    [Tooltip("How much damage is applied to the helicopter on impact.")]
    [SerializeField] private int contactDamage = 1;

    [Tooltip("Optional direct helicopter reference. If empty, it is found automatically.")]
    [SerializeField] private Transform helicopterTransform;

    [Tooltip("Optional direct health reference. If empty, it is found automatically.")]
    [SerializeField] private PlayerHealth playerHealth;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (helicopterTransform == null)
            helicopterTransform = ResolveHelicopterTransform();

        Vector3 direction = transform.forward;
        if (helicopterTransform != null)
        {
            Vector3 toHelicopter = helicopterTransform.position - transform.position;
            if (toHelicopter.sqrMagnitude > 0.0001f)
                direction = toHelicopter.normalized;
        }

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);

    }

    private Transform ResolveHelicopterTransform()
    {
        HelicopterController controller = FindFirstObjectByType<HelicopterController>();
        if (controller != null)
            return controller.transform;

        GameObject helicopterObject = GameObject.Find("Helicopter");
        if (helicopterObject != null)
            return helicopterObject.transform;

        return null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root == helicopterTransform || collision.gameObject.GetComponentInParent<HelicopterController>() != null)
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (playerHealth != null && playerHealth.isActiveAndEnabled)
                playerHealth.TakeDamage(contactDamage);
        }

        Destroy(gameObject);
    }
}

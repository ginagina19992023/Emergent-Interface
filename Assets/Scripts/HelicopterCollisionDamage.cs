using UnityEngine;

/// <summary>
/// Applies collision damage to <see cref="PlayerHealth"/> on the game-state object. Must live on the helicopter (the object with the collider).
/// </summary>
public class HelicopterCollisionDamage : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    float _invulnerableUntil;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (playerHealth == null || !playerHealth.isActiveAndEnabled)
            return;
        if (Time.time < _invulnerableUntil)
            return;
        if (collision.gameObject.GetComponent<Bullet>() != null)
            return;

        playerHealth.TakeDamage(1);
        _invulnerableUntil = Time.time + playerHealth.HitInvulnerabilitySeconds;
    }
}

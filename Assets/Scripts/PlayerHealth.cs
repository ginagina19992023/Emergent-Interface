using System;
using UnityEngine;

/// <summary>
/// Player health on the helicopter. Solid collisions cost 1 HP (with a short invulnerability window).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Starting and maximum health (adjust in the Inspector).")]
    [SerializeField] private int maxHealth = 5;

    [Tooltip("Seconds after a hit before another collision can deal damage.")]
    [SerializeField] private float hitInvulnerabilitySeconds = 0.35f;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }

    public event Action<int, int> OnHealthChanged;

    float _invulnerableUntil;

    void Awake()
    {
        CurrentHealth = Mathf.Max(0, maxHealth);
    }

    void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }

    /// <summary>Apply damage (e.g. from collisions). Clamped to at least 0 current health.</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isActiveAndEnabled)
            return;
        if (Time.time < _invulnerableUntil)
            return;
        if (collision.gameObject.GetComponent<Bullet>() != null)
            return;

        TakeDamage(1);
        _invulnerableUntil = Time.time + hitInvulnerabilitySeconds;
    }
}

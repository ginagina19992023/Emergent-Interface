using System;
using UnityEngine;

/// <summary>
/// Player health (typically on a <see cref="GameState"/> object). Collision damage is forwarded from the helicopter via <see cref="HelicopterCollisionDamage"/>.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Starting and maximum health (adjust in the Inspector).")]
    [SerializeField] private int maxHealth = 5;

    [Tooltip("Seconds after a hit before another collision can deal damage (used by HelicopterCollisionDamage).")]
    [SerializeField] private float hitInvulnerabilitySeconds = 0.35f;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public float HitInvulnerabilitySeconds => hitInvulnerabilitySeconds;

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    bool _deathEventRaised;

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
        if (CurrentHealth <= 0 && !_deathEventRaised)
        {
            _deathEventRaised = true;
            OnPlayerDied?.Invoke();
        }
    }
}

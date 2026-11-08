using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumHealth = 100f;
    [SerializeField, Min(0f)] private float damageImmunityDuration = 0.75f;

    private float currentHealth;
    private float nextDamageTime = float.NegativeInfinity;
    private int lastDamageFrame = -1;
    private bool gameOverLocked;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maximumHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> HealthChanged;
    public event Action<PlayerHealth> Died;

    private void Awake()
    {
        maximumHealth = Mathf.Max(1f, maximumHealth);
        currentHealth = maximumHealth;
    }

    public bool TakeDamage(float amount)
    {
        if (gameOverLocked
            || IsDead
            || amount <= 0f
            || float.IsNaN(amount)
            || float.IsInfinity(amount)
            || Time.frameCount == lastDamageFrame
            || Time.unscaledTime < nextDamageTime)
        {
            return false;
        }

        lastDamageFrame = Time.frameCount;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        nextDamageTime = Time.unscaledTime + damageImmunityDuration;
        HealthChanged?.Invoke(currentHealth, maximumHealth);

        if (IsDead)
        {
            Died?.Invoke(this);
        }

        return true;
    }

    public bool Heal(float amount)
    {
        if (gameOverLocked
            || IsDead
            || amount <= 0f
            || float.IsNaN(amount)
            || float.IsInfinity(amount))
        {
            return false;
        }

        float healedHealth = Mathf.Min(maximumHealth, currentHealth + amount);
        if (Mathf.Approximately(healedHealth, currentHealth))
        {
            return false;
        }

        currentHealth = healedHealth;
        HealthChanged?.Invoke(currentHealth, maximumHealth);
        return true;
    }

    public void LockForGameOver()
    {
        gameOverLocked = true;
    }
}

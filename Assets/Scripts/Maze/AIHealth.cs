using System;
using UnityEngine;

public class AIHealth : MonoBehaviour
{
    public const int MaximumHearts = 3;

    [SerializeField, Min(0f)] private float damageImmunityDuration = 1f;

    private int currentHealth = MaximumHearts;
    private float nextDamageTime = float.NegativeInfinity;
    private int lastDamageFrame = -1;
    private bool gameOverLocked;
    private AICharacterController aiController;
    private Rigidbody2D body;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => MaximumHearts;
    public bool IsDead => currentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action<AIHealth> Died;

    private void Awake()
    {
        currentHealth = MaximumHearts;
        aiController = GetComponent<AICharacterController>();
        body = GetComponent<Rigidbody2D>();
    }

    public bool TakeDamage()
    {
        if (gameOverLocked
            || IsDead
            || Time.frameCount == lastDamageFrame
            || Time.unscaledTime < nextDamageTime)
        {
            return false;
        }

        lastDamageFrame = Time.frameCount;
        currentHealth = Mathf.Max(0, currentHealth - 1);
        nextDamageTime = Time.unscaledTime + damageImmunityDuration;
        HealthChanged?.Invoke(currentHealth, MaximumHearts);

        if (IsDead)
        {
            if (aiController != null)
            {
                aiController.enabled = false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            Died?.Invoke(this);
        }

        return true;
    }

    public bool Heal(int amount)
    {
        if (gameOverLocked || IsDead || amount <= 0)
        {
            return false;
        }

        int healedHealth = Mathf.Min(MaximumHearts, currentHealth + amount);
        if (healedHealth == currentHealth)
        {
            return false;
        }

        currentHealth = healedHealth;
        HealthChanged?.Invoke(currentHealth, MaximumHearts);
        return true;
    }

    public void LockForGameOver()
    {
        gameOverLocked = true;
        if (aiController != null)
        {
            aiController.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}

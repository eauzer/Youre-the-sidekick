using System;
using UnityEngine;

namespace Beginner2D
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 60f;
        [SerializeField, Min(0f)] private float deathDelay = 0.2f;
        [SerializeField] private string deathAnimationTrigger;

        private float currentHealth;
        private bool isDead;
        private bool deathStarted;
        private Collider2D[] colliders;
        private Rigidbody2D body;
        private Animator animator;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public bool IsDead => isDead;
        public event Action<EnemyHealth> OnDeath;
        public event Action<float, float> HealthChanged;

        private void Awake()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
            currentHealth = maximumHealth;
            colliders = GetComponentsInChildren<Collider2D>(true);
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public bool TakeDamage(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return false;
            }

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maximumHealth);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
            if (currentHealth <= 0f)
            {
                Die();
            }

            return true;
        }

        private void Die()
        {
            if (deathStarted)
            {
                return;
            }

            deathStarted = true;
            isDead = true;
            OnDeath?.Invoke(this);

            foreach (Collider2D enemyCollider in colliders)
            {
                if (enemyCollider != null)
                {
                    enemyCollider.enabled = false;
                }
            }

            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = false;
                }
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }

            if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                animator.SetTrigger(deathAnimationTrigger);
            }

            Destroy(gameObject, deathDelay);
        }
    }
}

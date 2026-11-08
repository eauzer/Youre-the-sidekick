using UnityEngine;

namespace Beginner2D
{
    public class EnemyProjectile : MonoBehaviour
    {
        [Min(0f)] public float damage = 15f;
        public float lifetime = 5f;
        private Rigidbody2D body;
        private Collider2D[] projectileColliders;
        private Transform owner;
        private bool hasHit;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            projectileColliders = GetComponentsInChildren<Collider2D>(true);
            if (projectileColliders.Length == 0)
            {
                projectileColliders = new Collider2D[] { gameObject.AddComponent<CircleCollider2D>() };
            }

            foreach (Collider2D projectileCollider in projectileColliders)
            {
                projectileCollider.isTrigger = false;
            }
        }

        void Start()
        {
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Collider2D ownerCollider, float damageAmount)
        {
            owner = ownerCollider != null ? ownerCollider.transform.root : null;
            if (damageAmount > 0f)
            {
                damage = damageAmount;
            }

            if (ownerCollider == null)
            {
                return;
            }

            foreach (Collider2D projectileCollider in projectileColliders)
            {
                foreach (Collider2D ownerPart in owner.GetComponentsInChildren<Collider2D>(true))
                {
                    Physics2D.IgnoreCollision(projectileCollider, ownerPart);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider);
        }

        private void HandleHit(Collider2D other)
        {
            if (hasHit || other == null || (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))))
            {
                return;
            }

            global::PlayerHealth playerHealth = other.GetComponentInParent<global::PlayerHealth>();
            if (playerHealth != null)
            {
                hasHit = true;
                if (!playerHealth.IsDead)
                {
                    playerHealth.TakeDamage(damage);
                }

                Destroy(gameObject);
                return;
            }

            if (!other.isTrigger)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}

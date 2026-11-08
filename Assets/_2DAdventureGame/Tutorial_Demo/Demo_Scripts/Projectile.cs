using UnityEngine;

namespace Beginner2D
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 25f;

        private Rigidbody2D rigidbody2d;
        private bool hitConsumed;

        private void Awake()
        {
            Helpers.RecursiveLayerSet(transform, Helpers.ProjectileLayer);
            rigidbody2d = GetComponent<Rigidbody2D>();
            if (rigidbody2d != null)
            {
                rigidbody2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void OnEnable()
        {
            Helpers.RecursiveLayerSet(transform, Helpers.ProjectileLayer);
        }

        private void Update()
        {
            if (transform.position.magnitude > 1000f)
            {
                Destroy(gameObject);
            }
        }

        public void Launch(Vector2 direction, float force, Collider2D thrower = null)
        {
            Collider2D projectileCollider = GetComponentInChildren<Collider2D>();
            if (thrower != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, thrower);
            }

            if (rigidbody2d != null)
            {
                rigidbody2d.AddForce(direction * force);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleImpact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.collider);
        }

        private void HandleImpact(Collider2D other)
        {
            if (hitConsumed || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<global::PlayerHealth>() != null
                || other.transform.root.CompareTag("Player"))
            {
                return;
            }

            hitConsumed = true;
            global::Beginner2D.EnemyHealth enemyHealth = other.GetComponentInParent<global::Beginner2D.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else
            {
                Enemy legacyEnemy = other.GetComponentInParent<Enemy>();
                if (legacyEnemy != null)
                {
                    legacyEnemy.Fix();
                }
            }

            Destroy(gameObject);
        }
    }
}
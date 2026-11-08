using UnityEngine;

namespace Beginner2D
{
    public class WallTurretAI : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform player;

        [Header("Detection")]
        public float detectionRange = 8f;
        public bool requireLineOfSight = true;
        public LayerMask wallLayerMask;

        [Header("Firing")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float projectileSpeed = 7f;
        [Min(0f)] public float projectileDamage = 20f;
        public float fireRateMin = 2f;
        public float fireRateMax = 4f;

        float fireTimer;
        EnemyHealth enemyHealth;

        void Start()
        {
            enemyHealth = GetComponent<EnemyHealth>();

            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            fireTimer = Random.Range(fireRateMin, fireRateMax);
        }

        void Update()
        {
            if (player == null || (enemyHealth != null && enemyHealth.IsDead)) return;

            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > detectionRange) return;

            if (requireLineOfSight && !HasLineOfSight()) return;

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = Random.Range(fireRateMin, fireRateMax);
            }
        }

        bool HasLineOfSight()
        {
            Vector2 origin = firePoint != null ? firePoint.position : transform.position;
            Vector2 dir = (Vector2)player.position - origin;
            float distance = dir.magnitude;

            int layerMask = wallLayerMask.value == 0 ? Physics2D.DefaultRaycastLayers : wallLayerMask.value;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir.normalized, distance, layerMask);
            float nearestHitDistance = float.PositiveInfinity;
            Collider2D nearestCollider = null;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.distance < nearestHitDistance)
                {
                    nearestHitDistance = hit.distance;
                    nearestCollider = hit.collider;
                }
            }

            return nearestCollider == null || nearestCollider.GetComponentInParent<PlayerHealth>() != null;
        }

        void Fire()
        {
            if (projectilePrefab == null || player == null || (enemyHealth != null && enemyHealth.IsDead)) return;

            Vector2 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 dir = ((Vector2)player.position - spawnPos).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

            EnemyProjectile enemyProjectile = proj.GetComponent<EnemyProjectile>();
            if (enemyProjectile != null)
                enemyProjectile.Initialize(GetComponentInChildren<Collider2D>(), projectileDamage);

            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.linearVelocity = dir * projectileSpeed;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

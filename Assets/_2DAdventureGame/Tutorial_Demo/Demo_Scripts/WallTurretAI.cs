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
        public float fireRateMin = 2f;
        public float fireRateMax = 4f;

        float fireTimer;

        void Start()
        {
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
            if (player == null) return;

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

            RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, distance, wallLayerMask);
            return hit.collider == null;
        }

        void Fire()
        {
            if (projectilePrefab == null || player == null) return;

            Vector2 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 dir = ((Vector2)player.position - spawnPos).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = dir * projectileSpeed;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

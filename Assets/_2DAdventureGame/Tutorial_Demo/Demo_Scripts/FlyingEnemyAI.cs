using System.Collections;
using UnityEngine;

namespace Beginner2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyMazeNavigation))]
    public class FlyingEnemyAI : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform player;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float preferredDistanceMin = 5f;
        public float preferredDistanceMax = 10f;
        public float strafeReverseMinTime = 2f;
        public float strafeReverseMaxTime = 5f;

        [Header("Firing")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float projectileSpeed = 8f;
        [Min(0f)] public float projectileDamage = 15f;
        public int burstCount = 3;
        public float burstInterval = 0.15f;
        public float spreadAngle = 10f;
        public float fireRateMin = 2f;
        public float fireRateMax = 5f;

        int strafeDirection = 1;
        float strafeTimer;
        float fireTimer;
        bool isFiring;
        Rigidbody2D body;
        EnemyMazeNavigation navigation;
        EnemyHealth enemyHealth;
        Vector2 movementTarget;
        bool hasMovementTarget;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            navigation = GetComponent<EnemyMazeNavigation>();
            enemyHealth = GetComponent<EnemyHealth>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Start()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            strafeTimer = Random.Range(strafeReverseMinTime, strafeReverseMaxTime);
            fireTimer = Random.Range(fireRateMin, fireRateMax);
        }

        void Update()
        {
            if (player == null || (enemyHealth != null && enemyHealth.IsDead)) return;

            FacePlayer();
            ChooseMovementTarget();
            HandleStrafeReversal();
            HandleFiring();
        }

        void FixedUpdate()
        {
            if (player != null && navigation != null && hasMovementTarget
                && (enemyHealth == null || !enemyHealth.IsDead))
            {
                navigation.TryMoveTowards(movementTarget, moveSpeed);
            }
        }

        void FacePlayer()
        {
            Vector2 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        void ChooseMovementTarget()
        {
            Vector2 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            Vector2 dirToPlayer = toPlayer.normalized;
            Vector2 strafeDir = new Vector2(-dirToPlayer.y, dirToPlayer.x) * strafeDirection;

            Vector2 moveDir = strafeDir;

            if (distance > preferredDistanceMax)
                moveDir += dirToPlayer;
            else if (distance < preferredDistanceMin)
                moveDir -= dirToPlayer;

            hasMovementTarget = moveDir.sqrMagnitude > 0.001f;
            if (hasMovementTarget)
                movementTarget = body.position + moveDir.normalized * Mathf.Max(1f, moveSpeed);
        }

        void HandleStrafeReversal()
        {
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeDirection = -strafeDirection;
                strafeTimer = Random.Range(strafeReverseMinTime, strafeReverseMaxTime);
            }
        }

        void HandleFiring()
        {
            if (isFiring) return;

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                StartCoroutine(FireBurst());
                fireTimer = Random.Range(fireRateMin, fireRateMax);
            }
        }

        IEnumerator FireBurst()
        {
            isFiring = true;

            for (int i = 0; i < burstCount; i++)
            {
                FireProjectile();
                if (i < burstCount - 1)
                    yield return new WaitForSeconds(burstInterval);
            }

            isFiring = false;
        }

        void FireProjectile()
        {
            if (projectilePrefab == null || player == null || (enemyHealth != null && enemyHealth.IsDead)) return;

            Vector2 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 aimDir = ((Vector2)player.position - spawnPos).normalized;

            float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            float offset = Random.Range(-spreadAngle, spreadAngle);
            float finalAngle = (baseAngle + offset) * Mathf.Deg2Rad;

            Vector2 fireDir = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            EnemyProjectile enemyProjectile = proj.GetComponent<EnemyProjectile>();
            if (enemyProjectile != null)
                enemyProjectile.Initialize(GetComponentInChildren<Collider2D>(), projectileDamage);

            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.linearVelocity = fireDir * projectileSpeed;
            }
        }
    }
}

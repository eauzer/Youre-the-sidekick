using UnityEngine;

namespace Beginner2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyMazeNavigation))]
    public class GroundEnemyAI : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform player;

        [Header("Pathfinding")]
        public MazePathfinder pathfinder;
        public float pathRecalculateInterval = 0.5f;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float waypointReachDistance = 0.1f;

        [Header("Combat")]
        public int contactDamage = 1;
        [Min(0.05f)] public float contactAttackCooldown = 1f;

        Rigidbody2D rb;
        EnemyMazeNavigation navigation;
        EnemyHealth enemyHealth;
        float nextAttackTime;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            navigation = GetComponent<EnemyMazeNavigation>();
            enemyHealth = GetComponent<EnemyHealth>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Start()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (pathfinder == null)
                pathfinder = MazePathfinder.Instance;
        }

        void FixedUpdate()
        {
            if (player == null || navigation == null || (enemyHealth != null && enemyHealth.IsDead))
            {
                return;
            }
            navigation.TryMoveTowards(player.position, moveSpeed);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        void TryDamagePlayer(Collider2D other)
        {
            if (Time.time < nextAttackTime || (enemyHealth != null && enemyHealth.IsDead))
                return;

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsDead)
                return;

            Vector2 closestPoint = other.ClosestPoint(rb.position);
            if (Vector2.Distance(rb.position, closestPoint) > waypointReachDistance + 0.75f)
                return;

            if (playerHealth.TakeDamage(contactDamage))
                nextAttackTime = Time.time + contactAttackCooldown;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Beginner2D
{
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

        Rigidbody2D rb;
        List<Vector3> path;
        int currentWaypoint;
        float pathTimer;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();

            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (pathfinder == null)
                pathfinder = MazePathfinder.Instance;

            pathTimer = 0f;
        }

        void Update()
        {
            if (player == null || pathfinder == null) return;

            pathTimer -= Time.deltaTime;
            if (pathTimer <= 0f)
            {
                RecalculatePath();
                pathTimer = pathRecalculateInterval;
            }
        }

        void FixedUpdate()
        {
            if (path == null || currentWaypoint >= path.Count) return;

            Vector2 target = path[currentWaypoint];
            Vector2 current = rb.position;
            Vector2 direction = (target - current);
            float distance = direction.magnitude;

            if (distance <= waypointReachDistance)
            {
                currentWaypoint++;
                return;
            }

            Vector2 move = direction.normalized * moveSpeed * Time.deltaTime;
            if (move.magnitude > distance)
                move = direction;

            rb.MovePosition(current + move);
        }

        void RecalculatePath()
        {
            path = pathfinder.FindPath(transform.position, player.position);
            currentWaypoint = path != null && path.Count > 1 ? 1 : 0;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
                playerController.ChangeHealth(-contactDamage);
        }
    }
}

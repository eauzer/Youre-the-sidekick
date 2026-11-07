using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beginner2D
{
    public class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        public class EnemyType
        {
            public GameObject prefab;
            public int maxActive = 5;
            public bool requiresWalkable = true;
            public bool spawnNearWalls;
        }

        [Header("Enemy Types")]
        public EnemyType[] enemyTypes;

        [Header("References")]
        public MazeGenerator mazeGenerator;
        public Transform player;

        [Header("Spawn Settings")]
        public float spawnRangeMin = 12f;
        public float spawnRangeMax = 25f;
        public float spawnCheckInterval = 1f;
        public int maxTotalEnemies = 15;

        [Header("Despawn Settings")]
        public float despawnDistance = 30f;
        public float despawnCheckInterval = 2f;

        Camera mainCamera;
        float spawnTimer;
        float despawnTimer;
        List<GameObject>[] activeEnemies;

        void Start()
        {
            mainCamera = Camera.main;

            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            activeEnemies = new List<GameObject>[enemyTypes.Length];
            for (int i = 0; i < enemyTypes.Length; i++)
                activeEnemies[i] = new List<GameObject>();

            spawnTimer = spawnCheckInterval;
            despawnTimer = despawnCheckInterval;
        }

        void Update()
        {
            if (player == null || mazeGenerator == null || mainCamera == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                TrySpawn();
                spawnTimer = spawnCheckInterval;
            }

            despawnTimer -= Time.deltaTime;
            if (despawnTimer <= 0f)
            {
                DespawnDistant();
                despawnTimer = despawnCheckInterval;
            }
        }

        void TrySpawn()
        {
            int totalActive = 0;
            for (int i = 0; i < activeEnemies.Length; i++)
            {
                CleanupNulls(activeEnemies[i]);
                totalActive += activeEnemies[i].Count;
            }

            if (totalActive >= maxTotalEnemies) return;

            int typeIndex = PickSpawnableType();
            if (typeIndex == -1) return;

            Vector3? spawnPos = FindSpawnPosition(enemyTypes[typeIndex]);
            if (!spawnPos.HasValue) return;

            GameObject enemy = Instantiate(enemyTypes[typeIndex].prefab, spawnPos.Value, Quaternion.identity);
            activeEnemies[typeIndex].Add(enemy);
        }

        int PickSpawnableType()
        {
            int start = UnityEngine.Random.Range(0, enemyTypes.Length);
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                int idx = (start + i) % enemyTypes.Length;
                if (activeEnemies[idx].Count < enemyTypes[idx].maxActive)
                    return idx;
            }
            return -1;
        }

        Vector3? FindSpawnPosition(EnemyType enemyType)
        {
            Vector3 playerPos = player.position;
            Vector2Int playerCell = mazeGenerator.WorldToGrid(playerPos);

            int cellMin = Mathf.RoundToInt(spawnRangeMin / mazeGenerator.CellSize);
            int cellMax = Mathf.RoundToInt(spawnRangeMax / mazeGenerator.CellSize);

            const int maxAttempts = 30;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int offsetX = UnityEngine.Random.Range(-cellMax, cellMax + 1);
                int offsetY = UnityEngine.Random.Range(-cellMax, cellMax + 1);
                int manhattanDist = Mathf.Abs(offsetX) + Mathf.Abs(offsetY);
                if (manhattanDist < cellMin || manhattanDist > cellMax)
                    continue;

                Vector2Int candidate = playerCell + new Vector2Int(offsetX, offsetY);

                if (candidate.x < 0 || candidate.x >= mazeGenerator.Width ||
                    candidate.y < 0 || candidate.y >= mazeGenerator.Height)
                    continue;

                bool walkable = mazeGenerator.IsWalkable(candidate);
                if (enemyType.requiresWalkable && !walkable) continue;
                if (!enemyType.requiresWalkable && walkable) continue;

                if (enemyType.spawnNearWalls && !IsAdjacentToWall(candidate)) continue;

                Vector3 worldPos = mazeGenerator.GridToWorld(candidate);

                if (IsVisibleToCamera(worldPos)) continue;

                return worldPos;
            }

            return null;
        }

        bool IsAdjacentToWall(Vector2Int cell)
        {
            return !mazeGenerator.IsWalkable(cell + Vector2Int.up) ||
                   !mazeGenerator.IsWalkable(cell + Vector2Int.down) ||
                   !mazeGenerator.IsWalkable(cell + Vector2Int.left) ||
                   !mazeGenerator.IsWalkable(cell + Vector2Int.right);
        }

        bool IsVisibleToCamera(Vector3 worldPos)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(worldPos);
            return viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                   viewportPoint.y > -0.05f && viewportPoint.y < 1.05f;
        }

        void DespawnDistant()
        {
            float despawnSqr = despawnDistance * despawnDistance;
            Vector3 playerPos = player.position;

            for (int i = 0; i < activeEnemies.Length; i++)
            {
                for (int j = activeEnemies[i].Count - 1; j >= 0; j--)
                {
                    GameObject enemy = activeEnemies[i][j];
                    if (enemy == null)
                    {
                        activeEnemies[i].RemoveAt(j);
                        continue;
                    }

                    if ((enemy.transform.position - playerPos).sqrMagnitude > despawnSqr)
                    {
                        Destroy(enemy);
                        activeEnemies[i].RemoveAt(j);
                    }
                }
            }
        }

        void CleanupNulls(List<GameObject> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }
        }
    }
}

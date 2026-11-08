using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(0)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float minimumDistanceFromStart = 8f;
    [SerializeField, Min(0f)] private float minimumDistanceFromExit = 6f;
    [SerializeField, Min(0f)] private float minimumEnemySpacing = 5f;
    [SerializeField, Min(0f)] private float minimumCharacterDistance = 4f;
    [SerializeField, Min(0f)] private float spawnDelay;
    [SerializeField] private int enemySortingOrder = 10;

    private MazeGenerator mazeGenerator;
    private MazeRenderer mazeRenderer;
    private AStarPathfinder pathfinder;
    private Transform enemyContainer;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<Vector2> spawnedPositions = new List<Vector2>();
    private bool spawnInProgress;
    private int spawnVersion;

    public IReadOnlyList<GameObject> SpawnedEnemies => spawnedEnemies;

    private IEnumerator Start()
    {
        yield return SpawnEnemiesAfterMazeReady();
    }

    public IEnumerator SpawnEnemiesAfterMazeReady()
    {
        if (spawnInProgress)
        {
            yield break;
        }

        spawnInProgress = true;
        int currentSpawnVersion = ++spawnVersion;
        ResolveReferences();
        while (currentSpawnVersion == spawnVersion && isActiveAndEnabled && (mazeGenerator == null || mazeRenderer == null
            || mazeGenerator.Width == 0 || mazeGenerator.Height == 0 || !mazeRenderer.IsRendered))
        {
            ResolveReferences();
            yield return null;
        }

        if (currentSpawnVersion != spawnVersion || !isActiveAndEnabled
            || enemyPrefab == null || mazeGenerator == null || mazeRenderer == null)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner has no Enemy prefab assigned.", this);
            }

            if (currentSpawnVersion == spawnVersion)
            {
                spawnInProgress = false;
            }

            yield break;
        }

        Physics2D.SyncTransforms();
        Transform aiTransform = FindAnyObjectByType<AICharacterController>()?.transform;
        Transform playerTransform = FindAnyObjectByType<PlayerHealth>()?.transform;
        List<Vector2Int> candidates = mazeGenerator.GetWalkablePositions();
        Shuffle(candidates);
        EnsureEnemyContainer();

        int spawnedCount = 0;
        foreach (Vector2Int cell in candidates)
        {
            if (spawnedCount >= enemyCount)
            {
                break;
            }

            if (currentSpawnVersion != spawnVersion)
            {
                yield break;
            }

            if (!IsValidSpawnCell(cell, aiTransform, playerTransform, spawnedPositions))
            {
                continue;
            }

            GameObject enemy = Instantiate(
                enemyPrefab,
                mazeGenerator.GridToWorld(cell),
                Quaternion.identity,
                enemyContainer);
            Beginner2D.EnemyMazeNavigation navigation = enemy.GetComponent<Beginner2D.EnemyMazeNavigation>();
            if (navigation != null)
            {
                navigation.SetMazeReferences(mazeGenerator, pathfinder);
            }

            foreach (SpriteRenderer sprite in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprite.sortingOrder += enemySortingOrder;
            }

            spawnedEnemies.Add(enemy);
            spawnedPositions.Add(mazeGenerator.GridToWorld(cell));
            spawnedCount++;

            if (spawnDelay > 0f && spawnedCount < enemyCount)
            {
                yield return new WaitForSeconds(spawnDelay);
                if (currentSpawnVersion != spawnVersion)
                {
                    yield break;
                }
            }
        }

        if (spawnedCount < enemyCount)
        {
            Debug.LogWarning($"EnemySpawner placed {spawnedCount} of {enemyCount} requested enemies because too few valid cells were available.", this);
        }

        if (currentSpawnVersion == spawnVersion)
        {
            spawnInProgress = false;
        }
    }

    public void ClearEnemies()
    {
        spawnVersion++;
        spawnInProgress = false;
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            enemy.SetActive(false);
            Destroy(enemy);
        }

        spawnedEnemies.Clear();
        spawnedPositions.Clear();
        if (enemyContainer != null)
        {
            enemyContainer.gameObject.SetActive(false);
            Destroy(enemyContainer.gameObject);
            enemyContainer = null;
        }
    }

    private void ResolveReferences()
    {
        if (mazeGenerator == null)
        {
            mazeGenerator = GetComponent<MazeGenerator>();
        }

        if (mazeRenderer == null)
        {
            mazeRenderer = GetComponent<MazeRenderer>();
        }

        if (pathfinder == null)
        {
            pathfinder = FindAnyObjectByType<AStarPathfinder>();
        }
    }

    private bool IsValidSpawnCell(
        Vector2Int cell,
        Transform aiTransform,
        Transform playerTransform,
        List<Vector2> existingPositions)
    {
        if (!mazeGenerator.IsWalkable(cell)
            || cell == mazeGenerator.StartPosition
            || cell == mazeGenerator.ExitPosition)
        {
            return false;
        }

        Vector2 candidate = mazeGenerator.GridToWorld(cell);
        Vector2 start = mazeGenerator.GridToWorld(mazeGenerator.StartPosition);
        Vector2 exit = mazeGenerator.GridToWorld(mazeGenerator.ExitPosition);
        if (Vector2.Distance(candidate, start) < minimumDistanceFromStart
            || Vector2.Distance(candidate, exit) < minimumDistanceFromExit)
        {
            return false;
        }

        if ((aiTransform != null && Vector2.Distance(candidate, aiTransform.position) < minimumCharacterDistance)
            || (playerTransform != null && Vector2.Distance(candidate, playerTransform.position) < minimumCharacterDistance))
        {
            return false;
        }

        foreach (Vector2 existingPosition in existingPositions)
        {
            if (Vector2.Distance(candidate, existingPosition) < minimumEnemySpacing)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureEnemyContainer()
    {
        if (enemyContainer != null)
        {
            return;
        }

        enemyContainer = new GameObject("Spawned Enemies").transform;
        enemyContainer.SetParent(transform, false);
    }

    private void Shuffle(List<Vector2Int> cells)
    {
        for (int index = cells.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (cells[index], cells[swapIndex]) = (cells[swapIndex], cells[index]);
        }
    }
}
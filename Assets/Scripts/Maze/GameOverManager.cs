using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private AIHealth aiHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private MazeRenderer mazeRenderer;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private AICharacterController aiController;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private string mainMenuScenePath = "Assets/Scenes/WelcomeScreen.unity";

    private bool gameOver;
    private bool roundTransitionInProgress;
    private Beginner2D.PlayerController playerController;
    private Rigidbody2D playerBody;

    public bool IsGameOver => gameOver;

    private void Start()
    {
        ResolveRoundReferences();

        if (aiHealth == null)
        {
            aiHealth = FindAnyObjectByType<AIHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (healthUI == null)
        {
            healthUI = GetComponent<HealthUI>();
        }

        if (aiHealth != null)
        {
            aiHealth.Died += HandleAIDeath;
        }
        else
        {
            Debug.LogError("GameOverManager could not find AIHealth.", this);
        }

        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDeath;
        }
        else
        {
            Debug.LogError("GameOverManager could not find PlayerHealth.", this);
        }

        if (aiController != null)
        {
            aiController.ReachedExit += HandleVictory;
        }
        else
        {
            Debug.LogError("GameOverManager could not find AICharacterController.", this);
        }
    }

    private void OnDestroy()
    {
        if (aiHealth != null)
        {
            aiHealth.Died -= HandleAIDeath;
        }

        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDeath;
        }

        if (aiController != null)
        {
            aiController.ReachedExit -= HandleVictory;
        }
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScenePath);
    }

    private void ResolveRoundReferences()
    {
        if (aiController == null)
        {
            aiController = FindAnyObjectByType<AICharacterController>();
        }

        if (mazeGenerator == null)
        {
            mazeGenerator = FindAnyObjectByType<MazeGenerator>();
        }

        if (mazeRenderer == null)
        {
            mazeRenderer = FindAnyObjectByType<MazeRenderer>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        if (pathfinder == null && aiController != null)
        {
            pathfinder = aiController.GetComponent<AStarPathfinder>();
        }

        if (pathfinder == null)
        {
            pathfinder = FindAnyObjectByType<AStarPathfinder>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerController = playerHealth.GetComponent<Beginner2D.PlayerController>();
            playerBody = playerHealth.GetComponent<Rigidbody2D>();
        }
    }

    private void HandleVictory()
    {
        if (gameOver || roundTransitionInProgress || aiHealth == null || aiHealth.IsDead
            || playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        ResolveRoundReferences();
        if (mazeGenerator == null || mazeRenderer == null || pathfinder == null
            || aiController == null || enemySpawner == null)
        {
            Debug.LogError("GameOverManager cannot regenerate the maze because a maze reference is missing.", this);
            return;
        }

        roundTransitionInProgress = true;
        StartCoroutine(StartNextRound());
    }

    private IEnumerator StartNextRound()
    {
        aiHealth.LockForRoundTransition();
        playerHealth.LockForRoundTransition();
        aiController.PauseForRoundTransition();

        bool restorePlayerController = playerController != null && playerController.enabled;
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
        }

        enemySpawner.ClearEnemies();

        Beginner2D.EnemyProjectile[] enemyProjectiles = FindObjectsByType<Beginner2D.EnemyProjectile>();
        foreach (Beginner2D.EnemyProjectile projectile in enemyProjectiles)
        {
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }
        }

        yield return new WaitForFixedUpdate();

        bool generatedReachableMaze = false;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            mazeRenderer.RegenerateMaze();
            Physics2D.SyncTransforms();
            if (mazeGenerator.IsWalkable(mazeGenerator.StartPosition)
                && mazeGenerator.IsWalkable(mazeGenerator.ExitPosition)
                && pathfinder.FindPath(mazeGenerator.StartPosition, mazeGenerator.ExitPosition).Count > 0)
            {
                generatedReachableMaze = true;
                break;
            }

            yield return null;
        }

        if (!generatedReachableMaze)
        {
            Debug.LogError("MazeGenerator failed to create a connected path from start to exit after five attempts.", this);
            roundTransitionInProgress = false;
            aiController.ResetForNewRound(aiController.transform.position);
            aiHealth.ResetForNewRound();
            playerHealth.ResetForNewRound();
            if (playerController != null)
            {
                playerController.enabled = restorePlayerController;
            }

            yield break;
        }

        Vector3 startWorld = mazeGenerator.GridToWorld(mazeGenerator.StartPosition);
        Vector2 aiSpawn = new Vector2(startWorld.x, startWorld.y);
        aiController.ResetForNewRound(aiSpawn);
        Physics2D.SyncTransforms();

        if (TryFindPlayerSpawn(aiSpawn, out Vector2 playerSpawn))
        {
            SetPlayerPosition(playerSpawn);
        }
        else
        {
            Debug.LogError("No clear walkable cell was found for the Player in the regenerated maze.", this);
        }

        aiHealth.ResetForNewRound();
        playerHealth.ResetForNewRound();
        yield return enemySpawner.SpawnEnemiesAfterMazeReady();
        if (playerController != null)
        {
            playerController.enabled = restorePlayerController;
        }

        roundTransitionInProgress = false;
    }

    private bool TryFindPlayerSpawn(Vector2 aiSpawn, out Vector2 spawnPosition)
    {
        spawnPosition = Vector2.zero;
        float minimumDistance = mazeGenerator.CellSize * 0.9f;
        int shortestPathLength = int.MaxValue;
        List<Vector2Int> exitPath = pathfinder.FindPath(mazeGenerator.StartPosition, mazeGenerator.ExitPosition);
        for (int index = 1; index < exitPath.Count; index++)
        {
            Vector3 pathWorld = mazeGenerator.GridToWorld(exitPath[index]);
            Vector2 candidate = new Vector2(pathWorld.x, pathWorld.y);
            if (Vector2.Distance(candidate, aiSpawn) >= minimumDistance && IsPlayerPositionClear(candidate))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        List<Vector2Int> walkableCells = mazeGenerator.GetWalkablePositions();
        foreach (Vector2Int cell in walkableCells)
        {
            if (cell == mazeGenerator.StartPosition)
            {
                continue;
            }

            Vector3 cellWorld = mazeGenerator.GridToWorld(cell);
            Vector2 candidate = new Vector2(cellWorld.x, cellWorld.y);
            if (Vector2.Distance(candidate, aiSpawn) < minimumDistance)
            {
                continue;
            }

            List<Vector2Int> path = pathfinder.FindPath(mazeGenerator.StartPosition, cell);
            if (path.Count == 0 || path.Count >= shortestPathLength || !IsPlayerPositionClear(candidate))
            {
                continue;
            }

            shortestPathLength = path.Count;
            spawnPosition = candidate;
        }

        return shortestPathLength != int.MaxValue;
    }

    private bool IsPlayerPositionClear(Vector2 position)
    {
        Collider2D playerCollider = playerHealth.GetComponentInChildren<Collider2D>();
        if (playerCollider == null)
        {
            return Physics2D.OverlapCircle(position, mazeGenerator.CellSize * 0.25f) == null;
        }

        Vector2 centerOffset = (Vector2)(playerCollider.bounds.center - playerHealth.transform.position);
        Vector2 overlapSize = (Vector2)playerCollider.bounds.size * 0.9f;
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            position + centerOffset,
            overlapSize,
            playerHealth.transform.eulerAngles.z);

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.transform == playerHealth.transform || overlap.transform.IsChildOf(playerHealth.transform)
                || overlap.transform == aiController.transform || overlap.transform.IsChildOf(aiController.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void SetPlayerPosition(Vector2 position)
    {
        Transform playerTransform = playerHealth.transform;
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = position;
        }

        playerTransform.position = new Vector3(position.x, position.y, mazeGenerator.transform.position.z);
    }

    private void HandleAIDeath(AIHealth health)
    {
        TriggerGameOver("Your AI companion was lost.");
    }

    private void HandlePlayerDeath(PlayerHealth health)
    {
        TriggerGameOver("The sidekick was defeated.");
    }

    private void TriggerGameOver(string reason)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        if (aiHealth != null)
        {
            aiHealth.LockForGameOver();
        }

        if (playerHealth != null)
        {
            playerHealth.LockForGameOver();
            Beginner2D.PlayerController playerController = playerHealth.GetComponent<Beginner2D.PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            Rigidbody2D playerBody = playerHealth.GetComponent<Rigidbody2D>();
            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector2.zero;
            }
        }

        if (healthUI != null)
        {
            healthUI.ShowGameOver(reason);
        }
        else
        {
            Debug.LogError("GameOverManager could not find HealthUI to display the game-over screen.", this);
        }

        Time.timeScale = 0f;
    }
}

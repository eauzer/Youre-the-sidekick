using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class AICharacterController : MonoBehaviour
{
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float movementSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float maximumPlayerDistance = 12f;
    [SerializeField, Min(0f)] private float distanceResumeBuffer = 2f;
    [SerializeField, Min(0f)] private float minimumSpawnDistanceFromPlayer = 1.25f;
    [SerializeField, Min(0f)] private float blockedRepathDelay = 0.75f;
    [SerializeField, Min(1)] private int healthPoints = 3;

    [Header("Animation")]
    [SerializeField] private bool flipToFaceMovement = true;
    [SerializeField] private bool spriteFacesRight = true;

    private Rigidbody2D body;
    private CircleCollider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private List<Vector2Int> path = new List<Vector2Int>();
    private int nextPathIndex;
    private bool waitingForPlayer;
    private bool reachedExit;
    private float blockedDuration;
    private Vector2 lastObservedPosition;
    private bool movementWasRequested;
    private float retryPathTimer;
    private bool warnedMissingPlayer;
    private Sprite placeholderSprite;
    private Texture2D placeholderTexture;

    public int HealthPoints => healthPoints;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        bodyCollider.isTrigger = false;
        bodyCollider.radius = 0.35f;

        if (spriteRenderer.sprite == null)
        {
            // No art assigned: use a blue square so the AI is still visible.
            CreatePlaceholderSprite();
            spriteRenderer.color = new Color(0.2f, 0.45f, 1f, 1f);
        }
        else
        {
            // Real sprite: no tint, so the walk animation shows its true colors.
            spriteRenderer.color = Color.white;
        }

        spriteRenderer.sortingOrder = 3;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (mazeGenerator == null)
        {
            mazeGenerator = FindAnyObjectByType<MazeGenerator>();
        }

        if (pathfinder == null)
        {
            pathfinder = GetComponent<AStarPathfinder>();
        }

        if (mazeGenerator == null || pathfinder == null)
        {
            Debug.LogError("AICharacterController requires MazeGenerator and AStarPathfinder references.", this);
            enabled = false;
            yield break;
        }

        if (mazeGenerator.Width == 0 || mazeGenerator.Height == 0)
        {
            mazeGenerator.GenerateMaze();
        }

        yield return null;
        Physics2D.SyncTransforms();
        ResolvePlayer();

        if (!TryChooseSpawnPosition(out Vector2 spawnPosition))
        {
            Debug.LogError("No safe walkable tile was found for AICharacter.", this);
            enabled = false;
            yield break;
        }

        body.position = spawnPosition;
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, mazeGenerator.transform.position.z);
        lastObservedPosition = spawnPosition;
        RequestPathToExit();
    }

    private void OnDestroy()
    {
        if (placeholderSprite != null)
        {
            Destroy(placeholderSprite);
        }

        if (placeholderTexture != null)
        {
            Destroy(placeholderTexture);
        }
    }

    private void Update()
    {
        if (mazeGenerator == null || reachedExit)
        {
            return;
        }

        ResolvePlayer();
        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(body.position, player.position);
        float resumeDistance = Mathf.Max(0f, maximumPlayerDistance - distanceResumeBuffer);

        if (!waitingForPlayer && distance > maximumPlayerDistance)
        {
            waitingForPlayer = true;
        }
        else if (waitingForPlayer && distance <= resumeDistance)
        {
            waitingForPlayer = false;
        }
    }

    private void FixedUpdate()
    {
        if (mazeGenerator == null || reachedExit || waitingForPlayer)
        {
            return;
        }

        if (movementWasRequested)
        {
            if (Vector2.Distance(body.position, lastObservedPosition) < 0.001f)
            {
                blockedDuration += Time.fixedDeltaTime;
            }
            else
            {
                blockedDuration = 0f;
            }
        }

        lastObservedPosition = body.position;

        if (path == null || path.Count == 0 || nextPathIndex >= path.Count)
        {
            retryPathTimer += Time.fixedDeltaTime;
            if (retryPathTimer >= 0.5f)
            {
                retryPathTimer = 0f;
                RequestPathToExit();
            }

            return;
        }

        Vector2Int nextCell = path[nextPathIndex];
        if (!mazeGenerator.IsWalkable(nextCell))
        {
            RequestPathToExit();
            return;
        }

        Vector3 targetWorld = mazeGenerator.GridToWorld(nextCell);
        Vector2 destination = new Vector2(targetWorld.x, targetWorld.y);
        float remainingDistance = Vector2.Distance(body.position, destination);
        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            destination,
            movementSpeed * Time.fixedDeltaTime);

        FaceMovement(destination - body.position);

        body.MovePosition(nextPosition);
        movementWasRequested = remainingDistance > 0.02f;

        if (remainingDistance <= Mathf.Max(0.02f, movementSpeed * Time.fixedDeltaTime))
        {
            body.MovePosition(destination);
            nextPathIndex++;
            blockedDuration = 0f;
            movementWasRequested = false;

            if (nextCell == mazeGenerator.ExitPosition)
            {
                reachedExit = true;
            }
        }
        else if (blockedDuration >= blockedRepathDelay)
        {
            blockedDuration = 0f;
            movementWasRequested = false;
            RequestPathToExit();
        }
    }

    // Flips the sprite left/right to match the direction of travel.
    private void FaceMovement(Vector2 moveDelta)
    {
        if (!flipToFaceMovement || Mathf.Abs(moveDelta.x) < 0.01f)
        {
            return;
        }

        bool movingLeft = moveDelta.x < 0f;
        spriteRenderer.flipX = spriteFacesRight ? movingLeft : !movingLeft;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            warnedMissingPlayer = false;
        }
        else if (!warnedMissingPlayer)
        {
            Debug.LogWarning("No Player object is present. AI will navigate without distance protection until one is added.", this);
            warnedMissingPlayer = true;
        }
    }

    private bool TryChooseSpawnPosition(out Vector2 spawnPosition)
    {
        Vector3 desiredPosition = player != null
            ? player.position
            : mazeGenerator.GridToWorld(mazeGenerator.StartPosition);
        float minimumDistanceSquared = minimumSpawnDistanceFromPlayer * minimumSpawnDistanceFromPlayer;
        float closestDistanceSquared = float.PositiveInfinity;
        spawnPosition = Vector2.zero;

        List<Vector2Int> walkablePositions = mazeGenerator.GetWalkablePositions();
        foreach (Vector2Int cell in walkablePositions)
        {
            Vector3 world = mazeGenerator.GridToWorld(cell);
            Vector2 candidate = new Vector2(world.x, world.y);
            float distanceSquared = ((Vector2)desiredPosition - candidate).sqrMagnitude;

            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            if (player != null && distanceSquared < minimumDistanceSquared)
            {
                continue;
            }

            if (!IsSpawnPositionClear(candidate))
            {
                continue;
            }

            closestDistanceSquared = distanceSquared;
            spawnPosition = candidate;
        }

        return !float.IsPositiveInfinity(closestDistanceSquared);
    }

    private bool IsSpawnPositionClear(Vector2 position)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, bodyCollider.radius);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == bodyCollider || overlap.transform.IsChildOf(transform))
            {
                continue;
            }

            if (player != null && overlap.transform.IsChildOf(player))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void RequestPathToExit()
    {
        Vector2Int currentCell = WorldToGrid(body.position);
        if (!mazeGenerator.IsWalkable(currentCell))
        {
            currentCell = mazeGenerator.StartPosition;
        }

        path = pathfinder.FindPathToExit(currentCell);
        nextPathIndex = path.Count > 1 ? 1 : path.Count;
        retryPathTimer = 0f;

        if (path.Count == 1 && currentCell == mazeGenerator.ExitPosition)
        {
            reachedExit = true;
        }
    }

    private Vector2Int WorldToGrid(Vector2 worldPosition)
    {
        float cellSize = Mathf.Max(mazeGenerator.CellSize, 0.0001f);
        float gridX = (worldPosition.x - mazeGenerator.transform.position.x) / cellSize
            + (mazeGenerator.Width - 1) * 0.5f;
        float gridY = (worldPosition.y - mazeGenerator.transform.position.y) / cellSize
            + (mazeGenerator.Height - 1) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
    }

    private void CreatePlaceholderSprite()
    {
        placeholderTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        placeholderTexture.name = "AICharacter Placeholder Texture";
        placeholderTexture.SetPixel(0, 0, Color.white);
        placeholderTexture.Apply();
        placeholderSprite = Sprite.Create(
            placeholderTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        placeholderSprite.name = "AICharacter Placeholder Sprite";
        spriteRenderer.sprite = placeholderSprite;
    }
}
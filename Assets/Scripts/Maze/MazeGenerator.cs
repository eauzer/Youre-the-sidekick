using System;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField, Min(1)] private int rows = 10;
    [SerializeField, Min(1)] private int columns = 10;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int randomSeed;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField, Min(0)] private int smallCombatRoomCount = 4;
    [SerializeField, Min(0)] private int regularCombatRoomCount = 3;

    public bool[,] WalkableGrid { get; private set; } = new bool[0, 0];
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int ExitPosition { get; private set; }
    public int Width => WalkableGrid.GetLength(0);
    public int Height => WalkableGrid.GetLength(1);
    public float CellSize => cellSize;
    public int UsedSeed { get; private set; }
    private readonly List<RectInt> roomBounds = new List<RectInt>();

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateMaze();
        }
    }

    public void GenerateMaze()
    {
        rows = Mathf.Max(1, rows);
        columns = Mathf.Max(1, columns);

        int baseWidth = columns * 2 + 1;
        int baseHeight = rows * 2 + 1;
        bool[,] baseGrid = new bool[baseWidth, baseHeight];

        UsedSeed = useRandomSeed
            ? Guid.NewGuid().GetHashCode()
            : randomSeed;
        System.Random random = new System.Random(UsedSeed);

        bool[,] visited = new bool[columns, rows];
        List<Vector2Int> stack = new List<Vector2Int>(rows * columns);
        Vector2Int startingCell = Vector2Int.zero;
        Vector2Int baseStartPosition = LogicalToGridPosition(startingCell);

        visited[startingCell.x, startingCell.y] = true;
        baseGrid[baseStartPosition.x, baseStartPosition.y] = true;
        stack.Add(startingCell);

        while (stack.Count > 0)
        {
            Vector2Int currentCell = stack[stack.Count - 1];
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(currentCell, visited);

            if (unvisitedNeighbors.Count == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            Vector2Int nextCell = unvisitedNeighbors[random.Next(unvisitedNeighbors.Count)];
            Vector2Int currentGridPosition = LogicalToGridPosition(currentCell);
            Vector2Int nextGridPosition = LogicalToGridPosition(nextCell);
            Vector2Int wallPosition = (currentGridPosition + nextGridPosition) / 2;

            baseGrid[wallPosition.x, wallPosition.y] = true;
            baseGrid[nextGridPosition.x, nextGridPosition.y] = true;
            visited[nextCell.x, nextCell.y] = true;
            stack.Add(nextCell);
        }

        Vector2Int baseExitPosition = new Vector2Int(baseWidth - 2, baseHeight - 2);
        const int corridorScale = 3;
        WalkableGrid = ExpandWalkableGrid(baseGrid, corridorScale);
        StartPosition = ToExpandedGridPosition(baseStartPosition, corridorScale);
        ExitPosition = ToExpandedGridPosition(baseExitPosition, corridorScale);

        CarveRooms(corridorScale, random);

        if (!AreAllWalkableCellsConnected())
        {
            Debug.LogError("Generated maze contains disconnected walkable areas.", this);
        }

    }

    public bool IsWalkable(Vector2Int position)
    {
        return position.x >= 0
            && position.x < Width
            && position.y >= 0
            && position.y < Height
            && WalkableGrid[position.x, position.y];
    }

    public Vector3 GridToWorld(Vector2Int position)
    {
        return transform.position + new Vector3(
            (position.x - (Width - 1) * 0.5f) * cellSize,
            (position.y - (Height - 1) * 0.5f) * cellSize,
            0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - transform.position;
        int x = Mathf.RoundToInt(local.x / cellSize + (Width - 1) * 0.5f);
        int y = Mathf.RoundToInt(local.y / cellSize + (Height - 1) * 0.5f);
        return new Vector2Int(
            Mathf.Clamp(x, 0, Width - 1),
            Mathf.Clamp(y, 0, Height - 1));
    }

    public List<Vector2Int> GetWalkablePositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (WalkableGrid[x, y])
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
        }

        return positions;
    }

    private bool AreAllWalkableCellsConnected()
    {
        int walkableCount = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (WalkableGrid[x, y])
                {
                    walkableCount++;
                }
            }
        }

        bool[,] visited = new bool[Width, Height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(StartPosition);
        visited[StartPosition.x, StartPosition.y] = true;
        int reachedCount = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            reachedCount++;
            VisitWalkableNeighbor(current + Vector2Int.up, visited, queue);
            VisitWalkableNeighbor(current + Vector2Int.down, visited, queue);
            VisitWalkableNeighbor(current + Vector2Int.left, visited, queue);
            VisitWalkableNeighbor(current + Vector2Int.right, visited, queue);
        }

        return reachedCount == walkableCount && IsWalkable(ExitPosition);
    }

    private void VisitWalkableNeighbor(
        Vector2Int position,
        bool[,] visited,
        Queue<Vector2Int> queue)
    {
        if (IsWalkable(position) && !visited[position.x, position.y])
        {
            visited[position.x, position.y] = true;
            queue.Enqueue(position);
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell, bool[,] visited)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(4);
        AddNeighborIfUnvisited(cell + Vector2Int.up, visited, neighbors);
        AddNeighborIfUnvisited(cell + Vector2Int.down, visited, neighbors);
        AddNeighborIfUnvisited(cell + Vector2Int.left, visited, neighbors);
        AddNeighborIfUnvisited(cell + Vector2Int.right, visited, neighbors);
        return neighbors;
    }

    private void AddNeighborIfUnvisited(
        Vector2Int neighbor,
        bool[,] visited,
        List<Vector2Int> neighbors)
    {
        if (neighbor.x >= 0
            && neighbor.x < columns
            && neighbor.y >= 0
            && neighbor.y < rows
            && !visited[neighbor.x, neighbor.y])
        {
            neighbors.Add(neighbor);
        }
    }

    private static Vector2Int LogicalToGridPosition(Vector2Int logicalPosition)
    {
        return new Vector2Int(logicalPosition.x * 2 + 1, logicalPosition.y * 2 + 1);
    }

    private static bool[,] ExpandWalkableGrid(bool[,] source, int scale)
    {
        int expandedWidth = source.GetLength(0) * scale;
        int expandedHeight = source.GetLength(1) * scale;
        bool[,] expanded = new bool[expandedWidth, expandedHeight];

        for (int x = 0; x < source.GetLength(0); x++)
        {
            for (int y = 0; y < source.GetLength(1); y++)
            {
                if (!source[x, y])
                {
                    continue;
                }

                for (int offsetX = 0; offsetX < scale; offsetX++)
                {
                    for (int offsetY = 0; offsetY < scale; offsetY++)
                    {
                        expanded[x * scale + offsetX, y * scale + offsetY] = true;
                    }
                }
            }
        }

        return expanded;
    }

    private void CarveRooms(int corridorScale, System.Random random)
    {
        bool[,] reservedRoomCells = new bool[Width, Height];
        roomBounds.Clear();

        // Anchor the final combat room on the exit so it is reachable and contains the goal.
        TryCarveRoom(ExitPosition, 10, reservedRoomCells);

        List<Vector2Int> candidates = GetLogicalCellCandidates();
        Shuffle(candidates, random);
        PlaceRooms(regularCombatRoomCount, 8, candidates, corridorScale, reservedRoomCells);

        Shuffle(candidates, random);
        PlaceRooms(smallCombatRoomCount, 5, candidates, corridorScale, reservedRoomCells);
    }

    private List<Vector2Int> GetLogicalCellCandidates()
    {
        List<Vector2Int> candidates = new List<Vector2Int>(rows * columns);
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                candidates.Add(new Vector2Int(x, y));
            }
        }

        return candidates;
    }

    private void PlaceRooms(
        int roomCount,
        int roomSize,
        List<Vector2Int> candidates,
        int corridorScale,
        bool[,] reservedRoomCells)
    {
        if (roomCount <= 0)
        {
            return;
        }

        int placedCount = 0;
        foreach (Vector2Int logicalCell in candidates)
        {
            Vector2Int basePosition = LogicalToGridPosition(logicalCell);
            Vector2Int center = ToExpandedGridPosition(basePosition, corridorScale);
            if (TryCarveRoom(center, roomSize, reservedRoomCells))
            {
                placedCount++;
                if (placedCount == roomCount)
                {
                    return;
                }
            }
        }

        if (placedCount < roomCount)
        {
            Debug.LogWarning(
                $"Placed {placedCount} of {roomCount} requested {roomSize}x{roomSize} combat rooms.",
                this);
        }
    }

    private bool TryCarveRoom(Vector2Int center, int roomSize, bool[,] reservedRoomCells)
    {
        int roomWidth = Mathf.Min(roomSize, Width - 2);
        int roomHeight = Mathf.Min(roomSize, Height - 2);
        int startX = Mathf.Clamp(center.x - roomWidth / 2, 1, Width - roomWidth - 1);
        int startY = Mathf.Clamp(center.y - roomHeight / 2, 1, Height - roomHeight - 1);

        // Rooms are carved only around connected DFS cells, and reserved footprints cannot overlap.
        for (int x = startX; x < startX + roomWidth; x++)
        {
            for (int y = startY; y < startY + roomHeight; y++)
            {
                if (reservedRoomCells[x, y])
                {
                    return false;
                }
            }
        }

        for (int x = startX; x < startX + roomWidth; x++)
        {
            for (int y = startY; y < startY + roomHeight; y++)
            {
                WalkableGrid[x, y] = true;
                reservedRoomCells[x, y] = true;
            }
        }

        roomBounds.Add(new RectInt(startX, startY, roomWidth, roomHeight));
        return true;
    }

    private static void Shuffle<T>(IList<T> items, System.Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            T item = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = item;
        }
    }

    private static Vector2Int ToExpandedGridPosition(Vector2Int position, int scale)
    {
        return position * scale + new Vector2Int(scale / 2, scale / 2);
    }
}
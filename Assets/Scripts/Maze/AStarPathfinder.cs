using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private MazeGenerator mazeGenerator;

    /// <summary>
    /// Returns the shortest four-directional route, or an empty list if either endpoint is invalid
    /// or no route exists.
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> emptyPath = new List<Vector2Int>();
        if (!TryGetMazeGenerator())
        {
            return emptyPath;
        }

        if (mazeGenerator.Width == 0
            || mazeGenerator.Height == 0
            || !mazeGenerator.IsWalkable(start)
            || !mazeGenerator.IsWalkable(target))
        {
            return emptyPath;
        }

        if (start == target)
        {
            return new List<Vector2Int> { start };
        }

        MinHeap openHeap = new MinHeap();
        HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, int> gCosts = new Dictionary<Vector2Int, int>();
        Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
        long insertionOrder = 0;

        gCosts[start] = 0;
        int startHeuristic = ManhattanDistance(start, target);
        openHeap.Push(new HeapEntry(start, 0, startHeuristic, insertionOrder++));
        openSet.Add(start);

        while (openHeap.Count > 0)
        {
            HeapEntry currentEntry = openHeap.Pop();
            Vector2Int current = currentEntry.Position;

            if (closedSet.Contains(current)
                || !gCosts.TryGetValue(current, out int bestKnownCost)
                || currentEntry.GCost != bestKnownCost)
            {
                continue;
            }

            openSet.Remove(current);
            if (current == target)
            {
                return ReconstructPath(start, target, parents);
            }

            closedSet.Add(current);
            VisitNeighbor(current + Vector2Int.up, current, target, openSet, closedSet, gCosts, parents, openHeap, ref insertionOrder);
            VisitNeighbor(current + Vector2Int.down, current, target, openSet, closedSet, gCosts, parents, openHeap, ref insertionOrder);
            VisitNeighbor(current + Vector2Int.left, current, target, openSet, closedSet, gCosts, parents, openHeap, ref insertionOrder);
            VisitNeighbor(current + Vector2Int.right, current, target, openSet, closedSet, gCosts, parents, openHeap, ref insertionOrder);
        }

        return emptyPath;
    }

    public List<Vector2Int> FindPathToExit(Vector2Int start)
    {
        if (!TryGetMazeGenerator())
        {
            return new List<Vector2Int>();
        }

        return FindPath(start, mazeGenerator.ExitPosition);
    }

    private bool TryGetMazeGenerator()
    {
        if (mazeGenerator == null)
        {
            mazeGenerator = GetComponent<MazeGenerator>();
        }

        return mazeGenerator != null;
    }

    private void VisitNeighbor(
        Vector2Int neighbor,
        Vector2Int current,
        Vector2Int target,
        HashSet<Vector2Int> openSet,
        HashSet<Vector2Int> closedSet,
        Dictionary<Vector2Int, int> gCosts,
        Dictionary<Vector2Int, Vector2Int> parents,
        MinHeap openHeap,
        ref long insertionOrder)
    {
        if (!mazeGenerator.IsWalkable(neighbor) || closedSet.Contains(neighbor))
        {
            return;
        }

        int tentativeGCost = gCosts[current] + 1;
        if (gCosts.TryGetValue(neighbor, out int knownGCost) && tentativeGCost >= knownGCost)
        {
            return;
        }

        parents[neighbor] = current;
        gCosts[neighbor] = tentativeGCost;
        int heuristic = ManhattanDistance(neighbor, target);
        openHeap.Push(new HeapEntry(neighbor, tentativeGCost, heuristic, insertionOrder++));
        openSet.Add(neighbor);
    }

    private static List<Vector2Int> ReconstructPath(
        Vector2Int start,
        Vector2Int target,
        Dictionary<Vector2Int, Vector2Int> parents)
    {
        List<Vector2Int> path = new List<Vector2Int> { target };
        Vector2Int current = target;

        while (current != start)
        {
            if (!parents.TryGetValue(current, out current))
            {
                return new List<Vector2Int>();
            }

            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static int ManhattanDistance(Vector2Int first, Vector2Int second)
    {
        return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y);
    }

    private readonly struct HeapEntry
    {
        public readonly Vector2Int Position;
        public readonly int GCost;
        public readonly int HCost;
        public readonly long InsertionOrder;
        public int FCost => GCost + HCost;

        public HeapEntry(Vector2Int position, int gCost, int hCost, long insertionOrder)
        {
            Position = position;
            GCost = gCost;
            HCost = hCost;
            InsertionOrder = insertionOrder;
        }
    }

    private sealed class MinHeap
    {
        private readonly List<HeapEntry> entries = new List<HeapEntry>();

        public int Count => entries.Count;

        public void Push(HeapEntry entry)
        {
            entries.Add(entry);
            int index = entries.Count - 1;

            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (Compare(entries[parentIndex], entry) <= 0)
                {
                    break;
                }

                entries[index] = entries[parentIndex];
                index = parentIndex;
            }

            entries[index] = entry;
        }

        public HeapEntry Pop()
        {
            HeapEntry minimum = entries[0];
            HeapEntry last = entries[entries.Count - 1];
            entries.RemoveAt(entries.Count - 1);

            if (entries.Count == 0)
            {
                return minimum;
            }

            int index = 0;
            while (true)
            {
                int leftIndex = index * 2 + 1;
                if (leftIndex >= entries.Count)
                {
                    break;
                }

                int rightIndex = leftIndex + 1;
                int smallestChildIndex = rightIndex < entries.Count
                    && Compare(entries[rightIndex], entries[leftIndex]) < 0
                    ? rightIndex
                    : leftIndex;

                if (Compare(last, entries[smallestChildIndex]) <= 0)
                {
                    break;
                }

                entries[index] = entries[smallestChildIndex];
                index = smallestChildIndex;
            }

            entries[index] = last;
            return minimum;
        }

        private static int Compare(HeapEntry first, HeapEntry second)
        {
            int fCostComparison = first.FCost.CompareTo(second.FCost);
            if (fCostComparison != 0)
            {
                return fCostComparison;
            }

            int hCostComparison = first.HCost.CompareTo(second.HCost);
            return hCostComparison != 0
                ? hCostComparison
                : first.InsertionOrder.CompareTo(second.InsertionOrder);
        }
    }
}

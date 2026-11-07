using System.Collections.Generic;
using UnityEngine;

namespace Beginner2D
{
    public class MazePathfinder : MonoBehaviour
    {
        public static MazePathfinder Instance { get; private set; }

        public MazeGenerator mazeGenerator;

        static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            if (mazeGenerator == null) return null;

            Vector2Int startCell = mazeGenerator.WorldToGrid(start);
            Vector2Int endCell = mazeGenerator.WorldToGrid(end);

            if (!mazeGenerator.IsWalkable(startCell) || !mazeGenerator.IsWalkable(endCell))
                return null;

            int width = mazeGenerator.Width;
            int height = mazeGenerator.Height;

            int[,] gScore = new int[width, height];
            int[,] fScore = new int[width, height];
            Vector2Int[,] cameFrom = new Vector2Int[width, height];
            bool[,] closed = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gScore[x, y] = int.MaxValue;
                    fScore[x, y] = int.MaxValue;
                    cameFrom[x, y] = new Vector2Int(-1, -1);
                }
            }

            gScore[startCell.x, startCell.y] = 0;
            fScore[startCell.x, startCell.y] = Heuristic(startCell, endCell);

            SortedSet<(int f, int g, Vector2Int pos)> open = new SortedSet<(int, int, Vector2Int)>(
                Comparer<(int f, int g, Vector2Int pos)>.Create((a, b) =>
                {
                    int cmp = a.f.CompareTo(b.f);
                    if (cmp != 0) return cmp;
                    cmp = a.g.CompareTo(b.g);
                    if (cmp != 0) return cmp;
                    cmp = a.pos.x.CompareTo(b.pos.x);
                    if (cmp != 0) return cmp;
                    return a.pos.y.CompareTo(b.pos.y);
                }));

            open.Add((fScore[startCell.x, startCell.y], 0, startCell));

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);
                Vector2Int currentPos = current.pos;

                if (currentPos == endCell)
                    return ReconstructPath(cameFrom, endCell);

                if (closed[currentPos.x, currentPos.y]) continue;
                closed[currentPos.x, currentPos.y] = true;

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int neighbor = currentPos + Directions[i];

                    if (!mazeGenerator.IsWalkable(neighbor)) continue;
                    if (closed[neighbor.x, neighbor.y]) continue;

                    int tentativeG = gScore[currentPos.x, currentPos.y] + 1;
                    if (tentativeG >= gScore[neighbor.x, neighbor.y]) continue;

                    cameFrom[neighbor.x, neighbor.y] = currentPos;
                    gScore[neighbor.x, neighbor.y] = tentativeG;
                    int f = tentativeG + Heuristic(neighbor, endCell);
                    fScore[neighbor.x, neighbor.y] = f;
                    open.Add((f, tentativeG, neighbor));
                }
            }

            return null;
        }

        List<Vector3> ReconstructPath(Vector2Int[,] cameFrom, Vector2Int current)
        {
            List<Vector3> path = new List<Vector3> { mazeGenerator.GridToWorld(current) };

            while (cameFrom[current.x, current.y].x != -1)
            {
                current = cameFrom[current.x, current.y];
                path.Add(mazeGenerator.GridToWorld(current));
            }

            path.Reverse();
            return path;
        }

        static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}

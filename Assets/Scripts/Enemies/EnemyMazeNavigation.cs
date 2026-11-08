using System.Collections.Generic;
using UnityEngine;

namespace Beginner2D
{
	[RequireComponent(typeof(Rigidbody2D))]
	public class EnemyMazeNavigation : MonoBehaviour
	{
		[SerializeField] private global::MazeGenerator mazeGenerator;
		[SerializeField] private global::AStarPathfinder pathfinder;
		[SerializeField, Min(0.05f)] private float pathRecalculateInterval = 0.4f;
		[SerializeField, Min(0.01f)] private float waypointReachDistance = 0.04f;
		[SerializeField, Min(0.05f)] private float blockedRepathDelay = 0.4f;

		private Rigidbody2D body;
		private EnemyHealth enemyHealth;
		private List<Vector2Int> path;
		private int waypointIndex;
		private Vector2Int plannedTarget;
		private bool hasPlannedTarget;
		private bool pathSearchFailed;
		private float repathTimer;
		private float blockedTimer;
		private Vector2 previousPosition;
		private bool movementWasRequested;

		public global::MazeGenerator MazeGenerator
		{
			get
			{
				TryResolveReferences();
				return mazeGenerator;
			}
		}
		public bool HasReachedDestination => path != null && waypointIndex >= path.Count;

		private void Awake()
		{
			body = GetComponent<Rigidbody2D>();
			enemyHealth = GetComponent<EnemyHealth>();
			body.gravityScale = 0f;
			body.freezeRotation = true;
			body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
			previousPosition = body.position;

			int enemyLayer = Helpers.EnemyLayer;
			if (enemyLayer >= 0 && enemyLayer < 32)
			{
				body.excludeLayers |= 1 << enemyLayer;
			}
		}

		public bool TryMoveTowards(Vector2 worldTarget, float movementSpeed)
		{
			if (!TryResolveReferences())
			{
				StopMovement();
				return false;
			}

			Vector2Int targetCell = mazeGenerator.WorldToGrid(worldTarget);
			if (!mazeGenerator.IsWalkable(targetCell))
			{
				if (!TryFindNearbyWalkableTarget(targetCell, out Vector2Int resolvedTarget))
				{
					StopMovement();
					return false;
				}

				targetCell = resolvedTarget;
			}

			return TryMoveTowardsCell(targetCell, movementSpeed);
		}

		public bool CanReach(Vector2 worldTarget)
		{
			if (!TryResolveReferences() || (enemyHealth != null && enemyHealth.IsDead))
			{
				return false;
			}

			Vector2Int startCell = mazeGenerator.WorldToGrid(body.position);
			if (!mazeGenerator.IsWalkable(startCell))
			{
				return false;
			}

			Vector2Int targetCell = mazeGenerator.WorldToGrid(worldTarget);
			if (!mazeGenerator.IsWalkable(targetCell)
				&& !TryFindNearbyWalkableTarget(targetCell, out targetCell))
			{
				return false;
			}

			List<Vector2Int> candidatePath = pathfinder.FindPath(startCell, targetCell);
			return candidatePath != null && candidatePath.Count > 0;
		}

		public void SetMazeReferences(global::MazeGenerator generator, global::AStarPathfinder mazePathfinder)
		{
			mazeGenerator = generator;
			pathfinder = mazePathfinder;
		}

		public bool TryMoveTowardsCell(Vector2Int targetCell, float movementSpeed)
		{
			if (!TryResolveReferences() || !mazeGenerator.IsWalkable(targetCell)
				|| (enemyHealth != null && enemyHealth.IsDead))
			{
				StopMovement();
				return false;
			}

			UpdateBlockedTimer();
			Vector2Int currentCell = mazeGenerator.WorldToGrid(body.position);
			if (!mazeGenerator.IsWalkable(currentCell))
			{
				StopMovement();
				path = null;
				return false;
			}

			repathTimer -= Time.fixedDeltaTime;
			bool nextCellInvalid = path != null
				&& waypointIndex < path.Count
				&& !mazeGenerator.IsWalkable(path[waypointIndex]);
			bool targetChanged = !hasPlannedTarget || plannedTarget != targetCell;

			if (pathSearchFailed && !targetChanged && repathTimer > 0f)
			{
				StopMovement();
				return false;
			}

			if (targetChanged || nextCellInvalid || repathTimer <= 0f
				|| blockedTimer >= blockedRepathDelay || (path == null && !pathSearchFailed))
			{
				if (!RecalculatePath(currentCell, targetCell))
				{
					StopMovement();
					return false;
				}
			}

			if (path == null || waypointIndex >= path.Count)
			{
				body.linearVelocity = Vector2.zero;
				movementWasRequested = false;
				return path != null;
			}

			Vector2Int waypointCell = path[waypointIndex];
			if (!mazeGenerator.IsWalkable(waypointCell)
				|| Mathf.Abs(waypointCell.x - currentCell.x) + Mathf.Abs(waypointCell.y - currentCell.y) > 1)
			{
				StopMovement();
				path = null;
				return false;
			}

			Vector3 waypointWorld = mazeGenerator.GridToWorld(waypointCell);
			Vector2 destination = new Vector2(waypointWorld.x, waypointWorld.y);
			Vector2 currentPosition = body.position;
			float distance = Vector2.Distance(currentPosition, destination);
			if (distance <= waypointReachDistance)
			{
				body.MovePosition(destination);
				waypointIndex++;
				blockedTimer = 0f;
				movementWasRequested = false;
				return true;
			}

			float cellSize = Mathf.Max(mazeGenerator.CellSize, 0.0001f);
			float maxStep = Mathf.Min(Mathf.Max(0f, movementSpeed) * Time.fixedDeltaTime, cellSize * 0.25f);
			Vector2 nextPosition = Vector2.MoveTowards(currentPosition, destination, maxStep);
			Vector2Int candidateCell = mazeGenerator.WorldToGrid(nextPosition);
			if (!mazeGenerator.IsWalkable(candidateCell))
			{
				StopMovement();
				path = null;
				return false;
			}

			body.MovePosition(nextPosition);
			movementWasRequested = true;
			return true;
		}

		private bool RecalculatePath(Vector2Int startCell, Vector2Int targetCell)
		{
			path = pathfinder.FindPath(startCell, targetCell);
			plannedTarget = targetCell;
			hasPlannedTarget = true;
			pathSearchFailed = path == null || path.Count == 0;
			waypointIndex = path != null && path.Count > 1 ? 1 : path != null ? path.Count : 0;
			repathTimer = pathRecalculateInterval;
			blockedTimer = 0f;
			movementWasRequested = false;
			return !pathSearchFailed;
		}

		private bool TryFindNearbyWalkableTarget(Vector2Int center, out Vector2Int target)
		{
			for (int radius = 1; radius <= 3; radius++)
			{
				float nearestDistance = float.PositiveInfinity;
				target = center;
				bool found = false;

				for (int x = -radius; x <= radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
						{
							continue;
						}

						Vector2Int candidate = center + new Vector2Int(x, y);
						if (!mazeGenerator.IsWalkable(candidate))
						{
							continue;
						}

						float distance = (candidate - center).sqrMagnitude;
						if (distance < nearestDistance)
						{
							nearestDistance = distance;
							target = candidate;
							found = true;
						}
					}
				}

				if (found)
				{
					return true;
				}
			}

			target = center;
			return false;
		}

		private bool TryResolveReferences()
		{
			if (mazeGenerator == null)
			{
				mazeGenerator = FindAnyObjectByType<global::MazeGenerator>();
			}

			if (pathfinder == null)
			{
				pathfinder = FindAnyObjectByType<global::AStarPathfinder>();
			}

			return mazeGenerator != null && pathfinder != null
				&& mazeGenerator.Width > 0 && mazeGenerator.Height > 0;
		}

		private void UpdateBlockedTimer()
		{
			if (movementWasRequested)
			{
				blockedTimer = Vector2.Distance(body.position, previousPosition) <= 0.001f
					? blockedTimer + Time.fixedDeltaTime
					: 0f;
			}

			previousPosition = body.position;
			movementWasRequested = false;
		}

		public void StopMovement()
		{
			if (body != null)
			{
				body.linearVelocity = Vector2.zero;
			}

			movementWasRequested = false;
		}
	}
}

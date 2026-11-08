using System; //---- MTTT counter
using UnityEngine;

namespace Beginner2D
{
	/// <summary>
	/// Enemy behaviour. Patrols on the maze grid, prioritizes AICharacter, and can attack the Player.
	/// </summary>
	[RequireComponent(typeof(Rigidbody2D), typeof(EnemyMazeNavigation), typeof(EnemyHealth))]
	public class Enemy : MonoBehaviour
	{
		// ====== ENEMY MOVEMENT ========
		public float speed;

		public bool vertical;

		public float changeTime = 3.0f;
		Rigidbody2D rigidbody2d;
		float timer;
		int direction = 1;
		bool broken = true;

		[Header("Targeting / Combat")]
		[SerializeField, Min(0.1f)] private float detectionRange = 5f;
		[SerializeField, Min(0.1f)] private float movementSpeed = 2.5f;
		[SerializeField, Min(0.05f)] private float attackRange = 0.8f;
		[SerializeField, Min(0.05f)] private float attackCooldown = 1f;
		[SerializeField, Min(1)] private int aiAttackDamage = 1;
		[SerializeField, Min(0f)] private float playerAttackDamage = 15f;
		[SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.25f;
		Transform target;
		EnemyMazeNavigation navigation;
		EnemyHealth enemyHealth;
		global::AIHealth aiTargetHealth;
		global::PlayerHealth playerTargetHealth;
		float nextAttackTime;
		float targetRefreshTimer;
		Vector2Int patrolTarget;
		bool hasPatrolTarget;

		// ===== ANIMATION ========
		Animator animator;

		// ====== AUDIO ========
		AudioSource audioSource;
		public AudioClip fixedSound;

		// ====== BREAKING AND FIXING =======
		public bool isBroken { get { return broken; }}
		public event Action OnFixed;

		private void Awake()
		{
			//DEMO purpose only. In the tutorial, this is set in the inspector on the prefab, but to keep the layer
			//settings empty, we have to do it dynamically in the demo
			Helpers.RecursiveLayerSet(transform, Helpers.EnemyLayer);
			navigation = GetComponent<EnemyMazeNavigation>();
			enemyHealth = GetComponent<EnemyHealth>();
			Rigidbody2D body = GetComponent<Rigidbody2D>();
			body.gravityScale = 0f;
			body.freezeRotation = true;
			body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		}

		void Start ()
		{
			rigidbody2d = GetComponent<Rigidbody2D>();
			animator = GetComponent<Animator>();
			audioSource = GetComponent<AudioSource>();
			timer = changeTime;
			targetRefreshTimer = 0f;
		}

		void Update()
		{
			if (!broken || (enemyHealth != null && enemyHealth.IsDead))
			{
				return;
			}

			timer -= Time.deltaTime;

			if (timer < 0)
			{
				direction = -direction;
				timer = changeTime;
				hasPatrolTarget = false;
			}

			targetRefreshTimer -= Time.deltaTime;
			if (targetRefreshTimer <= 0f)
			{
				RefreshTarget();
				targetRefreshTimer = Mathf.Max(0.05f, targetRefreshInterval);
			}
		}

		void RefreshTarget()
		{
			if (aiTargetHealth == null)
			{
				aiTargetHealth = FindAnyObjectByType<global::AIHealth>();
			}

			GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
			playerTargetHealth = playerObject != null
				? playerObject.GetComponentInParent<global::PlayerHealth>()
				: null;

			bool aiAvailable = IsAvailableTarget(aiTargetHealth, detectionRange);
			bool playerAvailable = IsAvailableTarget(playerTargetHealth, detectionRange);
			target = aiAvailable
				? aiTargetHealth.transform
				: playerAvailable ? playerTargetHealth.transform : null;
		}

		bool IsAvailableTarget(global::AIHealth health, float range)
		{
			return health != null && health.isActiveAndEnabled && health.gameObject.activeInHierarchy && !health.IsDead
				&& Vector2.Distance(rigidbody2d.position, health.transform.position) <= range
				&& navigation.CanReach(health.transform.position);
		}

		bool IsAvailableTarget(global::PlayerHealth health, float range)
		{
			return health != null && health.isActiveAndEnabled && health.gameObject.activeInHierarchy && !health.IsDead
				&& Vector2.Distance(rigidbody2d.position, health.transform.position) <= range
				&& navigation.CanReach(health.transform.position);
		}

		void FixedUpdate()
		{
			if (!broken || (enemyHealth != null && enemyHealth.IsDead))
			{
				return;
			}

			if (TryAttackInRange())
			{
				return;
			}

			if (target != null)
			{
				Vector2 toTarget = (Vector2)target.position - rigidbody2d.position;
				if (!navigation.TryMoveTowards(target.position, movementSpeed))
				{
					targetRefreshTimer = 0f;
					RefreshTarget();
					if (target != null)
					{
						toTarget = (Vector2)target.position - rigidbody2d.position;
						navigation.TryMoveTowards(target.position, movementSpeed);
					}
				}

				SetAnimationDirection(toTarget);
			}
			else
			{
				Patrol();
			}
		}

		void Patrol()
		{
			global::MazeGenerator maze = navigation.MazeGenerator;
			if (maze == null)
			{
				navigation.TryMoveTowards(transform.position, speed);
				return;
			}

			if (!hasPatrolTarget || navigation.HasReachedDestination)
			{
				hasPatrolTarget = TryChoosePatrolTarget(maze);
			}

			if (hasPatrolTarget)
			{
				Vector2Int currentCell = maze.WorldToGrid(rigidbody2d.position);
				SetAnimationDirection(patrolTarget - currentCell);
				navigation.TryMoveTowardsCell(patrolTarget, speed);
			}
			else
			{
				navigation.TryMoveTowards(transform.position, speed);
			}
		}

		bool TryChoosePatrolTarget(global::MazeGenerator maze)
		{
			Vector2Int currentCell = maze.WorldToGrid(rigidbody2d.position);
			Vector2Int axis = vertical ? Vector2Int.up : Vector2Int.right;
			for (int attempt = 0; attempt < 2; attempt++)
			{
				for (int distance = 4; distance >= 1; distance--)
				{
					Vector2Int candidate = currentCell + axis * (direction * distance);
					if (maze.IsWalkable(candidate))
					{
						patrolTarget = candidate;
						return true;
					}
				}

				direction = -direction;
			}

			return false;
		}

		void SetAnimationDirection(Vector2 movement)
		{
			if (animator == null)
			{
				return;
			}

			if (movement.sqrMagnitude > 0.001f)
			{
				movement.Normalize();
			}

			animator.SetFloat("Move X", movement.x);
			animator.SetFloat("Move Y", movement.y);
		}

		bool TryAttackInRange()
		{
			if (IsAvailableTarget(aiTargetHealth, attackRange))
			{
				navigation.StopMovement();
				SetAnimationDirection(Vector2.zero);
				if (Time.unscaledTime >= nextAttackTime)
				{
					aiTargetHealth.TakeDamage(aiAttackDamage);
					nextAttackTime = Time.unscaledTime
						+ Mathf.Max(attackCooldown, aiTargetHealth.DamageImmunityDuration);
				}

				return true;
			}

			if (IsAvailableTarget(playerTargetHealth, attackRange))
			{
				navigation.StopMovement();
				SetAnimationDirection(Vector2.zero);
				if (Time.unscaledTime >= nextAttackTime)
				{
					playerTargetHealth.TakeDamage(playerAttackDamage);
					nextAttackTime = Time.unscaledTime
						+ Mathf.Max(attackCooldown, playerTargetHealth.DamageImmunityDuration);
				}

				return true;
			}

			return false;
		}

		public void Fix()
		{
			broken = false;
			target = null;
			rigidbody2d.simulated = false;
			animator.SetTrigger("Fixed");

			// Audio
			audioSource.Stop();
			audioSource.PlayOneShot(fixedSound);
			OnFixed?.Invoke();
		}

		// Shows the detection radius in the Scene view when the enemy is selected.
		void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, detectionRange);
		}
	}
}
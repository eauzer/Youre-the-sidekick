using System; //---- MTTT counter
using UnityEngine;

namespace Beginner2D
{
	/// <summary>
	/// Enemy behaviour. Patrols back & forth until the player (on the Player layer) enters its detection radius,
	/// then chases the player. Once fixed, it just idles.
	/// </summary>
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

		// ====== DETECTION & CHASE ========
		[Header("Detection / Chase")]
		public float detectionRadius = 5f;
		public float chaseSpeed = 2.5f;
		Transform target;

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
		}

		void Start ()
		{
			rigidbody2d = GetComponent<Rigidbody2D>();
			animator = GetComponent<Animator>();
			audioSource = GetComponent<AudioSource>();
			timer = changeTime;
		}

		void Update()
		{
			timer -= Time.deltaTime;

			if (timer < 0)
			{
				direction = -direction;
				timer = changeTime;
			}

			if (broken)
			{
				DetectPlayer();
			}
		}

		// Looks for anything on the Player layer inside the detection radius.
		void DetectPlayer()
		{
			Collider2D hit = Physics2D.OverlapCircle(
				transform.position,
				detectionRadius,
				1 << Helpers.PlayerLayer);

			target = hit != null ? hit.transform : null;
		}

		void FixedUpdate()
		{
			if(!broken)
			{
				return;
			}

			Vector2 position = rigidbody2d.position;

			if (target != null)
			{
				// Chase the player
				Vector2 toTarget = ((Vector2)target.position - position).normalized;
				position += toTarget * chaseSpeed * Time.deltaTime;
				animator.SetFloat("Move X", toTarget.x);
				animator.SetFloat("Move Y", toTarget.y);
			}
			else if (vertical)
			{
				position.y = position.y + speed * direction * Time.deltaTime;
				animator.SetFloat("Move X", 0);
				animator.SetFloat("Move Y", direction);
			}
			else
			{
				position.x = position.x + speed * direction * Time.deltaTime;
				animator.SetFloat("Move X", direction);
				animator.SetFloat("Move Y", 0);
			}

			rigidbody2d.MovePosition(position);
		}

		void OnCollisionEnter2D(Collision2D collision)
		{
			if (!broken || collision.gameObject.layer != Helpers.PlayerLayer)
			{
				return;
			}

			// TODO: damage the player here once the health system exists,
			// e.g. collision.gameObject.GetComponentInParent<...>().TakeDamage(1);
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
			Gizmos.DrawWireSphere(transform.position, detectionRadius);
		}
	}
}
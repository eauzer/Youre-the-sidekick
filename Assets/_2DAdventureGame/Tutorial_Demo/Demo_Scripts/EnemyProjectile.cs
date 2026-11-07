using UnityEngine;

namespace Beginner2D
{
    public class EnemyProjectile : MonoBehaviour
    {
        public int damage = 1;
        public float lifetime = 5f;

        void Start()
        {
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.ChangeHealth(-damage);
                Destroy(gameObject);
                return;
            }

            if (!other.isTrigger)
                Destroy(gameObject);
        }
    }
}

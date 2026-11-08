using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private AIHealth aiHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthUI healthUI;

    private bool gameOver;

    public bool IsGameOver => gameOver;

    private void Start()
    {
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
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

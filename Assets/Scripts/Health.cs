using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Events")]
    // These allow you to drag and drop visual effects, sounds, or scripts in the Inspector!
    public UnityEvent onTakeDamage;
    public UnityEvent onDie;
    public GameObject deathScreen; // Reference to the death screen UI

    private bool isDead = false;
    public float GetHealthPercentage() { return currentHealth / maxHealth; }
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        // Don't take damage if already dead (prevents double-triggering death logic)
        if (isDead) return;

        currentHealth -= amount;
        onTakeDamage?.Invoke(); // Shouts: "I took damage!"

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        // Adds health but ensures it never goes above the max limit
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        if(gameObject.CompareTag("Player"))
        {
            Debug.Log("Player has died!");
            gameObject.SetActive(false); // Deactivates the player object
            if (deathScreen != null)
            {
                deathScreen.SetActive(true); // Activates the death screen UI
                Cursor.lockState = CursorLockMode.None;
                 Cursor.visible = true;
            }
            else
            {
                Debug.LogWarning("Death screen UI is not assigned in the Inspector!");
            }
            // You can add additional logic here, like triggering a game over screen or restarting the level.
        }
        onDie?.Invoke(); // Shouts: "I am dead!"
    }
}
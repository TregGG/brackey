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
        onDie?.Invoke(); // Shouts: "I am dead!"
    }
}
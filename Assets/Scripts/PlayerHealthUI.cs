using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player Reference")]
    public Health playerHealth;

    [Header("UI Elements")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    void Start()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
        }

        if (playerHealth != null)
        {
            // Wire up the C# events!
            playerHealth.onTakeDamage.AddListener(UpdateUI);

            // NOTE: If you ever add an 'onHeal' event to Health.cs later, 
            // you can just add playerHealth.onHeal.AddListener(UpdateUI); right here!

            // Force the UI to update on the very first frame
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // 1. Update the visual bar
        if (healthSlider != null)
        {
            healthSlider.value = 1-playerHealth.GetHealthPercentage();
        }

        // 2. Update the exact numbers (Mathf.CeilToInt rounds up so you don't see decimals)
        if (healthText != null)
        {
            int current = Mathf.CeilToInt(playerHealth.GetCurrentHealth());
            int max = Mathf.CeilToInt(playerHealth.GetMaxHealth());
            healthText.text = $"HP: {current} / {max}";
        }
    }
}
using UnityEngine;
using UnityEngine.UI; 

public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public GameObject healthBarContainer; // The parent object to hide when the boss dies

    [Header("Boss Reference")]
    public Health bossHealth;

    void Start()
    {
        // Safety check to ensure the slider goes from 0 to 1 (percentages)
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
        }

        // If a boss is already assigned in the Inspector, wire it up immediately
        if (bossHealth != null)
        {
            InitializeHealthBar(bossHealth);
        }
        else
        {
            // Hide the UI completely if there is no boss active
            if (healthBarContainer != null) healthBarContainer.SetActive(false);
        }
    }

    // Call this if you spawn the boss dynamically later!
    public void InitializeHealthBar(Health activeBoss)
    {
        bossHealth = activeBoss;

        // Wire up the C# events
        bossHealth.onTakeDamage.AddListener(UpdateUI);
        bossHealth.onDie.AddListener(HideUI);

        // Turn the UI on and set the initial fill to 100%
        if (healthBarContainer != null) healthBarContainer.SetActive(true);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthSlider != null && bossHealth != null)
        {
            // We use the method you just added to the Health script!
            healthSlider.value = bossHealth.GetHealthPercentage();
        }
    }

    private void HideUI()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false);
        }
    }
}
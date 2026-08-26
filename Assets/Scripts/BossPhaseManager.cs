using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class BossPhaseManager : MonoBehaviour
{
    private Health myHealth;
    private EnemyWeaponController weaponController;
    private Transform playerTransform;
    private int currentPhase = 1;

    private bool isFightActive = false;

    [Header("Telegraphing & Attack")]
    public SpriteRenderer bossSprite;
    public Color normalColor = Color.white;
    public Color telegraphColor = Color.yellow;

    public float attackCooldown = 2f;
    public float telegraphDuration = 0.5f;

    void Awake()
    {
        myHealth = GetComponent<Health>();
        weaponController = GetComponent<EnemyWeaponController>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        myHealth.onTakeDamage.AddListener(CheckForPhaseChange);
        myHealth.onDie.AddListener(Die);
    }

    public void StartFight()
    {
        isFightActive = true;
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (!isFightActive || playerTransform == null) return;

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator AttackLoop()
    {
        while (isFightActive)
        {
            yield return new WaitForSeconds(attackCooldown);

            if (bossSprite != null) bossSprite.color = telegraphColor;
            yield return new WaitForSeconds(telegraphDuration);

            if (bossSprite != null) bossSprite.color = normalColor;
            if (weaponController != null)
            {
                // --- THE FIX: Ask the specific gun how to fire! ---
                int burstCount = weaponController.GetAIBurstCount();
                float burstSpacing = weaponController.GetAIBurstSpacing();

                for (int i = 0; i < burstCount; i++)
                {
                    weaponController.TryFire();

                    if (i < burstCount - 1)
                    {
                        yield return new WaitForSeconds(burstSpacing);
                    }
                }
            }
        }
    }

    private void CheckForPhaseChange()
    {
        if (currentPhase == 1 && isFightActive && myHealth.GetHealthPercentage() <= 0.5f)
        {
            currentPhase = 2;
            attackCooldown = 1f;
            Debug.Log("PHASE 2!");

            // Example: If you wanted, you could now safely do this:
            // weaponController.EquipWeapon("Laser"); 
            // And the burst math would automatically update itself!
        }
    }

    private void Die()
    {
        isFightActive = false;
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
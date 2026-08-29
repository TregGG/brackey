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

            bool usingChargeLaser = weaponController != null && weaponController.IsCurrentWeaponChargeLaser();

            if (usingChargeLaser)
            {
                // --- CHARGE LASER TELEGRAPH: the sprite flashes for the weapon's own chargeTime
                // (instead of the fixed telegraphDuration used below), giving a clear wind-up window
                // that scales with however long that specific weapon takes to charge. AI always
                // releases at full charge once the window ends - no partial-charge damage for enemies.
                float chargeTime = weaponController.GetChargeTime();

                if (bossSprite != null) bossSprite.color = telegraphColor;
                weaponController.BeginCharging();

                yield return new WaitForSeconds(chargeTime);

                if (bossSprite != null) bossSprite.color = normalColor;
                weaponController.FireFullyChargedShot();
            }
            else
            {
                if (bossSprite != null) bossSprite.color = telegraphColor;
                yield return new WaitForSeconds(telegraphDuration);

                if (bossSprite != null) bossSprite.color = normalColor;
                if (weaponController != null)
                {
                    // --- Ask the specific gun how to fire! ---
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
            // And the burst math / charge-laser telegraph would automatically update itself!
        }
    }

    private void Die()
    {
        isFightActive = false;

        // If the boss dies mid-charge, make sure that charge's VFX/SFX don't get orphaned when
        // we stop the coroutine driving it below.
        if (weaponController != null) weaponController.CancelCharge();

        StopAllCoroutines();
        Destroy(gameObject);
    }
}
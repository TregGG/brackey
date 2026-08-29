using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Cinemachine;

[RequireComponent(typeof(AudioSource))]
public class EnemyWeaponController : MonoBehaviour
{
    [Header("Arsenal Setup")]
    public WeaponDatabase database;
    [Tooltip("The ID of the weapon the enemy spawns with (e.g., 'Laser')")]
    public string weaponID;
    public Transform firePoint;

    private WeaponStatRow currentWeapon;
    private float nextFireTime;
    private AudioSource audioSource;
    private CinemachineImpulseSource impulseSource; // Optional: In case giant boss guns shake the screen

    // Object Pooling for enemy bullets
    private Dictionary<GameObject, ObjectPool<GameObject>> bulletPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // --- Charge laser runtime state (driven externally, e.g. by BossPhaseManager's telegraph coroutine) ---
    private GameObject activeChargeVFX;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void Start()
    {
        EquipWeapon(weaponID);
    }

    public void EquipWeapon(string newWeaponID)
    {
        if (database == null || string.IsNullOrEmpty(newWeaponID)) return;

        WeaponStatRow foundWeapon = database.weapons.Find(w => w.weaponID == newWeaponID);
        if (!string.IsNullOrEmpty(foundWeapon.weaponID))
        {
            // Cancel any charge in progress for the weapon we're leaving - same reasoning as the
            // player-side fix: otherwise a mid-fight weapon swap (e.g. a boss phase change) can
            // leave charge VFX/SFX running forever on a weapon that's no longer equipped.
            EndChargeEffects();

            currentWeapon = foundWeapon;
            weaponID = newWeaponID;
        }
    }

    // --- THE AI TRIGGERS THIS METHOD ---
    public void TryFire()
    {
        // Don't fire if no weapon is equipped or if we are waiting on the fire rate cooldown
        if (string.IsNullOrEmpty(currentWeapon.weaponID) || Time.time < nextFireTime) return;

        Shoot();
        nextFireTime = Time.time + currentWeapon.fireRate;
    }

    // --- Charge laser support: the AI doesn't "hold a button," so a driver script (e.g.
    // BossPhaseManager) calls BeginCharging(), waits GetChargeTime() seconds while playing its
    // own telegraph animation, then calls FireFullyChargedShot(). AI shots always release at
    // full charge - the anticipation comes from the telegraph window itself, not partial damage.
    public bool IsCurrentWeaponChargeLaser()
    {
        return currentWeapon.isChargeLaser;
    }

    public float GetChargeTime()
    {
        return currentWeapon.chargeTime;
    }

    public void BeginCharging()
    {
        if (currentWeapon.chargeVFX != null && firePoint != null)
        {
            activeChargeVFX = Instantiate(currentWeapon.chargeVFX, firePoint.position, firePoint.rotation, firePoint);
        }

        if (currentWeapon.chargeSFX != null)
        {
            audioSource.loop = true;
            audioSource.clip = currentWeapon.chargeSFX;
            audioSource.Play();
        }
    }

    public void FireFullyChargedShot()
    {
        EndChargeEffects();

        if (string.IsNullOrEmpty(currentWeapon.weaponID)) return;

        // AI always fires at full charge, so currentWeapon.damage is used as-is - no need for
        // the scaled-copy trick the player side uses for partial charges.
        Shoot();
    }

    // Call this if the fight/attack is interrupted (e.g. the boss dies) while a charge is
    // mid-flight, so its VFX/SFX don't get orphaned when the coroutine driving it is stopped.
    public void CancelCharge()
    {
        EndChargeEffects();
    }

    private void EndChargeEffects()
    {
        if (activeChargeVFX != null)
        {
            Destroy(activeChargeVFX);
            activeChargeVFX = null;
        }

        if (audioSource.loop)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    private void Shoot()
    {
        // 1. Screen Shake (if the enemy has a CinemachineImpulseSource attached)
        if (currentWeapon.useScreenShake && currentWeapon.shakeMagnitude > 0 && impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(currentWeapon.shakeMagnitude);
        }

        // 2. Audio & Muzzle Flash
        if (currentWeapon.fireSFX != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f); // Slightly wider pitch range for monster guns
            audioSource.PlayOneShot(currentWeapon.fireSFX);
        }

        if (currentWeapon.muzzleFlashVFX != null && firePoint != null)
        {
            Instantiate(currentWeapon.muzzleFlashVFX, firePoint.position, firePoint.rotation, firePoint);
        }

        // 3. Spread Math & Bullet Spawning
        int projectiles = currentWeapon.projectilesPerShot;

        if (projectiles <= 1)
        {
            SpawnBullet(firePoint.rotation);
            return;
        }

        float angleStep = currentWeapon.spreadAngle / (projectiles - 1);
        float startingAngle = -(currentWeapon.spreadAngle / 2f);

        for (int i = 0; i < projectiles; i++)
        {
            float currentAngle = startingAngle + (angleStep * i);
            Quaternion rotationOffset = Quaternion.Euler(0, 0, currentAngle);
            Quaternion finalRotation = firePoint.rotation * rotationOffset;

            SpawnBullet(finalRotation);
        }
    }

    private void SpawnBullet(Quaternion rotation)
    {
        ObjectPool<GameObject> pool = GetBulletPool(currentWeapon.bulletPrefab);
        GameObject bulletGo = pool.Get();

        bulletGo.transform.position = firePoint.position;
        bulletGo.transform.rotation = rotation;

        // --- Force it to the EnemyBullet layer! ---
        bulletGo.layer = LayerMask.NameToLayer("EnemyBullet");

        Bullet bulletScript = bulletGo.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.InitializeBullet(currentWeapon, pool, impulseSource, gameObject.tag);
        }
    }

    // --- Let the AI ask the gun how it should be fired ---
    public int GetAIBurstCount()
    {
        // Fallback to 1 if you forget to set it in the Inspector, so they don't fire 0 times!
        return currentWeapon.aiBurstCount > 0 ? currentWeapon.aiBurstCount : 1;
    }

    public float GetAIBurstSpacing()
    {
        return currentWeapon.aiBurstSpacing > 0f ? currentWeapon.aiBurstSpacing : 0.15f;
    }

    private ObjectPool<GameObject> GetBulletPool(GameObject prefab)
    {
        if (!bulletPools.ContainsKey(prefab))
        {
            bulletPools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => { if (Application.isPlaying) Destroy(obj); },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 200
            );
        }
        return bulletPools[prefab];
    }
}
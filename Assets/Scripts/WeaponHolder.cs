using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using TMPro;

[RequireComponent(typeof(CinemachineImpulseSource), typeof(AudioSource))]
public class WeaponHolder : MonoBehaviour
{
    public WeaponDatabase database;
    public string startingWeaponID = "Pistol";
    public Transform weaponSocket;

    private WeaponStatRow currentWeapon;
    private GameObject currentGunInstance;
    private Transform currentFirePoint;

    private PlayerControls controls;
    private float nextFireTime;

    private bool isHoldingFire = false;
    private bool firePressedThisFrame = false;

    private CinemachineImpulseSource impulseSource;
    private AudioSource audioSource;

    private Dictionary<GameObject, ObjectPool<GameObject>> bulletPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // --- DICTIONARY TRACKERS ---
    private Dictionary<string, int> currentAmmoTracker = new Dictionary<string, int>();
    private Dictionary<string, int> currentCarriedAmmoTracker = new Dictionary<string, int>();
    private Dictionary<string, bool> unlockedWeapons = new Dictionary<string, bool>();

    private bool isReloading = false;
    private Coroutine reloadCoroutine;

    [Header("UI Canvas")]
    public TextMeshProUGUI ammoText;

    [Header("Lock-On UI")]
    public RectTransform lockOnUI;
    private Transform lockedTarget;
    private bool wasHoldingFire = false;
    private Camera mainCam;

    // --- Charge Laser runtime state ---
    private bool isCharging = false;
    private float chargeStartTime;
    private GameObject activeChargeVFX;

    void Awake()
    {
        mainCam = Camera.main;

        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();

        controls = new PlayerControls();

        controls.Gameplay.Fire.performed += ctx => {
            isHoldingFire = true;
            firePressedThisFrame = true;
        };
        controls.Gameplay.Fire.canceled += ctx => isHoldingFire = false;

        // --- INVENTORY INPUTS ---
        InputAction[] weaponSlotActions =
        {
            controls.Gameplay.Weapon1,
            controls.Gameplay.Weapon2,
            controls.Gameplay.Weapon3,
            controls.Gameplay.Weapon4,
            controls.Gameplay.Weapon5,
            controls.Gameplay.Weapon6
        };

        for (int i = 0; i < weaponSlotActions.Length; i++)
        {
            int slotIndex = i; // local copy - capturing "i" directly would make every closure
                               // below share the same variable, so by the time any of them
                               // actually fired they'd all read the loop's final value.
            weaponSlotActions[i].performed += ctx => TryEquipByIndex(slotIndex);
        }

        controls.Gameplay.Reload.performed += ctx => TryReload();
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Start()
    {
        Cursor.visible = false;

        // Initialize our hash maps dynamically based on the database
        foreach (var weapon in database.weapons)
        {
            currentAmmoTracker[weapon.weaponID] = weapon.hasInfiniteAmmo ? weapon.magazineSize : 0;
            currentCarriedAmmoTracker[weapon.weaponID] = 0;
            unlockedWeapons[weapon.weaponID] = false;
        }

        // Unlock and equip the starting weapon
        unlockedWeapons[startingWeaponID] = true;
        TryEquipWeapon(startingWeaponID);
    }

    // Helper method so your 1-4 keys still work based on the database list order
    private void TryEquipByIndex(int index)
    {
        if (database != null && index >= 0 && index < database.weapons.Count)
        {
            TryEquipWeapon(database.weapons[index].weaponID);
        }
    }

    public void TryEquipWeapon(string weaponID)
    {
        if (database == null) return;

        // If we haven't picked up this weapon yet, do nothing
        if (!unlockedWeapons.ContainsKey(weaponID) || !unlockedWeapons[weaponID]) return;

        // If we are already holding it, do nothing
        if (currentGunInstance != null && currentWeapon.weaponID == weaponID) return;

        // Search the database for the weapon
        WeaponStatRow newWeapon = database.weapons.Find(w => w.weaponID == weaponID);

        // Since WeaponStatRow is a struct, we check if the string is empty to see if it failed to find it
        if (string.IsNullOrEmpty(newWeapon.weaponID)) return;

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        isReloading = false;

        // A weapon swap always cancels any in-progress lock-on, regardless of whether the OLD
        // or NEW weapon is a homing weapon. Without this, switching weapons mid-lock leaves
        // lockOnUI active forever, since the clearing logic only ever ran from inside the
        // homing branch of Update() for the weapon that started the lock.
        lockedTarget = null;
        wasHoldingFire = false;
        if (lockOnUI != null) lockOnUI.gameObject.SetActive(false);

        // Same reasoning applies to an in-progress laser charge: cancel its VFX/SFX and reset
        // state so switching away from a charging weapon doesn't leave it running forever.
        EndChargeEffects();
        isCharging = false;

        if (currentGunInstance != null) Destroy(currentGunInstance);

        currentWeapon = newWeapon;
        currentGunInstance = Instantiate(currentWeapon.weaponPrefab, weaponSocket);
        currentFirePoint = currentGunInstance.transform.GetChild(0);

        UpdateAmmoUI();
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
                defaultCapacity: 50,
                maxSize: 500
            );
        }
        return bulletPools[prefab];
    }

    void Update()
    {
        if (isReloading || string.IsNullOrEmpty(currentWeapon.weaponID)) return;

        // --- CHARGE LASER LOGIC (Hold to Charge, Release to Fire) ---
        if (currentWeapon.isChargeLaser)
        {
            HandleChargeLaser();
            return;
        }

        // --- HOMING WEAPON LOGIC (Hold to Lock, Release to Fire) ---
        if (currentWeapon.isHoming)
        {
            if (isHoldingFire)
            {
                FindLockOnTarget();
            }
            else if (wasHoldingFire) // The exact frame the player releases the trigger
            {
                if (Time.time >= nextFireTime && lockedTarget != null)
                {
                    Shoot();
                    nextFireTime = Time.time + currentWeapon.fireRate;
                }

                // Clear the UI and the lock
                lockedTarget = null;
                if (lockOnUI != null) lockOnUI.gameObject.SetActive(false);
            }

            wasHoldingFire = isHoldingFire;
            return; // Skip normal automatic firing logic!
        }

        // --- NORMAL WEAPON LOGIC ---
        bool wantsToShoot = currentWeapon.isAutomatic ? isHoldingFire : firePressedThisFrame;
        firePressedThisFrame = false;

        if (wantsToShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
    }

    // --- Charge laser state machine: mirrors the homing "hold to lock, release to fire" pattern ---
    private void HandleChargeLaser()
    {
        if (isHoldingFire && !isCharging)
        {
            // Just started holding the trigger - begin the charge
            isCharging = true;
            chargeStartTime = Time.time;
            BeginChargeEffects();
        }
        else if (!isHoldingFire && isCharging)
        {
            // Released - fire based on however long we actually held it
            float heldDuration = Time.time - chargeStartTime;
            float chargeFraction = currentWeapon.chargeTime > 0f
                ? Mathf.Clamp01(heldDuration / currentWeapon.chargeTime)
                : 1f;

            EndChargeEffects();
            isCharging = false;

            // Releasing too early just cancels the shot with no visual/damage - true "fizzle."
            // Cooldown (nextFireTime) is still respected even on a fully-charged release.
            if (chargeFraction >= currentWeapon.minChargeFraction && Time.time >= nextFireTime)
            {
                FireChargedLaser(chargeFraction);
                nextFireTime = Time.time + currentWeapon.fireRate;
            }
        }
    }

    private void BeginChargeEffects()
    {
        if (currentWeapon.chargeVFX != null && currentFirePoint != null)
        {
            activeChargeVFX = Instantiate(currentWeapon.chargeVFX, currentFirePoint.position, currentFirePoint.rotation, currentFirePoint);
        }

        if (currentWeapon.chargeSFX != null)
        {
            // Uses the main clip/loop channel so it doesn't collide with PlayOneShot fire SFX,
            // which plays on its own internal channel independently of audioSource.clip.
            audioSource.loop = true;
            audioSource.clip = currentWeapon.chargeSFX;
            audioSource.Play();
        }
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

    private void FireChargedLaser(float chargeFraction)
    {
        if (currentAmmoTracker[currentWeapon.weaponID] <= 0)
        {
            TryReload();
            return;
        }

        currentAmmoTracker[currentWeapon.weaponID]--;
        UpdateAmmoUI();

        if (currentAmmoTracker[currentWeapon.weaponID] <= 0)
        {
            TryReload();
        }

        if (currentWeapon.useScreenShake && currentWeapon.shakeMagnitude > 0)
        {
            impulseSource.GenerateImpulseWithForce(currentWeapon.shakeMagnitude);
        }

        if (currentWeapon.fireSFX != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(currentWeapon.fireSFX);
        }

        if (currentWeapon.muzzleFlashVFX != null)
        {
            Instantiate(currentWeapon.muzzleFlashVFX, currentFirePoint.position, currentFirePoint.rotation, currentFirePoint);
        }

        // --- Scale damage by how fully charged the shot was. WeaponStatRow is a struct, so
        // this copy is local and never touches the real currentWeapon or its ammo/state.
        WeaponStatRow chargedStats = currentWeapon;
        chargedStats.damage = Mathf.Lerp(currentWeapon.damage * currentWeapon.minDamageMultiplier, currentWeapon.damage, chargeFraction);

        int projectiles = currentWeapon.projectilesPerShot;

        if (projectiles <= 1)
        {
            SpawnBullet(currentFirePoint.rotation, chargedStats);
            return;
        }

        float angleStep = currentWeapon.spreadAngle / (projectiles - 1);
        float startingAngle = -(currentWeapon.spreadAngle / 2f);

        for (int i = 0; i < projectiles; i++)
        {
            float currentAngle = startingAngle + (angleStep * i);
            Quaternion rotationOffset = Quaternion.Euler(0, 0, currentAngle);
            Quaternion finalRotation = currentFirePoint.rotation * rotationOffset;

            SpawnBullet(finalRotation, chargedStats);
        }
    }

    private void FindLockOnTarget()
    {
        // 1. Get the mouse position in world space
        Vector2 mousePos = controls.Gameplay.Aim.ReadValue<Vector2>();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mousePos);

        // 2. Scan a 5-unit radius around the MOUSE cursor
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(mouseWorldPos, 5f);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var obj in objectsInRange)
        {
            if (obj.CompareTag("Enemy"))
            {
                Health health = obj.GetComponent<Health>();
                // Only lock onto living things
                if (health != null && health.GetCurrentHealth() > 0)
                {
                    float dist = Vector2.Distance(mouseWorldPos, obj.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = obj.transform;
                    }
                }
            }
        }

        lockedTarget = bestTarget;

        // 3. Snap the UI Reticle over the enemy
        if (lockOnUI != null)
        {
            if (lockedTarget != null)
            {
                lockOnUI.gameObject.SetActive(true);
                lockOnUI.position = mainCam.WorldToScreenPoint(lockedTarget.position);
            }
            else
            {
                lockOnUI.gameObject.SetActive(false);
            }
        }
    }

    private void Shoot()
    {
        if (currentAmmoTracker[currentWeapon.weaponID] <= 0)
        {
            TryReload();
            return;
        }

        currentAmmoTracker[currentWeapon.weaponID]--;
        UpdateAmmoUI();

        if (currentAmmoTracker[currentWeapon.weaponID] <= 0)
        {
            TryReload();
        }

        if (currentWeapon.useScreenShake && currentWeapon.shakeMagnitude > 0)
        {
            impulseSource.GenerateImpulseWithForce(currentWeapon.shakeMagnitude);
        }

        if (currentWeapon.fireSFX != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(currentWeapon.fireSFX);
        }

        if (currentWeapon.muzzleFlashVFX != null)
        {
            Instantiate(currentWeapon.muzzleFlashVFX, currentFirePoint.position, currentFirePoint.rotation, currentFirePoint);
        }

        int projectiles = currentWeapon.projectilesPerShot;

        if (projectiles <= 1)
        {
            SpawnBullet(currentFirePoint.rotation, currentWeapon);
            return;
        }

        float angleStep = currentWeapon.spreadAngle / (projectiles - 1);
        float startingAngle = -(currentWeapon.spreadAngle / 2f);

        for (int i = 0; i < projectiles; i++)
        {
            float currentAngle = startingAngle + (angleStep * i);
            Quaternion rotationOffset = Quaternion.Euler(0, 0, currentAngle);
            Quaternion finalRotation = currentFirePoint.rotation * rotationOffset;

            SpawnBullet(finalRotation, currentWeapon);
        }
    }

    private void TryReload()
    {
        if (isReloading || currentAmmoTracker[currentWeapon.weaponID] == currentWeapon.magazineSize) return;
        if (!currentWeapon.hasInfiniteAmmo && currentCarriedAmmoTracker[currentWeapon.weaponID] <= 0) return;

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;
        if (currentWeapon.reloadSFX != null) audioSource.PlayOneShot(currentWeapon.reloadSFX);

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        int amountNeeded = currentWeapon.magazineSize - currentAmmoTracker[currentWeapon.weaponID];
        int amountToReload = currentWeapon.hasInfiniteAmmo ? amountNeeded : Mathf.Min(amountNeeded, currentCarriedAmmoTracker[currentWeapon.weaponID]);

        currentAmmoTracker[currentWeapon.weaponID] += amountToReload;
        if (!currentWeapon.hasInfiniteAmmo) currentCarriedAmmoTracker[currentWeapon.weaponID] -= amountToReload;

        UpdateAmmoUI();
        isReloading = false;
    }

    public void AddAmmo(string weaponID, int amount)
    {
        if (currentCarriedAmmoTracker.ContainsKey(weaponID))
        {
            currentCarriedAmmoTracker[weaponID] += amount;
            UpdateAmmoUI();

            // If we just picked up ammo for the gun we are holding, and the mag is empty, auto-reload!
            if (currentWeapon.weaponID == weaponID && currentAmmoTracker[weaponID] <= 0)
            {
                TryReload();
            }
        }
    }

    public void UnlockWeapon(string weaponID)
    {
        if (unlockedWeapons.ContainsKey(weaponID))
        {
            unlockedWeapons[weaponID] = true;

            // Fill the magazine completely when picked up
            WeaponStatRow weaponStats = database.weapons.Find(w => w.weaponID == weaponID);
            if (!string.IsNullOrEmpty(weaponStats.weaponID))
            {
                currentAmmoTracker[weaponID] = weaponStats.magazineSize;
            }

            // Equip the weapon (which also updates the UI so you instantly see the full mag)
            TryEquipWeapon(weaponID);
        }
    }

    private void SpawnBullet(Quaternion rotation, WeaponStatRow statsToUse)
    {
        ObjectPool<GameObject> pool = GetBulletPool(statsToUse.bulletPrefab);
        GameObject bulletGo = pool.Get();

        bulletGo.transform.position = currentFirePoint.position;
        bulletGo.transform.rotation = rotation;

        // --- Force it to the PlayerBullet layer! ---
        bulletGo.layer = LayerMask.NameToLayer("PlayerBullet");

        Bullet bulletScript = bulletGo.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            // --- Pass the lockedTarget ---
            bulletScript.InitializeBullet(statsToUse, pool, impulseSource, gameObject.tag, lockedTarget);
        }
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null || string.IsNullOrEmpty(currentWeapon.weaponID)) return;

        string currentMag = currentAmmoTracker[currentWeapon.weaponID].ToString();
        string reserveAmmo = currentWeapon.hasInfiniteAmmo ? "∞" : currentCarriedAmmoTracker[currentWeapon.weaponID].ToString();

        ammoText.text = $"{currentWeapon.weaponID}\n{currentMag} / {reserveAmmo}";
    }
}
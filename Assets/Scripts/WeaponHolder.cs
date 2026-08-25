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

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();

        controls = new PlayerControls();

        controls.Gameplay.Fire.performed += ctx => {
            isHoldingFire = true;
            firePressedThisFrame = true;
        };
        controls.Gameplay.Fire.canceled += ctx => isHoldingFire = false;

        // --- INVENTORY INPUTS ---
        controls.Gameplay.Weapon1.performed += ctx => TryEquipByIndex(0);
        controls.Gameplay.Weapon2.performed += ctx => TryEquipByIndex(1);
        controls.Gameplay.Weapon3.performed += ctx => TryEquipByIndex(2);
        controls.Gameplay.Weapon4.performed += ctx => TryEquipByIndex(3);

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
        // 1. Check if the player is trying to shoot this exact frame
        bool wantsToShoot = string.IsNullOrEmpty(currentWeapon.weaponID) ? false :
                            (currentWeapon.isAutomatic ? isHoldingFire : firePressedThisFrame);

        // 2. IMMEDIATELY consume/reset the single-fire flag so it never buffers!
        firePressedThisFrame = false;

        // 3. Now, if we are reloading, or holding no weapon, just stop here.
        if (isReloading || string.IsNullOrEmpty(currentWeapon.weaponID)) return;

        // 4. If we made it this far, fire the gun!
        if (wantsToShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
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
            SpawnBullet(currentFirePoint.rotation);
            return;
        }

        float angleStep = currentWeapon.spreadAngle / (projectiles - 1);
        float startingAngle = -(currentWeapon.spreadAngle / 2f);

        for (int i = 0; i < projectiles; i++)
        {
            float currentAngle = startingAngle + (angleStep * i);
            Quaternion rotationOffset = Quaternion.Euler(0, 0, currentAngle);
            Quaternion finalRotation = currentFirePoint.rotation * rotationOffset;

            SpawnBullet(finalRotation);
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
            TryEquipWeapon(weaponID);
        }
    }

    private void SpawnBullet(Quaternion rotation)
    {
        ObjectPool<GameObject> pool = GetBulletPool(currentWeapon.bulletPrefab);
        GameObject bulletGo = pool.Get();

        bulletGo.transform.position = currentFirePoint.position;
        bulletGo.transform.rotation = rotation;

        Bullet bulletScript = bulletGo.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.InitializeBullet(currentWeapon, pool, impulseSource);
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
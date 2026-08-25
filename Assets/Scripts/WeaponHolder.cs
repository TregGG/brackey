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
    public int startingWeaponIndex = 0;
    public Transform weaponSocket;

    private int currentWeaponIndex; // Tracks which slot we are currently holding
    private WeaponStatRow currentWeapon;
    private GameObject currentGunInstance;
    private Transform currentFirePoint;

    private PlayerControls controls;
    private float nextFireTime;

    private bool isHoldingFire = false;
    private bool firePressedThisFrame = false;

    private CinemachineImpulseSource impulseSource;
    private AudioSource audioSource;

    // The Pool Dictionary: Maps a bullet prefab to its specific ObjectPool
    private Dictionary<GameObject, ObjectPool<GameObject>> bulletPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // --- AMMO TRACKING VARIABLES ---
    private int[] currentAmmoTracker;
    private int[] currentCarriedAmmoTracker;
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
        controls.Gameplay.Weapon1.performed += ctx => TryEquipWeapon(0);
        controls.Gameplay.Weapon2.performed += ctx => TryEquipWeapon(1);
        controls.Gameplay.Weapon3.performed += ctx => TryEquipWeapon(2);
        controls.Gameplay.Weapon4.performed += ctx => TryEquipWeapon(3);

        // Reload Input
        controls.Gameplay.Reload.performed += ctx => TryReload();
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Start()
    {
        Cursor.visible = false;

        currentAmmoTracker = new int[database.weapons.Count];
        currentCarriedAmmoTracker = new int[database.weapons.Count];

        for (int i = 0; i < database.weapons.Count; i++)
        {
            currentAmmoTracker[i] = database.weapons[i].magazineSize;

            // Just gives us our starting ammo when the game begins
            currentCarriedAmmoTracker[i] = database.weapons[i].startingCarriedAmmo;
        }

        TryEquipWeapon(startingWeaponIndex);
        UpdateAmmoUI();
    }

    private void TryEquipWeapon(int index)
    {
        if (database == null || index < 0 || index >= database.weapons.Count) return;

        // Don't do anything if we are already holding this exact weapon
        if (currentGunInstance != null && currentWeaponIndex == index) return;

        // Cancel any active reloads if we swap weapons!
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        isReloading = false;

        if (currentGunInstance != null) Destroy(currentGunInstance);

        currentWeaponIndex = index;
        currentWeapon = database.weapons[index];
        currentGunInstance = Instantiate(currentWeapon.weaponPrefab, weaponSocket);
        currentFirePoint = currentGunInstance.transform.GetChild(0);

        UpdateAmmoUI();
    }

    // --- POOLING LOGIC ---
    private ObjectPool<GameObject> GetBulletPool(GameObject prefab)
    {
        if (!bulletPools.ContainsKey(prefab))
        {
            bulletPools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),

              
                actionOnDestroy: (obj) =>
                {
                    // Only run this if the game is actually running to prevent Editor errors on exit!
                    if (Application.isPlaying) Destroy(obj);
                },

                collectionCheck: false,
                defaultCapacity: 50,
                maxSize: 500
            );
        }
        return bulletPools[prefab];
    }

    void Update()
    {
        // Block shooting completely if we are in the middle of a reload animation
        if (isReloading) return;

        bool wantsToShoot = currentWeapon.isAutomatic ? isHoldingFire : firePressedThisFrame;

        if (wantsToShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }

        firePressedThisFrame = false;
    }
    private void Shoot()
    {
        // --- AMMO CHECK LOGIC ---
       
            // If the mag is already empty (e.g., if they swapped weapons to cancel a previous reload)
            if (currentAmmoTracker[currentWeaponIndex] <= 0)
            {
                // Instantly force a reload instead of just clicking empty
                TryReload();
                return;
            }

            // Subtract a bullet for firing
            currentAmmoTracker[currentWeaponIndex]--;
            UpdateAmmoUI();

            // --- AUTO-RELOAD TRIGGER ---
            // If that was the absolute last bullet in the mag, start the reload immediately!
            if (currentAmmoTracker[currentWeaponIndex] <= 0)
            {
                TryReload();
            }
        

        if (currentWeapon.useScreenShake && currentWeapon.shakeMagnitude > 0)
        {
            impulseSource.GenerateImpulseWithForce(currentWeapon.shakeMagnitude);
        }

        // --- AUDIO & MUZZLE FLASH ---

        // Play the gunshot sound (PlayOneShot allows multiple rapid shots to overlap cleanly)
        if (currentWeapon.fireSFX != null)
        {
            // Optional: Randomize the pitch slightly so machine guns don't sound like robots!
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(currentWeapon.fireSFX);
        }

        // Spawn the Muzzle Flash, parented to the FirePoint so it follows the gun if the player moves
        if (currentWeapon.muzzleFlashVFX != null)
        {
            Instantiate(currentWeapon.muzzleFlashVFX, currentFirePoint.position, currentFirePoint.rotation, currentFirePoint);
        }

        // --- SPREAD MATH ---
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

    // --- RELOAD LOGIC ---
    private void TryReload()
    {
        // Don't reload if already reloading, if mag is full, or if we have NO reserve ammo left!
        if (isReloading || currentAmmoTracker[currentWeaponIndex] == currentWeapon.magazineSize) return;

        if (!currentWeapon.hasInfiniteAmmo && currentCarriedAmmoTracker[currentWeaponIndex] <= 0) return;

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;
        if (currentWeapon.reloadSFX != null) audioSource.PlayOneShot(currentWeapon.reloadSFX);

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        // --- Calculate exactly how much ammo we need vs how much we have ---
        int amountNeeded = currentWeapon.magazineSize - currentAmmoTracker[currentWeaponIndex];

        // If infinite, give all they need. If not, give the smaller number between what they need and what they have.
        int amountToReload = currentWeapon.hasInfiniteAmmo ? amountNeeded : Mathf.Min(amountNeeded, currentCarriedAmmoTracker[currentWeaponIndex]);

        // Add to the magazine and subtract from the pockets
        currentAmmoTracker[currentWeaponIndex] += amountToReload;
        if (!currentWeapon.hasInfiniteAmmo) currentCarriedAmmoTracker[currentWeaponIndex] -= amountToReload;

        UpdateAmmoUI();
        isReloading = false;
    }

    // --- PICKUP LOGIC ---
    public void AddAmmo(int weaponIndex, int amount)
    {
        if (weaponIndex < 0 || weaponIndex >= currentCarriedAmmoTracker.Length) return;

        currentCarriedAmmoTracker[weaponIndex] += amount;

        UpdateAmmoUI();
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
            // Pass the 'impulseSource' variable we cached in Awake!
            bulletScript.InitializeBullet(currentWeapon, pool, impulseSource);
        }
    }

    // --- UI LOGIC ---
    private void UpdateAmmoUI()
    {
        // Safety check in case the UI isn't hooked up yet
        if (ammoText == null) return;

        string currentMag = currentAmmoTracker[currentWeaponIndex].ToString();

        // If it's infinite, show the infinity symbol (∞). Otherwise, show the reserve number!
        string reserveAmmo = currentWeapon.hasInfiniteAmmo ? "∞" : currentCarriedAmmoTracker[currentWeaponIndex].ToString();

        // This formats it to look like:
        // Shotgun
        // 2 / 20
        ammoText.text = $"{currentWeapon.weaponID}\n{currentMag} / {reserveAmmo}";
    }
}
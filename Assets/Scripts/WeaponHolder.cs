using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Pool;
using System.Collections.Generic;

[RequireComponent(typeof(CinemachineImpulseSource), typeof(AudioSource))]
public class WeaponHolder : MonoBehaviour
{
    public WeaponDatabase database;
    public int startingWeaponIndex = 0;
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

    // The Pool Dictionary: Maps a bullet prefab to its specific ObjectPool
    private Dictionary<GameObject, ObjectPool<GameObject>> bulletPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

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
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Start()
    {
        Cursor.visible = false;
        TryEquipWeapon(startingWeaponIndex);
    }

    private void TryEquipWeapon(int index)
    {
        if (database == null || index < 0 || index >= database.weapons.Count) return;

        if (currentGunInstance != null) Destroy(currentGunInstance);

        currentWeapon = database.weapons[index];
        currentGunInstance = Instantiate(currentWeapon.weaponPrefab, weaponSocket);
        currentFirePoint = currentGunInstance.transform.GetChild(0);
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
}
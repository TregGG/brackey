using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Pool;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class GrenadeThrower : MonoBehaviour
{
    [Header("Setup")]
    public GameObject grenadePrefab;
    public Transform throwPoint; // You can use your player's center or the weapon socket

    [Header("Mechanics")]
    public float throwCooldown = 3f;

    private PlayerControls controls;
    private Camera mainCam;
    private CinemachineImpulseSource impulseSource;
    private ObjectPool<GameObject> grenadePool;
    private float nextThrowTime;

    void Awake()
    {
        mainCam = Camera.main;
        impulseSource = GetComponent<CinemachineImpulseSource>();
        controls = new PlayerControls();

        // Listen for the new input action!
        controls.Gameplay.ThrowGrenade.performed += ctx => TryThrowGrenade();

        // Dedicated pool just for grenades
        grenadePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(grenadePrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => { if (Application.isPlaying) Destroy(obj); },
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 15
        );
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    private void TryThrowGrenade()
    {
        // Don't throw if it's on cooldown
        if (Time.time < nextThrowTime || grenadePrefab == null) return;

        nextThrowTime = Time.time + throwCooldown;

        // Calculate the direction towards the mouse cursor
        Vector2 mousePos = controls.Gameplay.Aim.ReadValue<Vector2>();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mousePos);
        Vector2 throwDirection = ((Vector2)mouseWorldPos - (Vector2)throwPoint.position).normalized;

        // Grab a grenade from the pool and initialize it
        GameObject grenadeGo = grenadePool.Get();
        grenadeGo.transform.position = throwPoint.position;

        HandGrenade grenadeScript = grenadeGo.GetComponent<HandGrenade>();
        if (grenadeScript != null)
        {
            grenadeScript.Initialize(throwDirection, grenadePool, impulseSource);
        }
    }
}
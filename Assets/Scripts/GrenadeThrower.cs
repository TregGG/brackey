using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Pool;
using TMPro; 

[RequireComponent(typeof(CinemachineImpulseSource))]
public class GrenadeThrower : MonoBehaviour
{
    [Header("Setup")]
    public GameObject grenadePrefab;
    public Transform throwPoint;

    [Header("Mechanics")]
    public float throwCooldown = 3f;
    public int startingGrenades = 3;
    public int maxGrenades = 5; 

    [Header("UI Canvas")]
    public TextMeshProUGUI grenadeText;

    private int currentGrenades; // Tracks how many are in your pocket
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

        controls.Gameplay.ThrowGrenade.performed += ctx => TryThrowGrenade();

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

    void Start()
    {
        // Give the player their starting amount and update the screen
        currentGrenades = startingGrenades;
        UpdateGrenadeUI();
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    private void TryThrowGrenade()
    {
        // Abort if we are out of grenades!
        if (Time.time < nextThrowTime || grenadePrefab == null || currentGrenades <= 0) return;

        nextThrowTime = Time.time + throwCooldown;

        // Subtract one grenade and update the UI
        currentGrenades--;
        UpdateGrenadeUI();

        Vector2 mousePos = controls.Gameplay.Aim.ReadValue<Vector2>();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mousePos);
        Vector2 throwDirection = ((Vector2)mouseWorldPos - (Vector2)throwPoint.position).normalized;

        GameObject grenadeGo = grenadePool.Get();
        grenadeGo.transform.position = throwPoint.position;

        HandGrenade grenadeScript = grenadeGo.GetComponent<HandGrenade>();
        if (grenadeScript != null)
        {
            grenadeScript.Initialize(throwDirection, grenadePool, impulseSource);
        }
    }

    // --- So you can pick up grenade crates later ---
    public void AddGrenades(int amount)
    {
        currentGrenades += amount;
        if (currentGrenades > maxGrenades) currentGrenades = maxGrenades;

        UpdateGrenadeUI();
    }

    // --- UI Update Logic ---
    private void UpdateGrenadeUI()
    {
        if (grenadeText != null)
        {
            grenadeText.text = $"Grenades: {currentGrenades}";
        }
    }
}
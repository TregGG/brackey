using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAim : MonoBehaviour
{
    [Header("UI Crosshair")]
    [Tooltip("Drag the UICrosshair object from the Canvas here")]
    public RectTransform crosshairUI;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private Camera mainCam;
    private Vector2 aimScreenPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        mainCam = Camera.main;
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Update()
    {
        // 1. Read the mouse position continuously
        aimScreenPosition = controls.Gameplay.Aim.ReadValue<Vector2>();

        // 2. Snap the UI crosshair exactly to the mouse's screen pixels
        if (crosshairUI != null)
        {
            crosshairUI.position = aimScreenPosition;
        }
    }

    void FixedUpdate()
    {
        // 3. Keep physics-based rotation using World Space
        Vector2 playerPos = rb.position;

        // We still need world space to know where the mouse is relative to the player
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(aimScreenPosition);

        Vector2 lookDir = (Vector2)mouseWorldPos - playerPos;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        rb.MoveRotation(angle);
    }
}
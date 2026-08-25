using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Locomotion")]
    public float moveSpeed = 7f;
    public float sprintSpeed = 12f;

    [Header("Dash Mechanics")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Audio & Visuals")]
    public AudioClip dashSFX;
    public GameObject dashVFX; // For the initial dust burst
    public TrailRenderer dashTrail; // For the swoosh trailing behind the player

    private Rigidbody2D rb;
    private Vector2 movement;
    private AudioSource audioSource;

    // State trackers
    private bool isDashing = false;
    private bool canDash = true;
    private bool isSprinting = false;

    private PlayerControls controls;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        controls = new PlayerControls();

        controls.Gameplay.Dash.performed += context => OnDash();
        controls.Gameplay.Sprint.performed += context => isSprinting = true;
        controls.Gameplay.Sprint.canceled += context => isSprinting = false;
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Update()
    {
        if (isDashing) return;
        movement = controls.Gameplay.Move.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = movement * currentSpeed;
    }

    private void OnDash()
    {
        if (canDash && movement != Vector2.zero && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        // --- Trigger SFX and VFX ---
        if (dashSFX != null) audioSource.PlayOneShot(dashSFX);

        // Spawn the dust burst at the exact spot the player started the dash
        if (dashVFX != null) Instantiate(dashVFX, transform.position, Quaternion.identity);

        // Turn on the trail renderer
        if (dashTrail != null) dashTrail.emitting = true;

        rb.linearVelocity = movement * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;

        // Turn off the trail renderer the moment the dash ends
        if (dashTrail != null) dashTrail.emitting = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
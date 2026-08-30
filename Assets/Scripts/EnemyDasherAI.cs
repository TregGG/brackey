using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDasherAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float dashSpeed = 15f;
    public float dashDistance = 6f;
    public float dashDuration = 0.2f;
    public float telegraphTime = 0.5f;
    public float contactDamage = 25f;

    [Header("Aggro Settings")]
    public float aggroRange = 24f;
    private bool hasAggro = false;

    private Transform player;
    private Rigidbody2D rb;
    private bool isDashing = false;
    private bool isPreparingDash = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        // For Dasher specifically, keep your "|| isDashing" check here:
        // if (player == null || isDashing) return;
        if (player == null) return;

        // --- AGGRO CHECK ---
        if (!hasAggro)
        {
            // Check how far away the player is
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= aggroRange)
            {
                hasAggro = true; // The switch flips! They will never forget you now.
            }
            else
            {
                return; // Stop reading the script here. Don't aim, don't move.
            }
        }

        // --- AIMING LOGIC ---
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    void FixedUpdate()
    {
        // If there is no player, OR if we haven't spotted them yet, do nothing!
        if (player == null || !hasAggro) return;

        if (player == null || isDashing || isPreparingDash) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= dashDistance)
        {
            StartCoroutine(DashAttack());
        }
        else
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }

    private IEnumerator DashAttack()
    {
        isPreparingDash = true;
        rb.linearVelocity = Vector2.zero; // Stop to telegraph

        // Optional: Change sprite color here to telegraph the attack

        yield return new WaitForSeconds(telegraphTime);

        isPreparingDash = false;
        isDashing = true;

        if (player != null)
        {
            Vector2 dashDir = (player.position - transform.position).normalized;
            rb.linearVelocity = dashDir * dashSpeed;
        }

        yield return new WaitForSeconds(dashDuration);

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Health hitHealth = collision.gameObject.GetComponent<Health>();
            if (hitHealth != null) hitHealth.TakeDamage(contactDamage);
        }
    }
    // --- Editor Only: Visualize the Aggro Range ---
    void OnDrawGizmosSelected()
    {
        // Set the color of the circle (you can change this to Color.yellow, Color.red, etc.)
        Gizmos.color = Color.red;

        // Draw a wireframe sphere using the enemy's position and the aggroRange variable
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
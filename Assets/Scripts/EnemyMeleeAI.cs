using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMeleeAI : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float contactDamage = 15f;

    [Tooltip("How often (in seconds) the enemy can tick damage while touching the player")]
    public float damageCooldown = 0.5f;

    [Header("Aggro Settings")]
    public float aggroRange = 24f;
    private bool hasAggro = false;

    private Transform player;
    private Rigidbody2D rb;
    private float nextDamageTime;

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
        if (player == null) return;

        Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    // THE FIX: A unified method to handle both Solid Collisions and Triggers
    private void TryDamagePlayer(GameObject target)
    {
        if (Time.time < nextDamageTime) return; // Prevent instant-kill melting

        if (target.CompareTag("Player"))
        {
            Health hitHealth = target.GetComponent<Health>();
            if (hitHealth != null)
            {
                hitHealth.TakeDamage(contactDamage);
                nextDamageTime = Time.time + damageCooldown;
            }
        }
    }

    // Catches solid physics bumps (staying in contact)
    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    // Catches overlaps/triggers
    void OnTriggerStay2D(Collider2D collider)
    {
        TryDamagePlayer(collider.gameObject);
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
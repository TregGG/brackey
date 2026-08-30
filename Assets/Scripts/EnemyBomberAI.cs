using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyBomberAI : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public float fuseDistance = 1.5f;


    [Header("Aggro Settings")]
    public float aggroRange = 24f;
    private bool hasAggro = false;

    [Header("Explosion Stats")]
    public float explosionRadius = 3f;
    public float explosionDamage = 50f;
    public float explosionForce = 15f;
    public float shakeMagnitude = 1f;
    public GameObject explosionVFX;
    public AudioClip explosionSFX;

    private Transform player;
    private Rigidbody2D rb;
    private Health myHealth;
    private bool hasDetonated = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myHealth = GetComponent<Health>();
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

        if (player == null || hasDetonated) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= fuseDistance)
        {
            Detonate();
        }
        else
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }

    private void Detonate()
    {
        hasDetonated = true;

        // Visuals & Audio
        if (explosionSFX != null) AudioSource.PlayClipAtPoint(explosionSFX, transform.position);
        if (explosionVFX != null) Instantiate(explosionVFX, transform.position, Quaternion.identity);

        CinemachineImpulseSource impulse = GetComponent<CinemachineImpulseSource>();
        if (shakeMagnitude > 0 && impulse != null) impulse.GenerateImpulseWithForce(shakeMagnitude);

        // Calculate Damage
        Collider2D[] objectsInBlast = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D obj in objectsInBlast)
        {
            if (obj.gameObject == this.gameObject || obj.CompareTag("Enemy")) continue;

            Health hitHealth = obj.GetComponent<Health>();
            if (hitHealth != null) hitHealth.TakeDamage(explosionDamage);

            Rigidbody2D hitRb = obj.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                Vector2 pushDirection = (obj.transform.position - transform.position).normalized;
                hitRb.AddForce(pushDirection * explosionForce, ForceMode2D.Impulse);
            }
        }

        // Let the health script know we died (in case UI is tracking it)
        myHealth.TakeDamage(myHealth.maxHealth);

        // THE FIX: Actually delete the enemy from the scene
        Destroy(gameObject);
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
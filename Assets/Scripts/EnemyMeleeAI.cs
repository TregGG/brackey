using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMeleeAI : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float contactDamage = 15f;

    [Tooltip("How often (in seconds) the enemy can tick damage while touching the player")]
    public float damageCooldown = 0.5f;

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
        if (player == null) return;

        // Mathematically aim at the player
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void FixedUpdate()
    {
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
}
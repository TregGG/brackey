using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyWeaponController))]
public class EnemyShooterAI : MonoBehaviour
{

    public float moveSpeed = 3f;
    public float stoppingDistance = 5f;
    public float retreatDistance = 3f;

    [Header("Aggro Settings")]
    public float aggroRange = 24f;
    private bool hasAggro = false;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyWeaponController weaponController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weaponController = GetComponent<EnemyWeaponController>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        // --- AGGRO CHECK ---
        if (!hasAggro)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= aggroRange)
            {
                hasAggro = true;
            }
            else
            {
                return; // Stop the script. Don't move, aim, or shoot!
            }
        }

        // Aim at player
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Movement logic
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > stoppingDistance)
        {
            rb.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
        else if (distance < retreatDistance)
        {
            rb.position = Vector2.MoveTowards(transform.position, player.position, -moveSpeed * Time.deltaTime);
        }

        // Fire weapon using your existing framework
        weaponController.TryFire();
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
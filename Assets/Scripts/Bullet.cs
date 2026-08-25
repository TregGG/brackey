using UnityEngine;
using UnityEngine.Pool;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private WeaponStatRow myStats;
    private float lifeTimer;

    private Rigidbody2D rb;
    private CinemachineImpulseSource playerImpulseSource;
    private ObjectPool<GameObject> myPool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void InitializeBullet(WeaponStatRow stats, ObjectPool<GameObject> pool, CinemachineImpulseSource impulse)
    {
        myStats = stats;
        myPool = pool;
        playerImpulseSource = impulse;
        lifeTimer = 0f;

        if (myStats.isLaser)
        {
            rb.linearVelocity = Vector2.zero;
            FireLaser();
        }
        else
        {
            // Standard physical bullet
            rb.linearVelocity = transform.right * myStats.bulletSpeed;
        }
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= myStats.bulletLifeTime) ReturnToPool();
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        if (myPool != null) myPool.Release(gameObject);
        else Destroy(gameObject);
    }

    private void FireLaser()
    {
        // Start by assuming the laser will go the maximum distance
        float actualRange = myStats.laserRange;

        // 1. Fire the mathematical raycast first
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, transform.right, actualRange);

        // 2. Sort the array so we process hits from closest to furthest
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            // Ignore the player and triggers
            if (hit.collider.CompareTag("Player") || hit.collider.isTrigger) continue;

            // Play effects for the object we just hit
            if (myStats.impactSFX != null) AudioSource.PlayClipAtPoint(myStats.impactSFX, hit.point);
            if (myStats.impactVFX != null) Instantiate(myStats.impactVFX, hit.point, Quaternion.identity);

            // Push the object
            Rigidbody2D hitRb = hit.collider.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                hitRb.AddForce(transform.right * myStats.impactForce, ForceMode2D.Impulse);
            }

            // --- THE PENETRATION LOGIC ---
            // If the object is NOT tagged "Enemy", it acts as a solid wall/obstacle.
            if (!hit.collider.CompareTag("Enemy"))
            {
                actualRange = hit.distance; // Shorten the visual line to this exact impact point
                break; // Break the loop so nothing behind this obstacle gets hit
            }
        }

        // 3. Draw the visual beam using the newly calculated range
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.SetPosition(0, transform.position); // Start at the gun barrel
            lr.SetPosition(1, transform.position + (transform.right * actualRange)); // End at the obstacle
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // PREVENT BUG: If this is a laser, ignore physical collisions entirely!
        if (myStats.isLaser) return;

        Vector2 contactPoint = collision.contacts[0].point;

        if (myStats.impactSFX != null) AudioSource.PlayClipAtPoint(myStats.impactSFX, contactPoint);
        if (myStats.impactVFX != null) Instantiate(myStats.impactVFX, contactPoint, Quaternion.identity);

        if (!myStats.isExplosive)
        {
            Health hitHealth = collision.gameObject.GetComponent<Health>();
            if (hitHealth != null) hitHealth.TakeDamage(myStats.damage);

            Rigidbody2D hitRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (hitRb != null) hitRb.AddForce(transform.right * myStats.impactForce, ForceMode2D.Impulse);
        }
        else
        {
            Explode(contactPoint);
        }

        ReturnToPool();
    }

    private void Explode(Vector2 blastCenter)
    {
        // Uses the PLAYER'S impulse source, which never gets deactivated!
        if (myStats.explosionShakeMagnitude > 0 && playerImpulseSource != null)
        {
            playerImpulseSource.GenerateImpulseWithForce(myStats.explosionShakeMagnitude);
        }

        Collider2D[] objectsInBlast = Physics2D.OverlapCircleAll(blastCenter, myStats.explosionRadius);

        foreach (Collider2D obj in objectsInBlast)
        {
            if (obj.gameObject == this.gameObject || obj.CompareTag("Player")) continue;

            // Check for health in the blast radius
            Health hitHealth = obj.GetComponent<Health>();
            if (hitHealth != null) hitHealth.TakeDamage(myStats.damage);

            Rigidbody2D hitRb = obj.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                Vector2 pushDirection = (obj.transform.position - (Vector3)blastCenter).normalized;
                hitRb.AddForce(pushDirection * myStats.impactForce, ForceMode2D.Impulse);
            }
        }
    }
}
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

    // Now accepts the Player's Impulse Source!
    public void InitializeBullet(WeaponStatRow stats, ObjectPool<GameObject> pool, CinemachineImpulseSource impulse)
    {
        myStats = stats;
        myPool = pool;
        playerImpulseSource = impulse;
        lifeTimer = 0f;

        rb.linearVelocity = transform.right * myStats.bulletSpeed;
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Get the exact pixel where the bullet touched the outside of the box
        Vector2 contactPoint = collision.contacts[0].point;

        if (myStats.impactSFX != null) AudioSource.PlayClipAtPoint(myStats.impactSFX, contactPoint);
        if (myStats.impactVFX != null) Instantiate(myStats.impactVFX, contactPoint, Quaternion.identity);

        if (!myStats.isExplosive)
        {
            Rigidbody2D hitRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                // Standard bullets push in their flight direction
                hitRb.AddForce(transform.right * myStats.impactForce, ForceMode2D.Impulse);
            }
        }
        else
        {
            // Pass the exact contact point to the explosion
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

            Rigidbody2D hitRb = obj.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                // Math is now perfectly calculated from the outside edge of the box!
                Vector2 pushDirection = (obj.transform.position - (Vector3)blastCenter).normalized;
                hitRb.AddForce(pushDirection * myStats.impactForce, ForceMode2D.Impulse);
            }
        }
    }
}
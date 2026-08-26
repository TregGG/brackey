using UnityEngine;
using UnityEngine.Pool;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private string ownerTag;

    private WeaponStatRow myStats;
    private float lifeTimer;

    // --- For Homing Missiles ---
    private Transform currentTarget;
    private Collider2D targetCollider; // cached alongside currentTarget for surface-distance checks

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private CinemachineImpulseSource playerImpulseSource;
    private ObjectPool<GameObject> myPool;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
    }

    public void InitializeBullet(WeaponStatRow stats, ObjectPool<GameObject> pool, CinemachineImpulseSource impulse, string shooterTag, Transform preLockedTarget = null)
    {
        myStats = stats;
        myPool = pool;
        playerImpulseSource = impulse;
        ownerTag = shooterTag;
        lifeTimer = 0f;

        currentTarget = preLockedTarget; // Use the target the player locked onto
        targetCollider = currentTarget != null ? currentTarget.GetComponent<Collider2D>() : null;

        // --- THE FIX: force the physics world to catch up with the transform.position/rotation
        // the spawner just assigned. Without this, a reused pooled bullet's Collider2D can still be
        // registered at its OLD location (e.g. wherever it last exploded, right on the target) until
        // the next physics step, causing an immediate false-positive on the homing proximity fuse below.
        Physics2D.SyncTransforms();

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();

        if (myStats.isLaser)
        {
            rb.linearVelocity = Vector2.zero;
            FireLaser();
        }
        else
        {
            rb.linearVelocity = transform.right * myStats.bulletSpeed;
        }
    }



    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= myStats.bulletLifeTime)
        {
            // --- Explode when lifetime finishes instead of disappearing ---
            if (myStats.isExplosive) Explode(transform.position);
            ReturnToPool();
            return; // Stop running code so we don't steer a dead bullet!
        }

        if (myStats.isHoming)
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget();
            }

            if (currentTarget != null)
            {
                Vector2 direction = ((Vector2)currentTarget.position - rb.position).normalized;
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, myStats.homingTurnSpeed * Time.deltaTime);

                // --- Proximity fuse: uses the closest surface-to-surface gap (Physics2D.Distance),
                // so it automatically scales with target size instead of using a fixed center-to-center radius.
                // This catches missiles that orbit a target instead of ever landing a clean physics hit,
                // which happens near targets because the required turn rate approaches infinity as range -> 0.
                if (targetCollider != null && myCollider != null)
                {
                    ColliderDistance2D distInfo = Physics2D.Distance(myCollider, targetCollider);
                    float fuseBuffer = myStats.homingDetonationRadius > 0f ? myStats.homingDetonationRadius : 0.15f;

                    // distInfo.distance goes negative once the colliders actually overlap, so this also
                    // safely catches the case where a real collision should have fired but didn't.
                    if (distInfo.distance <= fuseBuffer)
                    {
                        if (myStats.isExplosive)
                        {
                            Explode(transform.position);
                        }
                        else
                        {
                            Health hitHealth = currentTarget.GetComponent<Health>();
                            if (hitHealth != null) hitHealth.TakeDamage(myStats.damage);
                        }
                        ReturnToPool();
                        return;
                    }
                }
            }

            rb.linearVelocity = transform.right * myStats.bulletSpeed;
        }
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
            // Ignore the shooter and triggers (using the ownerTag we set up earlier!)
            if (hit.collider.CompareTag(ownerTag) || hit.collider.isTrigger) continue;

            // Play effects for the object we just hit
            if (myStats.impactSFX != null) AudioSource.PlayClipAtPoint(myStats.impactSFX, hit.point);
            if (myStats.impactVFX != null) Instantiate(myStats.impactVFX, hit.point, Quaternion.identity);

            // --- DEAL DAMAGE ---
            Health hitHealth = hit.collider.GetComponent<Health>();
            if (hitHealth != null)
            {
                hitHealth.TakeDamage(myStats.damage);
            }

            // Push the object
            Rigidbody2D hitRb = hit.collider.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                hitRb.AddForce(transform.right * myStats.impactForce, ForceMode2D.Impulse);
            }

            // If the object does NOT have health (like a wall or crate), it blocks the laser.
            // If it DOES have health (Boss, Player, standard enemy), the laser slices right through it!
            if (hitHealth == null)
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

    private void FindTarget()
    {
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, myStats.homingDetectionRadius);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;
        Collider2D closestCollider = null;

        foreach (Collider2D obj in objectsInRange)
        {
            if (obj.gameObject == this.gameObject || obj.CompareTag(ownerTag)) continue;

            // --- THE FIX: Only track opposing teams! ---
            if (ownerTag == "Player" && !obj.CompareTag("Enemy")) continue;
            if (ownerTag != "Player" && !obj.CompareTag("Player")) continue;

            if (obj.GetComponent<Health>() != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = obj.transform;
                    closestCollider = obj;
                }
            }
        }
        currentTarget = closestTarget;
        targetCollider = closestCollider;
    }

    private void Explode(Vector2 blastCenter)
    {
        // --- THE FIX: Play the effects! ---
        if (myStats.impactSFX != null) AudioSource.PlayClipAtPoint(myStats.impactSFX, blastCenter);
        if (myStats.impactVFX != null) Instantiate(myStats.impactVFX, blastCenter, Quaternion.identity);

        if (myStats.explosionShakeMagnitude > 0 && playerImpulseSource != null)
        {
            playerImpulseSource.GenerateImpulseWithForce(myStats.explosionShakeMagnitude);
        }

        Collider2D[] objectsInBlast = Physics2D.OverlapCircleAll(blastCenter, myStats.explosionRadius);
        foreach (Collider2D obj in objectsInBlast)
        {
            if (obj.gameObject == this.gameObject || obj.CompareTag(ownerTag)) continue;

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
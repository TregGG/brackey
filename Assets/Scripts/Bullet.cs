using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private float speed;
    private float lifeTime;
    private float lifeTimer;
    private Rigidbody2D rb;
    private ObjectPool<GameObject> myPool;

    // Cached VFX and SFX
    private AudioClip hitSFX;
    private GameObject hitVFX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Now accepts the SFX and VFX as parameters
    public void InitializeBullet(float shotSpeed, float shotLifeTime, ObjectPool<GameObject> pool, AudioClip sfx, GameObject vfx)
    {
        speed = shotSpeed;
        lifeTime = shotLifeTime;
        myPool = pool;

        hitSFX = sfx;
        hitVFX = vfx;

        lifeTimer = 0f;
        rb.linearVelocity = transform.right * speed;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;

        if (myPool != null) myPool.Release(gameObject);
        else Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Don't hit the player! (Ensure your player has the tag "Player")
        if (hitInfo.CompareTag("Player")) return;

        // 1. Play the impact sound perfectly in 2D space
        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, transform.position);
        }

        // 2. Spawn the visual effect
        if (hitVFX != null)
        {
            Instantiate(hitVFX, transform.position, Quaternion.identity);
        }

        // 3. Go back to sleep
        ReturnToPool();
    }
}
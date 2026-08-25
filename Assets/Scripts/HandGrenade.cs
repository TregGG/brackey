using UnityEngine;
using UnityEngine.Pool;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class HandGrenade : MonoBehaviour
{
    [Header("Grenade Stats")]
    public float throwSpeed = 15f;
    public float fuseTime = 2f;
    public float explosionRadius = 3f;
    public float explosionForce = 20f;
    public float damage = 75f;
    public float shakeMagnitude = 1f;

    [Header("Audio & Visuals")]
    public AudioClip bounceSFX;
    public AudioClip explosionSFX;
    public GameObject explosionVFX;

    private float timer;
    private Rigidbody2D rb;
    private CinemachineImpulseSource playerImpulseSource;
    private ObjectPool<GameObject> myPool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, ObjectPool<GameObject> pool, CinemachineImpulseSource impulse)
    {
        myPool = pool;
        playerImpulseSource = impulse;
        timer = 0f;

        rb.linearVelocity = direction * throwSpeed;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= fuseTime) Explode();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (bounceSFX != null) AudioSource.PlayClipAtPoint(bounceSFX, transform.position);
    }

    private void Explode()
    {
        if (explosionSFX != null) AudioSource.PlayClipAtPoint(explosionSFX, transform.position);
        if (explosionVFX != null) Instantiate(explosionVFX, transform.position, Quaternion.identity);
        if (shakeMagnitude > 0 && playerImpulseSource != null) playerImpulseSource.GenerateImpulseWithForce(shakeMagnitude);

        Collider2D[] objectsInBlast = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D obj in objectsInBlast)
        {
            if (obj.gameObject == this.gameObject || obj.CompareTag("Player")) continue;

            Health hitHealth = obj.GetComponent<Health>();
            if (hitHealth != null) hitHealth.TakeDamage(damage); // Uses local damage

            Rigidbody2D hitRb = obj.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                //  Uses transform.position and local explosionForce
                Vector2 pushDirection = (obj.transform.position - transform.position).normalized;
                hitRb.AddForce(pushDirection * explosionForce, ForceMode2D.Impulse);
            }
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        if (myPool != null) myPool.Release(gameObject);
        else Destroy(gameObject);
    }
}
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeath : MonoBehaviour
{
    void Awake()
    {
        // Automatically find the Health script and wire up the death event
        Health myHealth = GetComponent<Health>();
        myHealth.onDie.AddListener(Despawn);
    }

    private void Despawn()
    {
        // Add particle effects or sound drops here later if you want!
        Destroy(gameObject);
    }
}
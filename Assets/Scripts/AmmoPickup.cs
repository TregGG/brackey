using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [Tooltip("0 = Pistol, 1 = Shotgun, 2 = RPG")]
    public int targetWeaponIndex = 1;
    public int ammoAmount = 10;

    [Header("Audio")]
    public AudioClip pickupSFX;

    void Awake()
    {
        // Ensure this is a trigger so the player walks through it, not into it!
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Only the player can pick this up
        if (hitInfo.CompareTag("Player"))
        {
            WeaponHolder playerWeapons = hitInfo.GetComponent<WeaponHolder>();
            if (playerWeapons != null)
            {
                // Send the ammo to the player's pockets
                playerWeapons.AddAmmo(targetWeaponIndex, ammoAmount);

                if (pickupSFX != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
                }

                // Destroy the crate
                Destroy(gameObject);
            }
        }
    }
}
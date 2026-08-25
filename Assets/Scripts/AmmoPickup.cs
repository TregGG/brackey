using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [Tooltip("Type the exact weaponID from the database (e.g., 'Shotgun')")]
    public string targetWeaponID;
    public int ammoAmount = 10;

    [Header("Audio")]
    public AudioClip pickupSFX;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Player"))
        {
            WeaponHolder playerWeapons = hitInfo.GetComponent<WeaponHolder>();
            if (playerWeapons != null)
            {
                // Send the ammo to the player's pockets using the string
                playerWeapons.AddAmmo(targetWeaponID, ammoAmount);

                if (pickupSFX != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}
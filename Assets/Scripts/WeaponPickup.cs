using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Setup")]
    [Tooltip("Type the exact weaponID from the database (e.g., 'Shotgun')")]
    public string targetWeaponID;

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
                // 1. Unlock, fill the magazine, and equip the gun
                playerWeapons.UnlockWeapon(targetWeaponID);

                // 2. Play sound and destroy
                if (pickupSFX != null) AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Setup")]
    [Tooltip("0 = Pistol, 1 = Shotgun, 2 = RPG, 3 = Laser")]
    public int targetWeaponIndex;

    [Header("Bonus Ammo (Optional)")]
    public int bonusReserveAmmo = 10;

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
                // 1. Unlock and equip the gun
                playerWeapons.UnlockWeapon(targetWeaponIndex);

                // 2. Give them some reserve ammo so they can keep shooting after the first mag
                playerWeapons.AddAmmo(targetWeaponIndex, bonusReserveAmmo);

                // 3. Play sound and destroy
                if (pickupSFX != null) AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
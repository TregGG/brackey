using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Setup")]
    public string targetWeaponID;

    [Header("Audio")]
    public AudioClip pickupSFX;

    [Header("Events")]
    public UnityEvent onPickup; 

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
                // Unlock and equip
                playerWeapons.UnlockWeapon(targetWeaponID);

                // Play sound
                if (pickupSFX != null) AudioSource.PlayClipAtPoint(pickupSFX, transform.position);

                //Shout to the Inspector that the weapon was picked up
                onPickup?.Invoke();

                // Destroy
                Destroy(gameObject);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WeaponStatRow
{
    public string weaponID;
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;

    [Header("Core Stats")]
    public float damage;
    public float fireRate;
    public float bulletSpeed;
    public float bulletLifeTime;
    public bool isAutomatic;
    public float impactForce;

    [Header("Shotgun Stats")]
    public int projectilesPerShot;
    public float spreadAngle;

    [Header("Screen Shake (Firing)")]
    public bool useScreenShake;
    public float shakeMagnitude;

    [Header("Audio & Visuals")]
    public AudioClip fireSFX;
    public GameObject muzzleFlashVFX;
    public AudioClip impactSFX;
    public GameObject impactVFX;

    [Header("Explosive Stats (RPG)")]
    public bool isExplosive;
    public float explosionRadius;
    public float explosionShakeMagnitude;

    // --- Homing Stats ---
    [Header("Homing Stats (Missiles)")]
    public bool isHoming;
    [Tooltip("How fast the missile can turn (e.g., 200 for slow, 600 for sharp turns)")]
    public float homingTurnSpeed;
    [Tooltip("How far the missile's radar can 'see' to find a target")]
    public float homingDetectionRadius;
    [Tooltip("Forgiveness margin (in world units) added to the closest surface-to-surface gap before detonating. Scales automatically with target size — 0.15-0.3 is usually plenty, you don't need a big value here.")]
    public float homingDetonationRadius;

    [Header("Laser Stats")]
    public bool isLaser;
    public float laserRange;

    // --- Charge Laser Stats ---
    [Header("Charge Laser Stats")]
    [Tooltip("Check this along with isLaser=true to make this a hold-to-charge, release-to-fire weapon")]
    public bool isChargeLaser;
    [Tooltip("Seconds of holding fire needed to reach full charge")]
    public float chargeTime;
    [Range(0f, 1f)]
    [Tooltip("Minimum charge fraction (0-1) required to release a shot at all. Releasing earlier than this just cancels the charge with no shot fired.")]
    public float minChargeFraction;
    [Range(0f, 1f)]
    [Tooltip("Damage multiplier applied when released at exactly minChargeFraction. Scales linearly up to 1x (full damage) at full charge. AI-fired shots always use full charge, so this only affects the player.")]
    public float minDamageMultiplier;
    [Tooltip("Optional looping sound played while charging")]
    public AudioClip chargeSFX;
    [Tooltip("Optional VFX (e.g. a growing energy orb) spawned at the fire point while charging")]
    public GameObject chargeVFX;

    [Header("Ammo & Reloading")]
    public bool hasInfiniteAmmo;
    public int magazineSize;
    public float reloadTime;
    public AudioClip reloadSFX; // The sound of swapping a mag
    public AudioClip emptyClickSFX; // The "click" when you try to shoot with 0 ammo

    // --- AI Attack Patterns ---
    [Header("AI Attack Pattern")]
    [Tooltip("How many times an enemy pulls the trigger per attack")]
    public int aiBurstCount;
    [Tooltip("Time between shots during an enemy's burst")]
    public float aiBurstSpacing;
}

[CreateAssetMenu(fileName = "WeaponDB", menuName = "Database/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponStatRow> weapons;
}
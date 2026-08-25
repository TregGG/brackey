using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WeaponStatRow
{
    public string weaponID;
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;

    [Header("Core Stats")]
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

    [Header("Laser Stats")]
    public bool isLaser;
    public float laserRange;

    [Header("Ammo & Reloading")]
    public bool hasInfiniteAmmo;
    public int magazineSize;
    public int startingCarriedAmmo;
    public float reloadTime;
    public AudioClip reloadSFX; // The sound of swapping a mag
    public AudioClip emptyClickSFX; // The "click" when you try to shoot with 0 ammo
}

[CreateAssetMenu(fileName = "WeaponDB", menuName = "Database/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponStatRow> weapons;
}
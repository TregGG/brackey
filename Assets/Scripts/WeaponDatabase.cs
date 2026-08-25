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

    [Header("Shotgun Stats")]
    public int projectilesPerShot;
    public float spreadAngle;

    [Header("Screen Shake")]
    public bool useScreenShake;
    public float shakeMagnitude;

    [Header("Audio & Visuals")]
    public AudioClip fireSFX;
    public GameObject muzzleFlashVFX;
    public AudioClip impactSFX;
    public GameObject impactVFX;
}

[CreateAssetMenu(fileName = "WeaponDB", menuName = "Database/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponStatRow> weapons;
}
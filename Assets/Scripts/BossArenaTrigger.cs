using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossArenaTrigger : MonoBehaviour
{
    [Header("Boss Reference")]
    public BossPhaseManager bossManager;

    [Header("UI Reference")]
    public BossHealthBar bossUI;

    [Header("Optional Arena Setup")]
    public GameObject arenaDoors; // Drag your locked doors here to trap the player!
    public AudioClip bossMusic;
    private AudioSource audioSource;

    private bool hasTriggered = false;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        audioSource = Camera.main.GetComponent<AudioSource>(); // Grabs the camera's audio source for music
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            // Wake up the UI!
            if (bossUI != null && bossManager != null)
            {
                bossUI.InitializeHealthBar(bossManager.GetComponent<Health>());
            }

            hasTriggered = true;

            // 1. Wake up the boss!
            if (bossManager != null)
            {
                bossManager.StartFight();
            }

            // 2. Trap the player inside (if you assigned doors)
            if (arenaDoors != null)
            {
                arenaDoors.SetActive(true);
            }

            // 3. Play boss music
            if (bossMusic != null && audioSource != null)
            {
                audioSource.clip = bossMusic;
                audioSource.Play();
            }
        }
    }
}
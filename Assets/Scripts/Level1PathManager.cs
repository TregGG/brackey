using UnityEngine;

public class Level1PathManager : MonoBehaviour
{
    [Header("The Doors (Assign solid obstacles here)")]
    public GameObject aresBossDoor;
    public GameObject evaShortcutDoor;

    void Start()
    {
        // Make sure both paths are closed when the level starts!
        if (aresBossDoor != null) aresBossDoor.SetActive(true);
        if (evaShortcutDoor != null) evaShortcutDoor.SetActive(true);
    }

    // Call this if they pick ARES (Yes)
    public void OpenAresPath()
    {
        // ARES path leads through a major fight guarding a strong weapon.
        if (aresBossDoor != null) aresBossDoor.SetActive(false); // Opens the fight path
        if (evaShortcutDoor != null) evaShortcutDoor.SetActive(true); // Locks the shortcut

        // Record the choice in our memory bank
        StoryManager.Instance.SetChoice1Ares();
    }

    // Call this if they pick EVA (No)
    public void OpenEvaPath()
    {
        // EVA opens a shortcut that skips the fight, but misses the weapon.
        if (evaShortcutDoor != null) evaShortcutDoor.SetActive(false); // Opens the shortcut
        if (aresBossDoor != null) aresBossDoor.SetActive(true); // Locks the boss out

        // Record the choice in our memory bank
        StoryManager.Instance.SetChoice1Eva();
    }
}
using UnityEngine;

public enum FactionChoice { None, Ares, Eva, Neither }

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    // These match your design document exactly
    public FactionChoice choice1 = FactionChoice.None;
    public FactionChoice choice2 = FactionChoice.None;
    public FactionChoice finalChoice = FactionChoice.None;

    void Awake()
    {
        // If this is the very first StoryManager in the game...
        if (Instance == null)
        {
            Instance = this;

            // It tells Unity: "When you load Level 2, DO NOT delete this object!"
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If we load into a scene that already has a duplicate StoryManager, 
            // destroy the duplicate so we don't overwrite our saved memory.
            Destroy(gameObject);
        }
    }

    // Call these from UnityEvents on your DialogueTriggers!
    public void SetChoice1Ares() { choice1 = FactionChoice.Ares; }
    public void SetChoice1Eva() { choice1 = FactionChoice.Eva; }

    public void SetChoice2Ares() { choice2 = FactionChoice.Ares; }
    public void SetChoice2Eva() { choice2 = FactionChoice.Eva; }
}
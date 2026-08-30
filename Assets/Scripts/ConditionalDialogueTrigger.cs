using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ConditionalDialogueTrigger : MonoBehaviour
{
    [Header("Level 2 Reaction Branch")]
    public DialogueSequence aresReactionSequence;
    public DialogueSequence evaReactionSequence;
    public DialogueSequence defaultSequence; // Fallback

    private bool hasFired = false;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasFired || !other.CompareTag("Player")) return;
        hasFired = true;

        // Check the memory bank to see what the player did in Level 1!
        if (StoryManager.Instance.choice1 == FactionChoice.Ares)
        {
            DialogueManager.Instance.StartDialogue(aresReactionSequence);
        }
        else if (StoryManager.Instance.choice1 == FactionChoice.Eva)
        {
            DialogueManager.Instance.StartDialogue(evaReactionSequence);
        }
        else
        {
            DialogueManager.Instance.StartDialogue(defaultSequence);
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level3DialogueTrigger : MonoBehaviour
{
    [Header("The 4 Combinations")]
    public DialogueSequence consistentAres; // ARES -> ARES
    public DialogueSequence consistentEva;  // EVA -> EVA
    public DialogueSequence switchedToEva;  // ARES -> EVA
    public DialogueSequence switchedToAres; // EVA -> ARES

    private bool hasFired = false;

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasFired || !other.CompareTag("Player")) return;
        if (StoryManager.Instance == null) return;

        hasFired = true;
        FactionChoice c1 = StoryManager.Instance.choice1;
        FactionChoice c2 = StoryManager.Instance.choice2;

        if (c1 == FactionChoice.Ares && c2 == FactionChoice.Ares)
            DialogueManager.Instance.StartDialogue(consistentAres);
        else if (c1 == FactionChoice.Eva && c2 == FactionChoice.Eva)
            DialogueManager.Instance.StartDialogue(consistentEva);
        else if (c1 == FactionChoice.Ares && c2 == FactionChoice.Eva)
            DialogueManager.Instance.StartDialogue(switchedToEva);
        else if (c1 == FactionChoice.Eva && c2 == FactionChoice.Ares)
            DialogueManager.Instance.StartDialogue(switchedToAres);
    }
}
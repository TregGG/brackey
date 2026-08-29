using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Setup")]
    public DialogueSequence dialogue;

    [Tooltip("If true, this can only ever fire once (e.g. a one-time story beat). If false, it can be replayed (e.g. a sign or a repeatable NPC).")]
    public bool triggerOnce = true;

    [Header("Trigger Zone")]
    [Tooltip("If true, walking into this object's trigger collider starts the dialogue automatically. Turn off if you only want to start it from code via TriggerDialogue().")]
    public bool startOnTriggerEnter = true;

    [Header("Outcome Events")]
    [Tooltip("Fires if the dialogue has a Yes/No choice and the player picked Yes.")]
    public UnityEvent onChoiceYes;
    [Tooltip("Fires if the dialogue has a Yes/No choice and the player picked No.")]
    public UnityEvent onChoiceNo;
    [Tooltip("Fires when the dialogue box closes, regardless of whether it had a choice.")]
    public UnityEvent onDialogueEnd;

    private bool hasFired = false;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!startOnTriggerEnter) return;
        if (!other.CompareTag("Player")) return;

        TriggerDialogue();
    }

    // --- Call this directly from code (an interact-key script, a cutscene manager,
    // another UnityEvent, etc.) as an alternative to walking into the trigger zone.
    public void TriggerDialogue()
    {
        if (dialogue == null) return;
        if (triggerOnce && hasFired) return;
        if (DialogueManager.Instance == null) return;

        hasFired = true;
        DialogueManager.Instance.StartDialogue(dialogue, OnDialogueFinished);
    }

    private void OnDialogueFinished(bool yesChosen)
    {
        if (dialogue.hasChoice)
        {
            if (yesChosen) onChoiceYes?.Invoke();
            else onChoiceNo?.Invoke();
        }

        onDialogueEnd?.Invoke();
    }
}

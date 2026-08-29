using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    [Header("Lines")]
    public List<DialogueLine> lines;

    [Header("Yes/No Prompt (Optional)")]
    [Tooltip("If checked, a Yes/No prompt appears once the last line has been dismissed.")]
    public bool hasChoice;
    [TextArea(1, 3)]
    public string choicePrompt = "Well?";
    public string yesLabel = "Yes";
    public string noLabel = "No";
}

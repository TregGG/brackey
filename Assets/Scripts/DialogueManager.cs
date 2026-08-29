using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue Box UI")]
    public GameObject dialogueBoxRoot;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;
    [Tooltip("A small bouncing arrow or similar, shown once a line has fully finished typing and is waiting for input.")]
    public GameObject continueIndicator;

    [Header("Yes/No Choice UI")]
    public GameObject choiceBoxRoot;
    public TextMeshProUGUI choicePromptText;
    public TextMeshProUGUI yesOptionText;
    public TextMeshProUGUI noOptionText;
    public Color choiceSelectedColor = Color.yellow;
    public Color choiceUnselectedColor = Color.white;

    [Header("Typewriter")]
    public float charsPerSecond = 40f;
    public AudioClip typeBlipSFX;
    [Range(1, 5)]
    public int blipEveryNChars = 2;

    private AudioSource audioSource;

    private DialogueSequence currentSequence;
    private int currentLineIndex;
    private bool isTyping;
    private bool lineFullyShown;
    private bool isChoiceActive;
    private bool choiceSelectionIsYes = true;
    private System.Action<bool> onDialogueComplete;
    private Coroutine typeCoroutine;

    // Cached player references so we can freeze/unfreeze control while talking
    private PlayerMovement playerMovement;
    private WeaponHolder playerWeapons;
    private PlayerAim playerAim;
    private Rigidbody2D playerRb;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerWeapons = player.GetComponent<WeaponHolder>();
            playerAim = player.GetComponent<PlayerAim>();
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        if (dialogueBoxRoot != null) dialogueBoxRoot.SetActive(false);
        if (choiceBoxRoot != null) choiceBoxRoot.SetActive(false);
    }

    void Update()
    {
        if (currentSequence == null) return; // No active dialogue - nothing to poll for.

        if (isChoiceActive)
        {
            HandleChoiceInput();
            return;
        }

        if (Keyboard.current == null && Mouse.current == null) return;

        bool advancePressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (!advancePressed) return;

        if (isTyping)
        {
            CompleteLineInstantly();
        }
        else if (lineFullyShown)
        {
            AdvanceLine();
        }
    }

    // --- Call this from anywhere: a DialogueTrigger, an interact-key script, or a cutscene.
    // onComplete(bool) fires once the box closes: for a sequence with hasChoice, the bool is
    // the player's answer (true = Yes); otherwise it's just called with true.
    public void StartDialogue(DialogueSequence sequence, System.Action<bool> onComplete = null)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Count == 0) return;
        if (currentSequence != null) return; // Ignore overlapping dialogue requests.
        if (bodyText == null || dialogueBoxRoot == null)
        {
            Debug.LogWarning("DialogueManager is missing required UI references (dialogueBoxRoot/bodyText).");
            return;
        }

        currentSequence = sequence;
        currentLineIndex = 0;
        onDialogueComplete = onComplete;

        SetPlayerControlLocked(true);

        dialogueBoxRoot.SetActive(true);
        if (choiceBoxRoot != null) choiceBoxRoot.SetActive(false);

        ShowLine(currentSequence.lines[0]);
    }

    // --- Convenience overload for Inspector-wired UnityEvents (e.g. DialogueTrigger's
    // onChoiceYes/onChoiceNo). A UnityEvent can carry a single serialized argument like a
    // DialogueSequence, but can't carry a C# delegate, so this is what you wire a follow-up
    // dialogue to directly in the Inspector for simple branching with no extra scripting.
    // Call StartDialogue(sequence, callback) from code instead if the follow-up itself needs
    // to report a result back (e.g. it has its own further choice to react to).
    public void StartDialogueFromEvent(DialogueSequence sequence)
    {
        StartDialogue(sequence, null);
    }

    private void ShowLine(DialogueLine line)
    {
        if (nameText != null) nameText.text = line.speakerName;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        typeCoroutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string fullText)
    {
        isTyping = true;
        lineFullyShown = false;
        bodyText.text = "";

        int charCount = 0;
        foreach (char c in fullText)
        {
            bodyText.text += c;
            charCount++;

            if (typeBlipSFX != null && !char.IsWhiteSpace(c) && charCount % blipEveryNChars == 0)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(typeBlipSFX);
            }

            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        FinishTyping();
    }

    private void CompleteLineInstantly()
    {
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        bodyText.text = currentSequence.lines[currentLineIndex].text;
        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        lineFullyShown = true;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void AdvanceLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentSequence.lines.Count)
        {
            ShowLine(currentSequence.lines[currentLineIndex]);
        }
        else if (currentSequence.hasChoice)
        {
            ShowChoicePrompt();
        }
        else
        {
            EndDialogue(true);
        }
    }

    private void ShowChoicePrompt()
    {
        isChoiceActive = true;
        choiceSelectionIsYes = true;

        dialogueBoxRoot.SetActive(false);
        if (choiceBoxRoot != null) choiceBoxRoot.SetActive(true);
        if (choicePromptText != null) choicePromptText.text = currentSequence.choicePrompt;
        if (yesOptionText != null) yesOptionText.text = currentSequence.yesLabel;
        if (noOptionText != null) noOptionText.text = currentSequence.noLabel;

        UpdateChoiceSelectorVisual();
    }

    private void UpdateChoiceSelectorVisual()
    {
        // Simple highlight-by-color approach. If you want a moving arrow/cursor instead,
        // swap this for repositioning a selector RectTransform next to whichever option
        // is currently selected.
        if (yesOptionText != null) yesOptionText.color = choiceSelectionIsYes ? choiceSelectedColor : choiceUnselectedColor;
        if (noOptionText != null) noOptionText.color = !choiceSelectionIsYes ? choiceSelectedColor : choiceUnselectedColor;
    }

    private void HandleChoiceInput()
    {
        if (Keyboard.current == null) return;

        bool navigatePressed = Keyboard.current.upArrowKey.wasPressedThisFrame
                             || Keyboard.current.downArrowKey.wasPressedThisFrame
                             || Keyboard.current.wKey.wasPressedThisFrame
                             || Keyboard.current.sKey.wasPressedThisFrame;

        if (navigatePressed)
        {
            choiceSelectionIsYes = !choiceSelectionIsYes;
            UpdateChoiceSelectorVisual();
        }

        bool confirmPressed = Keyboard.current.eKey.wasPressedThisFrame
                            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (confirmPressed)
        {
            bool result = choiceSelectionIsYes;
            isChoiceActive = false;
            if (choiceBoxRoot != null) choiceBoxRoot.SetActive(false);

            EndDialogue(result);
        }
    }

    private void EndDialogue(bool result)
    {
        if (dialogueBoxRoot != null) dialogueBoxRoot.SetActive(false);
        if (choiceBoxRoot != null) choiceBoxRoot.SetActive(false);

        currentSequence = null;
        SetPlayerControlLocked(false);

        System.Action<bool> callback = onDialogueComplete;
        onDialogueComplete = null;
        callback?.Invoke(result);
    }

    private void SetPlayerControlLocked(bool locked)
    {
        if (playerMovement != null) playerMovement.enabled = !locked;
        if (playerWeapons != null) playerWeapons.enabled = !locked;
        if (playerAim != null) playerAim.enabled = !locked;

        if (locked && playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }
}
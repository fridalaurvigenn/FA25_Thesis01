using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject choiceUI;

    [Header("Choices")]
    [SerializeField] private Button[] choices; //Buttons for all possible choices
    private TextMeshProUGUI[] choiceTexts; //Text components for each button

    [Header("Journal Unlock")]
    [SerializeField] private bool unlocksEliseEntry = false;
    [SerializeField] private JournalUIController journalUI;
    [SerializeField] private GameObject eliseReflectionPage;

    [Header("Dialogue Bubbles")]
    [SerializeField] private GameObject npcBubble;
    [SerializeField] private TextMeshProUGUI npcText;

    [SerializeField] private GameObject playerBubble;
    [SerializeField] private TextMeshProUGUI playerText;

    [SerializeField] private GameObject narratorBubble;
    [SerializeField] private TextMeshProUGUI narratorText;

    public Story currentStory;
    public bool dialogueIsPlaying { get; private set; }

    private bool eliseReflectionUnlocked = false;

    public static DialogueManager instance;
    private int selectedChoiceIndex = 0; //For tracking navigation

    [SerializeField] private TextAsset globalsJSON;
    private InkDialogueVariables globals;

    // after picking a choice, we briefly show the player's line
    // in the player bubble and wait for Space/Enter before continuing.
    private bool waitingForAdvanceAfterChoice = false;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in scene!");
        }
        instance = this;

        //Initialize choiceTexts based on the number of buttons
        choiceTexts = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choiceTexts[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();
            if (choiceTexts[i] == null)
            {
                Debug.LogError($"No TextMeshProUGUI found in choice button {choices[i].name}");
            }
        }
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        choiceUI.SetActive(false);

        //Build the globals bridge from compiled globals.ink.json
        globals = new InkDialogueVariables(globalsJSON);
    }

    private void Update()
    {
        if (!dialogueIsPlaying) return;

        // If we've just picked a choice and are showing the player's line,
        // wait for Space/Enter before continuing the story.
        if (waitingForAdvanceAfterChoice)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                waitingForAdvanceAfterChoice = false;
                ContinueStory(); // now show Elise / narrator / next line
            }
            return; // don't process normal choice navigation while waiting
        }

        // Scroll or arrows to navigate choices
        if (currentStory.currentChoices.Count > 0)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll > 0f || Input.GetKeyDown(KeyCode.UpArrow))
            {
                selectedChoiceIndex = Mathf.Max(0, selectedChoiceIndex - 1);
                HighlightChoice();
            }
            else if (scroll < 0f || Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedChoiceIndex = Mathf.Min(currentStory.currentChoices.Count - 1, currentStory.currentChoices.Count - 1);
                HighlightChoice();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnChoiceSelected(selectedChoiceIndex);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().FreezeMovement(true);
        currentStory = new Story(inkJSON.text);

        // Inject globals so VARs persist
        globals.StartListening(currentStory);

        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        waitingForAdvanceAfterChoice = false;

        // Hide all bubbles when starting
        if (npcBubble != null) npcBubble.SetActive(false);
        if (playerBubble != null) playerBubble.SetActive(false);
        if (narratorBubble != null) narratorBubble.SetActive(false);

        ContinueStory();
    }

    public void ExitDialogueMode()
    {
        // Stop listening so bridge stores changed globals
        if (currentStory != null && globals != null)
            globals.StopListening(currentStory);

        // Clear bubbles
        if (npcBubble != null) npcBubble.SetActive(false);
        if (playerBubble != null) playerBubble.SetActive(false);
        if (narratorBubble != null) narratorBubble.SetActive(false);

        // Fully release UI focus so the I key works again
        foreach (var b in choices) b.onClick.RemoveAllListeners();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().FreezeMovement(false);
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        choiceUI.SetActive(false);

        // Unlock Elise journal entry:
        // - only for this dialogue (unlocksEliseEntry)
        // - only after greeted_elise is true in Ink
        // - only once per save (eliseReflectionUnlocked)
        if (unlocksEliseEntry && !eliseReflectionUnlocked && journalUI != null && eliseReflectionPage != null)
        {
            eliseReflectionUnlocked = true;

            journalUI.NotifyNewEntry(
                eliseReflectionPage,
                "Press [Tab] to open Journal - New Entry!",
                2   // reflections tab index
            );
        }
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string rawText = currentStory.Continue();

            // Pipe the line to the bubble logic
            DisplayStyledLine(rawText);

            DisplayChoices();
        }
        else
        {
            ExitDialogueMode();
        }
    }

    /// <summary>
    /// Takes the raw line from Ink, formats it, and decides whether
    /// to show it in the Narrator, NPC, or Player bubble.
    /// Any line that starts with "Narrated:" / "Narrator:" goes to narrator bubble.
    /// Any line that starts with "NAME:" (e.g. "Elise:") is treated as NPC.
    /// All other lines are treated as player spoken lines.
    /// </summary>
    private void DisplayStyledLine(string rawText)
    {
        string formatted = ConvertFormatting(rawText);

        // Hide all bubbles and clear text first
        if (npcBubble != null) npcBubble.SetActive(false);
        if (playerBubble != null) playerBubble.SetActive(false);
        if (narratorBubble != null) narratorBubble.SetActive(false);

        if (npcText != null) npcText.text = "";
        if (playerText != null) playerText.text = "";
        if (narratorText != null) narratorText.text = "";

        if (string.IsNullOrWhiteSpace(formatted))
        {
            if (dialogueText != null) dialogueText.text = "";
            return;
        }

        int colonIndex = formatted.IndexOf(':');
        string speaker = "";
        string content = formatted;

        if (colonIndex > 0)
        {
            speaker = formatted.Substring(0, colonIndex).Trim();
            content = formatted.Substring(colonIndex + 1).TrimStart();
        }

        // Optional debug label
        if (dialogueText != null)
            dialogueText.text = content;

        // --- Narrated / inner monologue ---
        string lowerSpeaker = speaker.ToLowerInvariant();
        if (lowerSpeaker == "narrated" || lowerSpeaker == "narrator")
        {
            if (narratorBubble != null) narratorBubble.SetActive(true);
            if (narratorText != null) narratorText.text = content;

            // Make sure other bubbles are off
            if (npcBubble != null) npcBubble.SetActive(false);
            if (playerBubble != null) playerBubble.SetActive(false);
            if (npcText != null) npcText.text = "";

            return;
        }

        // --- NPC line (anything with a NAME: prefix, like "Elise:") ---
        if (!string.IsNullOrEmpty(speaker))
        {
            if (npcBubble != null) npcBubble.SetActive(true);
            if (npcText != null) npcText.text = content;

            // Make sure other bubbles are off

            if (playerBubble != null) playerBubble.SetActive(false);
            if (narratorBubble != null) narratorBubble.SetActive(false);
        }
        else
        {
            // --- Player spoken line (no prefix) ---
            if (playerBubble != null) playerBubble.SetActive(true);
            if (playerText != null) playerText.text = content;

            // Make sure other bubbles are off
            if (npcBubble != null) npcBubble.SetActive(false);
            if (npcText != null) npcText.text = "";
            if (narratorBubble != null) narratorBubble.SetActive(false);
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > 0)
        {
            choiceUI.SetActive(true);
            selectedChoiceIndex = 0;

            for (int i = 0; i < choices.Length; i++)
            {
                if (i < currentChoices.Count)
                {
                    choices[i].gameObject.SetActive(true);
                    choiceTexts[i].text = currentChoices[i].text;
                    int choiceIndex = i;
                    choices[i].onClick.RemoveAllListeners();
                    choices[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }
                else
                {
                    choices[i].gameObject.SetActive(false);
                    if (EventSystem.current != null)
                        EventSystem.current.SetSelectedGameObject(null); // drop focus
                }
            }

            HighlightChoice();
        }
        else
        {
            choiceUI.SetActive(false);
        }
    }

    private void HighlightChoice()
    {
        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            string prefix = (i == selectedChoiceIndex) ? "> " : "";
            choiceTexts[i].text = prefix + currentStory.currentChoices[i].text;
        }

        if (EventSystem.current != null && choices.Length > 0)
            EventSystem.current.SetSelectedGameObject(choices[selectedChoiceIndex].gameObject);
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        // Grab the text of the chosen option before we advance Ink
        string chosenText = currentStory.currentChoices[choiceIndex].text;

        // Show it in the player bubble
        if (npcBubble != null) npcBubble.SetActive(false);
        if (narratorBubble != null) narratorBubble.SetActive(false);
        if (playerBubble != null) playerBubble.SetActive(true);
        if (playerText != null) playerText.text = chosenText;

        // Tell Ink which choice was picked
        currentStory.ChooseChoiceIndex(choiceIndex);

        // Hide the choice UI
        choiceUI.SetActive(false);

        // Now wait for the next Space/Enter before advancing story
        waitingForAdvanceAfterChoice = true;
    }

    private string ConvertFormatting(string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("*")) continue;
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"\[\[(.*?)\]\]", "<b>$1</b>");
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"\[(.*?)\]", "<i>$1</i>");
        }
        return string.Join("\n", lines);
    }
}

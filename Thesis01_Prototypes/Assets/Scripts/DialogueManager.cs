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

    [Header("Journal Unlock (optional)")]
    [SerializeField] private bool unlocksEliseEntry = false;
    [SerializeField] private JournalUIController journalUI;
    [SerializeField] private GameObject eliseReflectionPage;

    public Story currentStory;
    public bool dialogueIsPlaying { get; private set; }

    public static DialogueManager instance;
    private int selectedChoiceIndex = 0; //For tracking navigation

    [SerializeField] private TextAsset globalsJSON;
    private InkDialogueVariables globals;

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

        //Scroll or arrows to navigate choices
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
                selectedChoiceIndex = Mathf.Min(currentStory.currentChoices.Count - 1, selectedChoiceIndex + 1);
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

        //Inject globals so VARs persists
        globals.StartListening(currentStory);

        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        ContinueStory();
    }

    public void ExitDialogueMode()
    {
        //Stop listening so bridge stores changed globals
        if (currentStory != null && globals != null)
            globals.StopListening(currentStory);

        Debug.Log($"[Ink] greeted_elise on open = {currentStory.variablesState["greeted_elise"]}");
        //Fully release UI focus so the I key works again
        foreach (var b in choices) b.onClick.RemoveAllListeners();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().FreezeMovement(false);
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
        choiceUI.SetActive(false);

        // Unlock Elise journal entry (only once, handled inside the journal)
        if (unlocksEliseEntry && journalUI != null)
        {
            // Make sure the Elise page is active so it can be seen in the Reflections tab
            if (eliseReflectionPage != null)
                eliseReflectionPage.SetActive(true);

            journalUI.UnlockEliseReflection();
        }
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string rawText = currentStory.Continue();
            dialogueText.text = ConvertFormatting(rawText);
            DisplayChoices();
        }
        else
        {
            ExitDialogueMode();
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
                        EventSystem.current.SetSelectedGameObject(null); //drop focus
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

        EventSystem.current.SetSelectedGameObject(choices[selectedChoiceIndex].gameObject);
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        choiceUI.SetActive(false);
        ContinueStory();
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

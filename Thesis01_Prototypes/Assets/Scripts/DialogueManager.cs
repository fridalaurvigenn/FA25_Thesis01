using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    public TextMeshProUGUI dialogueText;
    private Story currentStory;
    private bool dialogueIsPlaying = false;
    private bool showingChoices = false;

    private int selectedChoiceIndex = 0;

    void Awake()
    {
        instance = this;
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialogueText.text = "";
        ContinueStory();
    }

    void Update()
    {
        if (!dialogueIsPlaying) return;

        if (showingChoices)
        {
            HandleChoiceInput();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ContinueStory();
        }
    }

    void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();
            dialogueText.text = text;
        }
        else if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoicesInline();
        }
        else
        {
            ExitDialogueMode();
        }
    }

    void DisplayChoicesInline()
    {
        showingChoices = true;
        selectedChoiceIndex = 0;

        string combinedText = "";
        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            string prefix = (i == selectedChoiceIndex) ? "> " : "  ";
            combinedText += prefix + currentStory.currentChoices[i].text + "\n";
        }

        dialogueText.text = combinedText;
    }

    void HandleChoiceInput()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0f || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedChoiceIndex = Mathf.Max(0, selectedChoiceIndex - 1);
            DisplayChoicesInline();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedChoiceIndex = Mathf.Min(currentStory.currentChoices.Count - 1, selectedChoiceIndex + 1);
            DisplayChoicesInline();
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            currentStory.ChooseChoiceIndex(selectedChoiceIndex);
            showingChoices = false;
            ContinueStory();
        }
    }

    void ExitDialogueMode()
    {
        dialogueText.text = "";
        dialogueIsPlaying = false;
        showingChoices = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

public class NPCInteractor : MonoBehaviour
{
    public GameObject promptUI;
    public GameObject dialogueUI;
    private bool isPlayerInRange = false;
    public TextAsset inkJSON;

    void Update()
    {
        //Trigger dialogue
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.I))
        {
            dialogueUI.SetActive(true);
            DialogueManager.instance.EnterDialogueMode(inkJSON);
            promptUI.SetActive(false);
        }

        //Keep prompt active while in range, unless dialogue is open
        if (isPlayerInRange && !DialogueManager.instance.dialogueIsPlaying)
        {
            promptUI.SetActive(true);
        }
        else if (!isPlayerInRange || DialogueManager.instance.dialogueIsPlaying)
        {
            promptUI.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger zone");
            isPlayerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            promptUI.SetActive(false); //Always hide prompt when player leaves
        }
    }
}

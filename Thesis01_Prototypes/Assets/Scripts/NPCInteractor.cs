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
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.I))
        {
            dialogueUI.SetActive(true);
            DialogueManager.instance.EnterDialogueMode(inkJSON);
            promptUI.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger zone");
            isPlayerInRange = true;
            promptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            promptUI.SetActive(false);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestorationRitual : MonoBehaviour
{
    public SpriteCrossfade crossfadeScript;
    public BunnyMovement bunnyScript;
    public TextMeshProUGUI promptText;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Press E to perform ritual";

            if (Input.GetKeyDown(KeyCode.E))
            {
                crossfadeScript.StartFade();
                bunnyScript.StartMoving();
                promptText.gameObject.SetActive(false);
            }
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestorationRitual : MonoBehaviour
{
    [Header("Linked Scripts")]
    public SpriteCrossfade crossfadeScript;
    public BunnyMovement bunnyScript;

    [Header("UI Elements")]
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI hintText;
    public CanvasGroup spellbookCanvas;
    public Image pageImage;

    [Header("Settings")]
    public float holdDuration = 5f;

    private bool playerInRange = false;
    private bool hasPerformedRitual = false;
    private bool isCasting = false;
    private bool spellbookOpen = false;
    private float holdTimer = 0f;

    private Color startColor = new Color32(231, 226, 217, 255); //#E7E2D9
    private Color endColor = Color.white;

    void Update()
    {
        if (playerInRange && !hasPerformedRitual)
        {
            //Show "Press U" hint
            if (!spellbookOpen && !isCasting)
            {
                hintText.gameObject.SetActive(true);
                hintText.text = "Press U to take out your spell book";
            }
            else
            {
                hintText.gameObject.SetActive(false);
            }

            //Toggle spellbook with U
            if (Input.GetKeyDown(KeyCode.U) && !spellbookOpen && !isCasting)
            {
                ToggleSpellbook(true);
            }

            //Show casting prompt
            if (spellbookOpen && !isCasting)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "Hold E to focus your energy...";
            }
            else
            {
                promptText.gameObject.SetActive(false);
            }

            //Start casting when E is held
            if (spellbookOpen && Input.GetKey(KeyCode.E))
            {
                if (!isCasting)
                {
                    isCasting = true;
                    holdTimer = 0f;
                    pageImage.color = startColor;
                }

                holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(holdTimer / holdDuration);
                pageImage.color = Color.Lerp(startColor, endColor, progress);

                if (holdTimer >= holdDuration)
                {
                    CompleteRitual();
                }
            }

            //Cancel ritual if E released early
            if (Input.GetKeyUp(KeyCode.E) && isCasting)
            {
                CancelRitual();
            }
        }
        else
        {
            //Close book and hide UI if player leaves or finishes ritual
            if (spellbookOpen)
            {
                ToggleSpellbook(false);
            }

            promptText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);
        }
    }

    void ToggleSpellbook(bool open)
    {
        spellbookOpen = open;
        spellbookCanvas.alpha = open ? 1 : 0;
        spellbookCanvas.blocksRaycasts = open;
    }

    void CompleteRitual()
    {
        hasPerformedRitual = true;
        isCasting = false;
        ToggleSpellbook(false);

        crossfadeScript.StartFade();
        bunnyScript.StartMoving();
    }

    void CancelRitual()
    {
        isCasting = false;
        holdTimer = 0f;
        pageImage.color = startColor;
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

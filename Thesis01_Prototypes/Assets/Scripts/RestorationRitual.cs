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
    public RectTransform focusArea;
    public RectTransform focusLight;

    [Header("Ritual Settings")]
    public float holdDuration = 6f;
    public float focusLossSpeed = 0.5f;
    public float minLightScale = 0.5f;
    public float maxLightScale = 1.4f;

    [Header("Light Behaviour")]
    public float lightHoldThreshold = 0.6f;
    public float fadeDuration = 0.2f;
    public float edgePadding = 20f;
    public float minReappearDistance = 80f;

    [Header("Light Pulse")]
    public float pulseSpeed = 2.5f;
    public float minPulseAlpha = 0.5f;
    public float maxPulseAlpha = 1f;

    [Header("UI Fade")]
    public float textFadeDuration = 0.25f;
    public float spellbookFadeDuration = 0.3f;

    private bool playerInRange = false;
    private bool hasPerformedRitual = false;
    private bool isCasting = false;
    private bool spellbookOpen = false;
    private bool isTransitioningLight = false;
    private bool isCancelling = false;

    private float holdTimer = 0f;
    private float currentLightHoldTimer = 0f;

    private Coroutine promptFadeRoutine;
    private Coroutine hintFadeRoutine;
    private Coroutine spellbookFadeRoutine;
    private Coroutine lightFadeRoutine;
    private Coroutine lightRepositionRoutine;
    private Coroutine cancelRoutine;

    private string currentPromptText = "";
    private string currentHintText = "";

    void Start()
    {
        if (focusLight != null)
        {
            focusLight.gameObject.SetActive(false);
            focusLight.localScale = Vector3.one * minLightScale;
            SetLightAlpha(minPulseAlpha);
        }

        if (spellbookCanvas != null)
        {
            spellbookCanvas.alpha = 0f;
            spellbookCanvas.blocksRaycasts = false;
            spellbookCanvas.gameObject.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            SetTextAlpha(promptText, 1f);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
            SetTextAlpha(hintText, 1f);
        }
    }

    void Update()
    {
        if (playerInRange && !hasPerformedRitual)
        {
            HandleHints();
            HandleSpellbookToggle();
            HandleCasting();
        }
        else
        {
            CloseEverythingImmediate();
        }
    }

    void HandleHints()
    {
        if (!spellbookOpen)
        {
            SetHintText("Press U to take out your spell book");
        }
        else
        {
            HideHintText();
        }

        if (spellbookOpen && !isCasting)
        {
            SetPromptText("Hold E to focus your energy...");
        }
        else if (spellbookOpen && isCasting)
        {
            SetPromptText("Stay with the light...");
        }
        else
        {
            HidePromptText();
        }
    }

    void HandleSpellbookToggle()
    {
        if (Input.GetKeyDown(KeyCode.U) && !spellbookOpen && !isCancelling)
        {
            ToggleSpellbook(true);
        }
    }

    void HandleCasting()
    {
        if (spellbookOpen && Input.GetKey(KeyCode.E) && !isCancelling)
        {
            if (!isCasting)
            {
                StartCasting();
            }

            UpdateCasting();
        }

        if (Input.GetKeyUp(KeyCode.E) && isCasting && !isCancelling)
        {
            CancelRitual();
        }
    }

    void StartCasting()
    {
        isCasting = true;
        holdTimer = 0f;
        currentLightHoldTimer = 0f;
        isTransitioningLight = false;

        if (focusLight != null)
        {
            focusLight.gameObject.SetActive(true);
            focusLight.localScale = Vector3.one * minLightScale;

            MoveFocusLightFarEnough();
            SetLightAlpha(0f);

            if (lightFadeRoutine != null)
            {
                StopCoroutine(lightFadeRoutine);
            }

            lightFadeRoutine = StartCoroutine(FadeLight(0f, minPulseAlpha, fadeDuration));
        }
    }

    void UpdateCasting()
    {
        if (focusLight == null || focusArea == null || isTransitioningLight || isCancelling) return;

        bool isHoveringLight = RectTransformUtility.RectangleContainsScreenPoint(
            focusLight,
            Input.mousePosition,
            null
        );

        if (isHoveringLight)
        {
            holdTimer += Time.deltaTime;
            currentLightHoldTimer += Time.deltaTime;
        }
        else
        {
            holdTimer -= Time.deltaTime * focusLossSpeed;
            currentLightHoldTimer = 0f;
        }

        holdTimer = Mathf.Clamp(holdTimer, 0f, holdDuration);

        float progress = Mathf.Clamp01(holdTimer / holdDuration);

        float scale = Mathf.Lerp(minLightScale, maxLightScale, progress);
        focusLight.localScale = Vector3.one * scale;

        if (!isHoveringLight)
        {
            PulseLight(progress);
        }

        if (currentLightHoldTimer >= lightHoldThreshold)
        {
            currentLightHoldTimer = 0f;

            if (lightRepositionRoutine != null)
            {
                StopCoroutine(lightRepositionRoutine);
            }

            lightRepositionRoutine = StartCoroutine(RepositionLight());
        }

        if (holdTimer >= holdDuration)
        {
            CompleteRitual();
        }
    }

    void PulseLight(float progress)
    {
        Image lightImage = focusLight.GetComponent<Image>();
        if (lightImage != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float baseAlpha = Mathf.Lerp(minPulseAlpha, maxPulseAlpha, pulse);
            float progressBoost = Mathf.Lerp(0f, 0.1f, progress);
            float finalAlpha = Mathf.Clamp01(baseAlpha + progressBoost);

            Color c = lightImage.color;
            lightImage.color = new Color(c.r, c.g, c.b, finalAlpha);
        }
    }

    void SetLightAlpha(float alpha)
    {
        Image lightImage = focusLight.GetComponent<Image>();
        if (lightImage != null)
        {
            Color c = lightImage.color;
            lightImage.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    float GetCurrentAlpha()
    {
        Image lightImage = focusLight.GetComponent<Image>();
        if (lightImage != null)
        {
            return lightImage.color.a;
        }

        return 1f;
    }

    IEnumerator FadeLight(float startAlpha, float endAlpha, float duration)
    {
        Image lightImage = focusLight.GetComponent<Image>();
        if (lightImage == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            Color c = lightImage.color;
            lightImage.color = new Color(c.r, c.g, c.b, alpha);

            yield return null;
        }

        Color finalColor = lightImage.color;
        lightImage.color = new Color(finalColor.r, finalColor.g, finalColor.b, endAlpha);
    }

    IEnumerator RepositionLight()
    {
        if (focusLight == null) yield break;

        isTransitioningLight = true;

        if (lightFadeRoutine != null)
        {
            StopCoroutine(lightFadeRoutine);
        }

        lightFadeRoutine = StartCoroutine(FadeLight(GetCurrentAlpha(), 0f, fadeDuration));
        yield return lightFadeRoutine;

        MoveFocusLightFarEnough();
        SetLightAlpha(0f);

        lightFadeRoutine = StartCoroutine(FadeLight(0f, minPulseAlpha, fadeDuration));
        yield return lightFadeRoutine;

        isTransitioningLight = false;
        lightRepositionRoutine = null;
        lightFadeRoutine = null;
    }

    IEnumerator FadeOutAndDisableLight()
    {
        if (focusLight == null) yield break;

        isTransitioningLight = true;

        if (lightRepositionRoutine != null)
        {
            StopCoroutine(lightRepositionRoutine);
            lightRepositionRoutine = null;
        }

        if (lightFadeRoutine != null)
        {
            StopCoroutine(lightFadeRoutine);
        }

        lightFadeRoutine = StartCoroutine(FadeLight(GetCurrentAlpha(), 0f, fadeDuration));
        yield return lightFadeRoutine;

        focusLight.gameObject.SetActive(false);
        focusLight.localScale = Vector3.one * minLightScale;
        SetLightAlpha(minPulseAlpha);

        isTransitioningLight = false;
        lightFadeRoutine = null;
    }

    void MoveFocusLightFarEnough()
    {
        if (focusArea == null || focusLight == null) return;

        Vector2 previousPosition = focusLight.anchoredPosition;
        Vector2 newPosition = GetRandomPointInFocusArea();

        int attempts = 0;
        while (Vector2.Distance(newPosition, previousPosition) < minReappearDistance && attempts < 20)
        {
            newPosition = GetRandomPointInFocusArea();
            attempts++;
        }

        focusLight.anchoredPosition = newPosition;
    }

    Vector2 GetRandomPointInFocusArea()
    {
        float areaWidth = focusArea.rect.width;
        float areaHeight = focusArea.rect.height;

        float minX = -areaWidth / 2f + edgePadding;
        float maxX = areaWidth / 2f - edgePadding;
        float minY = -areaHeight / 2f + edgePadding;
        float maxY = areaHeight / 2f - edgePadding;

        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        return new Vector2(randomX, randomY);
    }

    void ToggleSpellbook(bool open)
    {
        spellbookOpen = open;

        if (spellbookCanvas == null) return;

        if (spellbookFadeRoutine != null)
        {
            StopCoroutine(spellbookFadeRoutine);
        }

        spellbookFadeRoutine = StartCoroutine(FadeSpellbook(open));
    }

    IEnumerator FadeSpellbook(bool open)
    {
        spellbookCanvas.gameObject.SetActive(true);
        spellbookCanvas.blocksRaycasts = false;

        float startAlpha = spellbookCanvas.alpha;
        float endAlpha = open ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < spellbookFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spellbookFadeDuration;
            spellbookCanvas.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        spellbookCanvas.alpha = endAlpha;
        spellbookCanvas.blocksRaycasts = open;

        if (!open)
        {
            spellbookCanvas.gameObject.SetActive(false);
        }

        spellbookFadeRoutine = null;
    }

    void SetPromptText(string newText)
    {
        if (promptText == null) return;
        if (currentPromptText == newText && promptText.gameObject.activeSelf) return;

        currentPromptText = newText;

        if (promptFadeRoutine != null)
        {
            StopCoroutine(promptFadeRoutine);
        }

        promptFadeRoutine = StartCoroutine(FadeTextChange(promptText, newText, textFadeDuration));
    }

    void HidePromptText()
    {
        if (promptText == null || !promptText.gameObject.activeSelf) return;

        currentPromptText = "";

        if (promptFadeRoutine != null)
        {
            StopCoroutine(promptFadeRoutine);
        }

        promptFadeRoutine = StartCoroutine(FadeTextOut(promptText, textFadeDuration));
    }

    void SetHintText(string newText)
    {
        if (hintText == null) return;
        if (currentHintText == newText && hintText.gameObject.activeSelf) return;

        currentHintText = newText;

        if (hintFadeRoutine != null)
        {
            StopCoroutine(hintFadeRoutine);
        }

        hintFadeRoutine = StartCoroutine(FadeTextChange(hintText, newText, textFadeDuration));
    }

    void HideHintText()
    {
        if (hintText == null || !hintText.gameObject.activeSelf) return;

        currentHintText = "";

        if (hintFadeRoutine != null)
        {
            StopCoroutine(hintFadeRoutine);
        }

        hintFadeRoutine = StartCoroutine(FadeTextOut(hintText, textFadeDuration));
    }

    IEnumerator FadeTextChange(TextMeshProUGUI textElement, string newText, float duration)
    {
        textElement.gameObject.SetActive(true);

        Color baseColor = textElement.color;

        if (!string.IsNullOrEmpty(textElement.text))
        {
            float elapsedOut = 0f;
            float startingAlpha = textElement.color.a;

            while (elapsedOut < duration)
            {
                elapsedOut += Time.deltaTime;
                float t = elapsedOut / duration;
                float alpha = Mathf.Lerp(startingAlpha, 0f, t);
                textElement.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        textElement.text = newText;

        float elapsedIn = 0f;
        while (elapsedIn < duration)
        {
            elapsedIn += Time.deltaTime;
            float t = elapsedIn / duration;
            float alpha = Mathf.Lerp(0f, 1f, t);
            textElement.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        textElement.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    IEnumerator FadeTextOut(TextMeshProUGUI textElement, float duration)
    {
        Color baseColor = textElement.color;
        float startAlpha = textElement.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            textElement.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        textElement.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        textElement.gameObject.SetActive(false);
        textElement.text = "";
    }

    void SetTextAlpha(TextMeshProUGUI textElement, float alpha)
    {
        if (textElement == null) return;

        Color c = textElement.color;
        textElement.color = new Color(c.r, c.g, c.b, alpha);
    }

    void CompleteRitual()
    {
        hasPerformedRitual = true;
        isCasting = false;
        isTransitioningLight = false;
        isCancelling = false;

        if (cancelRoutine != null)
        {
            StopCoroutine(cancelRoutine);
            cancelRoutine = null;
        }

        if (lightRepositionRoutine != null)
        {
            StopCoroutine(lightRepositionRoutine);
            lightRepositionRoutine = null;
        }

        if (lightFadeRoutine != null)
        {
            StopCoroutine(lightFadeRoutine);
            lightFadeRoutine = null;
        }

        if (focusLight != null)
        {
            focusLight.gameObject.SetActive(false);
        }

        HidePromptText();
        HideHintText();
        ToggleSpellbook(false);

        if (crossfadeScript != null)
        {
            crossfadeScript.StartFade();
        }

        if (bunnyScript != null)
        {
            bunnyScript.StartMoving();
        }
    }

    void CancelRitual()
    {
        if (isCancelling) return;

        isCasting = false;
        isCancelling = true;
        holdTimer = 0f;
        currentLightHoldTimer = 0f;

        if (cancelRoutine != null)
        {
            StopCoroutine(cancelRoutine);
        }

        cancelRoutine = StartCoroutine(CancelRitualRoutine());
    }

    IEnumerator CancelRitualRoutine()
    {
        if (lightFadeRoutine != null)
        {
            StopCoroutine(lightFadeRoutine);
            lightFadeRoutine = null;
        }

        if (lightRepositionRoutine != null)
        {
            StopCoroutine(lightRepositionRoutine);
            lightRepositionRoutine = null;
        }

        yield return StartCoroutine(FadeOutAndDisableLight());

        isTransitioningLight = false;
        isCancelling = false;
        cancelRoutine = null;
    }

    void CloseEverythingImmediate()
    {
        if (spellbookFadeRoutine != null)
        {
            StopCoroutine(spellbookFadeRoutine);
            spellbookFadeRoutine = null;
        }

        if (promptFadeRoutine != null)
        {
            StopCoroutine(promptFadeRoutine);
            promptFadeRoutine = null;
        }

        if (hintFadeRoutine != null)
        {
            StopCoroutine(hintFadeRoutine);
            hintFadeRoutine = null;
        }

        if (lightFadeRoutine != null)
        {
            StopCoroutine(lightFadeRoutine);
            lightFadeRoutine = null;
        }

        if (lightRepositionRoutine != null)
        {
            StopCoroutine(lightRepositionRoutine);
            lightRepositionRoutine = null;
        }

        if (cancelRoutine != null)
        {
            StopCoroutine(cancelRoutine);
            cancelRoutine = null;
        }

        spellbookOpen = false;
        isCasting = false;
        isTransitioningLight = false;
        isCancelling = false;
        holdTimer = 0f;
        currentLightHoldTimer = 0f;

        if (focusLight != null)
        {
            focusLight.gameObject.SetActive(false);
            focusLight.localScale = Vector3.one * minLightScale;
            SetLightAlpha(minPulseAlpha);
        }

        if (spellbookCanvas != null)
        {
            spellbookCanvas.alpha = 0f;
            spellbookCanvas.blocksRaycasts = false;
            spellbookCanvas.gameObject.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            promptText.text = "";
            SetTextAlpha(promptText, 1f);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
            hintText.text = "";
            SetTextAlpha(hintText, 1f);
        }

        currentPromptText = "";
        currentHintText = "";
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

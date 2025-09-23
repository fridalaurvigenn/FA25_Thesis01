using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteCrossfade : MonoBehaviour
{
    public SpriteRenderer beforeRenderer;
    public SpriteRenderer afterRenderer;
    public float fadeDuration = 2f;

    private bool isFading = false;

    public void StartFade()
    {
        if (!isFading)
        {
            StartCoroutine(FadeSprites());
        }
    }

    IEnumerator FadeSprites()
    {
        isFading = true;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            beforeRenderer.color = new Color(1, 1, 1, 1 - t);
            afterRenderer.color = new Color(1, 1, 1, t);
            timer += Time.deltaTime;
            yield return null;
        }

        beforeRenderer.color = new Color(1, 1, 1, 0);
        afterRenderer.color = new Color(1, 1, 1, 1);
        isFading = false;
    }
}

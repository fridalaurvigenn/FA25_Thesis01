using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellbookAnimator : MonoBehaviour
{
    public Image targetImage;
    public Sprite[] frames;
    public float frameRate = 0.12f;

    private int currentFrame = 0;
    private int direction = 1;
    private float timer = 0f;

    void Start()
    {
        if (targetImage != null && frames.Length > 0)
        {
            targetImage.sprite = frames[0];
        }
    }

    void Update()
    {
        if (targetImage == null || frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= frameRate)
        {
            timer = 0f;
            AdvanceFrame();
        }
    }

    void AdvanceFrame()
    {
        targetImage.sprite = frames[currentFrame];

        currentFrame += direction;

        if (currentFrame >= frames.Length)
        {
            currentFrame = frames.Length - 2;
            direction = -1;
        }
        else if (currentFrame < 0)
        {
            currentFrame = 1;
            direction = 1;
        }
    }
}

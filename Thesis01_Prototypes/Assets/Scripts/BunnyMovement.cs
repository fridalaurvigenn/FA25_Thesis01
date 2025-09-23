using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BunnyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float hopHeight = 0.5f;
    public float hopSpeed = 5f;

    private bool shouldMove = false;
    private Vector3 startPosition;
    private float direction;

    void Start()
    {
        startPosition = transform.position;
        direction = Mathf.Sign(transform.localScale.x);
    }

    void Update()
    {
        if (shouldMove)
        {
            //Horizontal movement
            transform.position += Vector3.right * moveSpeed * Time.deltaTime * direction;

            //Hopping Y offset
            float hopOffset = Mathf.Sin(Time.time * hopSpeed) * hopHeight;
            transform.position = new Vector3(transform.position.x, startPosition.y + hopOffset, transform.position.z);
        }
    }

    public void StartMoving()
    {
        shouldMove = true;
        startPosition = transform.position;
        direction = Mathf.Sign(transform.localScale.x);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BunnyStop"))
        {
            shouldMove = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 1.25f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    private bool isFrozen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();  
    }

    void Update()
    {
        if (isFrozen) return;
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical") * 0.75f; //0.75f to dampen the up and down/backwards and forwards speed
    }

    void FixedUpdate()
    {
        //Only move if there is input
        if (moveInput != Vector2.zero)
        {
            rb.velocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed);
        }
        else
        {
            rb.velocity = Vector2.zero; //Stop moving immediately w no input
        }

            //Clamp the player's vertical position
            Vector2 clampedPosition = rb.position;
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -1.75f, 0.3f);
        rb.position = clampedPosition;
    }

    public void FreezeMovement(bool freeze)
    {
        isFrozen = freeze;

        if (freeze && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else if (!freeze && rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
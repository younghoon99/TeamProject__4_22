using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 inputVector;
    public float speed = 5f;
    public float jumpForce = 5f;
    public bool isGrounded = false;
    public Transform groundCheck;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground")); 
        inputVector.x = Input.GetAxis("Horizontal");
        inputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        if (inputVector.x > 0)
        {
            transform.localScale = new Vector3(-3, 3, 3);
        }
        else if (inputVector.x < 0)
        {
            transform.localScale = new Vector3(3, 3, 3);
        }
        if (inputVector.x != 0)
        {
            rb.AddForce(new Vector2(inputVector.x * speed, 0));
        }
        else
        {
            rb.AddForce(new Vector2(0, 0));
        }
        if (rb.velocity.magnitude > speed)
        {
            rb.velocity = new Vector2(rb.velocity.normalized.x * speed, rb.velocity.y);     
        }
    }
}

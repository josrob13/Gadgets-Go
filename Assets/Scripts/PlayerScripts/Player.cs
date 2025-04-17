using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private float velocity = 15.0f;
    [SerializeField] private float jumpForce = 15.0f;
    [SerializeField] private float acceleration = 15.0f;
    private Rigidbody rb;
    private Vector3 currentVelocity = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get input for movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Calculate movement vector
        Vector3 targetVelocity = new Vector3(moveHorizontal, 0.0f, moveVertical).normalized * velocity;

        // If input, accelerate
        if (targetVelocity.magnitude > 0)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }else {
            currentVelocity = Vector3.zero;
        }

        // Apply movement to the Rigidbody
        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(rb.linearVelocity.y) < 0.001f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Stop the game if escape is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}

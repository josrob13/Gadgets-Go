using Unity.Cinemachine;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed; // current speed of the player
    public float walkSpeed; // speed when walking
    public float sprintSpeed; // speed when sprinting

    public float groundDrag; // drag when on the ground

    public float jumpForce; // force of the jump
    public float jumpCooldown; // cooldown between jumps
    bool readyToJump = true;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    
    [Header("Camera controls")]
    public CinemachineCamera cinemachineCamera;
    public float amplitudeGain = 0.2f;
    public float frequencyGain = 0.84f;
    private float currentAmplitude = 0f;
    private float currentFrequency = 0f;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public Transform orientation;

    [Header("Inputs")]
    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    public MoveState moveState;
    public enum MoveState
    {
        Walking,
        Sprinting,
        Air
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        moveSpeed = walkSpeed;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position + Vector3.up * (playerHeight * 0.5f), Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();
        ShakeHandler();

        // Debug.Log("Grounded: " + grounded);

        // Debug.Log("Velocidad vertical: " + rb.linearVelocity.y);

        // handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();

        if (!grounded)
        {/*
            if (rb.linearVelocity.y < 0)
                rb.AddForce(Vector3.down * Physics.gravity.y * 2.5f, ForceMode.Acceleration);*/
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // jump
        if (Input.GetKeyDown(jumpKey) && grounded && readyToJump)
        {
            Debug.Log("Jump");
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void StateHandler()
    {
        bool sprinting = Input.GetKey(sprintKey);

        if (sprinting)
            moveSpeed = sprintSpeed;
        else
            moveSpeed = walkSpeed;

        if (grounded && sprinting)
            moveState = MoveState.Sprinting;
        else if (grounded)
            moveState = MoveState.Walking;
        else
            moveState = MoveState.Air;
    }

    private void ShakeHandler()
    {
        if (verticalInput != 0 || horizontalInput != 0)
        {
            if (moveState == MoveState.Walking)
            {
                SetCameraShake(amplitudeGain, frequencyGain);
            }
            else if (moveState == MoveState.Sprinting)
            {
                SetCameraShake(amplitudeGain + 0.5f, frequencyGain + 0.5f);
            }
        }
        else
            SetCameraShake(amplitudeGain, frequencyGain);
    }

    private void SetCameraShake(float amplitude, float frequency)
    {
        currentAmplitude = Mathf.Lerp(currentAmplitude, amplitude, Time.deltaTime * 1.5f);
        currentFrequency = Mathf.Lerp(currentFrequency, frequency, Time.deltaTime * 1.5f);

        CinemachineBasicMultiChannelPerlin perlin = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
        if (perlin != null)
        {
            perlin.AmplitudeGain = currentAmplitude;
            perlin.FrequencyGain = currentFrequency;
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // limit velocity
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float acceleration;
    public float deceleration;
    public float airMultiplier;
    public float runSpeed;

    public float minHeight;
    public float maxHeight;

    public float gravityStrength;

    private bool isGrounded;
    private bool extendingJump;

    private Vector2 movementInput;
    private bool jumpHeld;
    private bool jumpQueued;
    private bool dashQueued;

    public Rigidbody2D playerRigidbody;
    public EdgeCollider2D playerCollider;
    public Transform cameraTarget;

    private CameraController cameraController;
    private InputMaster inputMaster;

    // Start is called before the first frame update
    void Awake()
    {
        inputMaster = new InputMaster();

        // Make the camera follow the player
        cameraController = Camera.main.GetComponent<CameraController>();
        cameraController.SetTarget(cameraTarget);
    }

    // Update is called once per frame
    void Update()
    {
        #region Input

        // WASD movement
        movementInput.x = inputMaster.Player.Horizontal.ReadValue<float>();
        movementInput.y = inputMaster.Player.Vertical.ReadValue<float>();

        // Jumping
        jumpHeld = inputMaster.Player.Jump.ReadValue<float>() != 0f;
        jumpQueued = inputMaster.Player.Jump.triggered ? true : jumpQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate

        // Dashing
        dashQueued = inputMaster.Player.Dash.triggered ? true : dashQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate

        #endregion
    }

    // FixedUpdate is called once per physics iteration
    void FixedUpdate()
    {
        #region Ground Detection

        isGrounded = false;

        #endregion

        #region Horizontal Movement

        // How fast the player should be moving after fully accelerating
        Vector3 targetVelocity = movementInput.x * Vector3.right * runSpeed;

        Vector3 linearVelocity = playerRigidbody.velocity.x * Vector3.right;

        // Default values in case there is no movement input (fixes divide by zero)
        float projection = 0f;
        Vector3 accelerationForce = Vector3.zero;
        if (targetVelocity.sqrMagnitude > 0f)
        {
            // The current velocity is projected onto the target velocity
            projection = Vector3.Dot(targetVelocity, linearVelocity) / targetVelocity.magnitude;

            // The acceleration velocity to add to reach the target velocity, given a deceleration velocity
            accelerationForce = (targetVelocity.magnitude - projection) * targetVelocity.normalized;
        }

        // The deceleration velocity to add which aligns the current velocity with the target as efficiently as possible, given a desired target velocity
        // This should result in the most responsive turning while preserving deceleration and game feel
        Vector3 decelerationForce = (targetVelocity.normalized * projection) - linearVelocity;

        // Calculate movement (acceleration + deceleration) force based on grounded state and 
        Vector3 movementForce = Vector3.zero;
        movementForce += accelerationForce * (isGrounded ? acceleration : acceleration * airMultiplier);
        movementForce += decelerationForce * (isGrounded ? deceleration : deceleration * airMultiplier);
        playerRigidbody.AddForce(movementForce, ForceMode2D.Force);

        #endregion

        #region Jumping

        if (jumpHeld == false || isGrounded || playerRigidbody.velocity.y < 0f) extendingJump = false;

        if (jumpQueued)
        {
            Jump();
        }

        #endregion

        #region Gravity

        if (isGrounded == false)
        {
            // When the player is extending a jump, keep their gravity lowered so they can reach the max height
            float currentGravity = gravityStrength;
            currentGravity *= (extendingJump) ? (minHeight / maxHeight) : 1f;

            playerRigidbody.AddForce(Vector3.down * currentGravity, ForceMode2D.Force);
        }

        #endregion

        jumpQueued = false;
        dashQueued = false;
    }

    void Jump()
    {
        if (playerRigidbody.velocity.y < 0f)
        {
            // Nullify previous downward velocity
            playerRigidbody.AddForce(Vector3.up * -playerRigidbody.velocity.y, ForceMode2D.Impulse);
        }

        // Add jumping force, this amount allows the player to reach the minimum height at the default gravity from a standing jump
        float jumpForce = Mathf.Sqrt(2f * gravityStrength * minHeight);
        playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);

        // Enable extending the jump to the max height
        extendingJump = true;
    }

    #region Input System

    private void OnEnable()
    {
        inputMaster.Enable();
    }

    private void OnDisable()
    {
        inputMaster.Disable();
    }

    #endregion
}

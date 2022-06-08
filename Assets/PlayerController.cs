using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float acceleration;
    public float deceleration;
    public float airMultiplier;
    public float runSpeed;

    [Header("Jumping")]
    public float minJumpHeight;
    public float maxJumpHeight;

    [Header("Dashing")]
    public float dashForce;
    public float dashSuspenseDuration;

    [Header("Falling")]
    public float gravityStrength;
    public float terminalVelocity;

    [Header("Sprites")]
    public Sprite walkSprite;
    public Sprite jumpSprite;
    public Sprite fallSprite;
    public Sprite blinkSprite;

    [Header("Configuration")]
    public LayerMask groundLayers;
    public LayerMask platformLayer;
    public LayerMask spikeLayer;
    public AttemptStats currentAttempt;
    public Vector2 playerSize;
    
    // Timers
    private float groundCheckTimer;
    private float dashSuspenseTimer;
    
    // States
    private bool isGrounded;
    private bool isStandingOnPlatform;
    private bool extendingJump;
    private bool dashAvailable;

    // Inputs
    private Vector2 movementInput;
    private bool jumpHeld;
    private bool jumpQueued;
    private bool dashQueued;
    private bool facingRight;
    private Vector3 respawnCheckpoint;

    [Header("Components")]
    public Transform playerTransform;
    public Rigidbody2D playerRigidbody;
    public EdgeCollider2D playerCollider;
    public SpriteRenderer playerSprite;
    public Transform cameraTarget;

    private CameraController cameraController;
    private FallthroughPlatform fallthroughPlatforms;
    private InputMaster inputMaster;

    // Start is called before the first frame update
    void Awake()
    {
        inputMaster = new InputMaster();

        // Make the camera follow the player
        cameraController = Camera.main.GetComponent<CameraController>();
        cameraController.SetTarget(cameraTarget);

        fallthroughPlatforms = GameObject.FindWithTag("Platforms").GetComponent<FallthroughPlatform>();

        // Starting state
        facingRight = true;
        dashAvailable = true;

        #region Set Collider Shape

        // I'm using an edge collider for this because it stops the player getting caught on intersection between tiles

        playerCollider.points[0].x = -playerSize.x / 2f;
        playerCollider.points[0].y = 0f;

        playerCollider.points[1].x = -playerSize.x / 2f;
        playerCollider.points[1].y = playerSize.y;

        playerCollider.points[2].x = playerSize.x / 2f;
        playerCollider.points[2].y = playerSize.y;

        playerCollider.points[3].x = playerSize.x / 2f;
        playerCollider.points[3].y = 0f;

        playerCollider.points[4].x = -playerSize.x / 2f;
        playerCollider.points[4].y = 0f;

        #endregion
    }

    // Update is called once per frame
    void Update()
    {
        #region Input

        // WASD movement
        movementInput.x = inputMaster.Player.Horizontal.ReadValue<float>();
        movementInput.y = inputMaster.Player.Vertical.ReadValue<float>();
        if (movementInput.x < 0f) facingRight = false;
        if (movementInput.x > 0f) facingRight = true;

        // Jumping
        jumpHeld = inputMaster.Player.Jump.ReadValue<float>() != 0f;
        jumpQueued = inputMaster.Player.Jump.triggered ? true : jumpQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate

        // Dashing
        dashQueued = inputMaster.Player.Dash.triggered ? true : dashQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate

        #endregion

        #region Animations

        // Face the direction of our last movement input
        playerSprite.flipX = (facingRight == false);

        // Determine which sprite to show
        if (dashSuspenseTimer > 0f) playerSprite.sprite = blinkSprite;
        else if (playerRigidbody.velocity.y > 0.1f) playerSprite.sprite = jumpSprite;
        else if (playerRigidbody.velocity.y < -2f) playerSprite.sprite = fallSprite;
        else playerSprite.sprite = walkSprite;

        #endregion
    }

    // FixedUpdate is called once per physics iteration
    void FixedUpdate()
    {
        #region Taking Damage

        // If the player is currently alive
        if (currentAttempt.currentHealth > 0)
        {
            Vector2 boxCentre = playerTransform.position;
            boxCentre.y += playerSize.y / 2f;

            // If the player is touching a spike
            if (Physics2D.OverlapBox(boxCentre, playerSize * 0.9f, 0f, spikeLayer))
            {
                currentAttempt.currentHealth -= 1;

                if (currentAttempt.currentHealth <= 0)
                {
                    // Player is dead
                    GameOver();
                }
                else
                {
                    // Player lost a heart
                    Respawn();
                }
            }
        }

        #endregion

        #region Ground Detection

        if (groundCheckTimer <= 0f)
        {
            // Cache
            int groundHits = 0;
            int platformHits = 0;
            Vector2 rayOrigin = Vector2.zero;
            rayOrigin.y = playerTransform.position.y + 0.01f;
            float rayLength = 0.1f;
        
            // Raycast down from the bottom edge of the player hitbox
            for (int rayNumber = 0; rayNumber <= 8; rayNumber++)
            {
                // Move along the bottom of the player from left to right
                rayOrigin.x = playerTransform.position.x - (playerSize.x / 2f) + (playerSize.x * (rayNumber / 8f));

                if (Physics2D.Raycast(rayOrigin, Vector2.down, rayLength + 0.01f, groundLayers))
                {
                    // Ground layer hit by this ray
                    groundHits++;

                    // Check if the thing that was hit is a platform
                    if (Physics2D.Raycast(rayOrigin, Vector2.down, rayLength + 0.01f, platformLayer))
                    {
                        platformHits++;
                    }
                }
            }

            // Determine the grounded state based on raycast hits
            isGrounded = (groundHits > 1);
            isStandingOnPlatform = (platformHits > 0);
            groundCheckTimer = 0f;

            // If the player is in a safe space, save the position as a respawn point
            Vector3 boxCentre = playerTransform.position + (Vector3.up * (playerSize.y / 2f));
            Vector2 boxSize = playerSize;
            boxSize.x += 2f;

            // If the player is fully on the ground and there are no spikes nearby
            if (groundHits >= 9 && Physics2D.OverlapBox(boxCentre, boxSize, 0f, spikeLayer) == false)
            {
                respawnCheckpoint = playerTransform.position;
            }

        }
        else
        {
            // Continue timer
            groundCheckTimer -= Time.fixedDeltaTime;
            if (groundCheckTimer < 0f) groundCheckTimer = 0f;
        }

        #endregion

        #region Platform Interaction

        // If the player presses down + jump when on a platform
        if (isStandingOnPlatform && movementInput.y < 0f && jumpQueued)
        {
            // The jump input is "used up" when falling through a platform
            jumpQueued = false;

            // Put ground check on cooldown briefly
            groundCheckTimer = 0.1f;
            isGrounded = false;
            isStandingOnPlatform = false;

            // Disable platform collider for a short duration so we fall through it
            StartCoroutine(fallthroughPlatforms.FlipOnOff());
        }

        #endregion

        #region Horizontal Movement

        // How fast the player should be moving after fully accelerating
        Vector3 targetVelocity = movementInput.x * Vector3.right * runSpeed;

        // How fast the player is currently moving horizontally
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

        // Calculate movement (acceleration + deceleration) force based on grounded state
        Vector3 movementForce = Vector3.zero;
        movementForce += accelerationForce * (isGrounded ? acceleration : acceleration * airMultiplier);
        movementForce += decelerationForce * (isGrounded ? deceleration : deceleration * airMultiplier);

        // When dashing, ignore normal movement forces
        if (dashSuspenseTimer > 0f) movementForce = Vector3.zero;

        // Apply horizontal movement forces (acceleration/deceleration)
        playerRigidbody.AddForce(movementForce, ForceMode2D.Force);

        #endregion

        #region Dashing

        // Restore dash when grounded
        if (isGrounded) dashAvailable = true;

        // Decrease dash suspense timer
        if (dashSuspenseTimer > 0f)
        {
            dashSuspenseTimer -= Time.fixedDeltaTime;
            if (dashSuspenseTimer < 0f) dashSuspenseTimer = 0f;
        }

        // Dash when the input is pressed
        if (dashQueued && dashAvailable)
        {
            // Which direction should the player dash?
            Vector2 dashDirection = movementInput;

            // If the player is not holding any movement direction, dash in the direction we're currently facing
            if (movementInput == Vector2.zero) dashDirection.x = facingRight ? 1f : -1f;

            // Start the dash timer
            dashSuspenseTimer = dashSuspenseDuration;

            // Apply the dash force
            Dash(dashDirection);
        }

        #endregion

        #region Jumping

        // Stop extending jump
        if (jumpHeld == false || isGrounded || playerRigidbody.velocity.y < 0f) extendingJump = false;

        // Start new jump
        if (jumpQueued && isGrounded)
        {
            Jump();
        }

        #endregion

        #region Gravity + Terminal Velocity

        // Only apply when off ground
        if (isGrounded == false && dashSuspenseTimer <= 0f)
        {
            // When the player is extending a jump, keep their gravity lowered so they can reach the max height
            float currentGravity = gravityStrength;
            currentGravity *= (extendingJump) ? (minJumpHeight / maxJumpHeight) : 1f;

            // When to apply air resistance
            if (playerRigidbody.velocity.y < 0f)
            {
                // Coefficient of drag is equal to 2x gravity divded by falling speed squared
                float dragCoefficient = (2f * currentGravity) / (terminalVelocity * terminalVelocity);

                // Calculate resistance counter-force from drag coefficient and current velocity
                float airResistance = (dragCoefficient / 2f) * (playerRigidbody.velocity.y * playerRigidbody.velocity.y);

                // Apply air resistance force
                playerRigidbody.AddForce(Vector3.up * airResistance, ForceMode2D.Force);
            }

            // Apply gravity force
            playerRigidbody.AddForce(Vector3.down * currentGravity, ForceMode2D.Force);
        }

        #endregion

        // Reset queued inputs
        jumpQueued = false;
        dashQueued = false;
    }

    // Do a normal jump
    void Jump()
    {
        if (playerRigidbody.velocity.y < 0f)
        {
            // Nullify previous downward velocity
            playerRigidbody.AddForce(Vector3.up * -playerRigidbody.velocity.y, ForceMode2D.Impulse);
        }

        // Add jumping force, this amount allows the player to reach the minimum height at the default gravity from a standing jump
        float jumpForce = Mathf.Sqrt(2f * gravityStrength * minJumpHeight);
        playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);

        // Enable extending the jump to the max height
        extendingJump = true;

        // Put ground detection on cooldown briefly to prevent ground check immediately returning true
        groundCheckTimer = 0.1f;
        isGrounded = false;
        isStandingOnPlatform = false;
    }

    // Dash in a given direction
    void Dash(Vector2 direction)
    {
        dashAvailable = false;

        // Nullify previous velocity
        playerRigidbody.AddForce(-playerRigidbody.velocity, ForceMode2D.Impulse);

        // Apply dashing force
        playerRigidbody.AddForce(direction.normalized * dashForce, ForceMode2D.Impulse);

        // Stop player from holding jump and dashing to make super jump upwards
        extendingJump = false;
    }

    void GameOver()
    {
        Debug.Log("Game Over!");
    }

    void Respawn()
    {
        // Go back to the last safe position with no velocity
        playerTransform.position = respawnCheckpoint;
        playerRigidbody.AddForce(-playerRigidbody.velocity, ForceMode2D.Impulse);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variables 

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
    public float dashCooldown;

    [Header("Falling")]
    public float gravityStrength;
    public float terminalVelocity;

    [Header("Ladders")]
    public float climbUpSpeed;
    public float climbDownSpeed;
    public float ladderBounceForce; // Small boost applied when reaching the top of a ladder

    [Header("Timing Windows")]
    public float jumpCoyoteDuration;
    public float jumpBufferDuration;

    [Header("Sprites")]
    public Sprite walkSprite;
    public Sprite jumpSprite;
    public Sprite fallSprite;
    public Sprite blinkSprite;

    [Header("Camera")]
    public float cameraPanAmount;
    public float cameraPanDelay;
    public float cameraPanSpeed;

    [Header("Respawning")]
    public float respawnInputDelay;
    public PhysicsMaterial2D bounceMaterial;

    [Header("Configuration")]
    public LayerMask groundLayers;
    public LayerMask platformLayer;
    public LayerMask spikeLayer;
    public ContactFilter2D ladderFilter;
    public Vector2 playerSize;
    
    // Timers
    private float groundCheckTimer;
    private float dashSuspenseTimer;
    private float dashCooldownTimer;
    private float jumpCoyoteTimer;
    private float jumpBufferTimer;
    private float cameraPanDelayTimer;
    private float respawnInputDelayTimer;

    // States
    private bool isGrounded;
    private bool isStandingOnPlatform;
    private bool extendingJump;
    private bool dashAvailable;
    private bool isConnectedToLadder;
    private float ladderX;

    // Info
    private Vector3 respawnCheckpoint;
    private Vector3 cameraTargetOffset;

    // Inputs
    private Vector2 movementInput;
    private bool jumpHeld;
    private bool jumpQueued;
    private bool dashQueued;
    private bool facingRight;
    private bool climbHeldAndValid;
    private float lastClimbValue;

    [Header("Components")]
    public Transform playerTransform;
    public Rigidbody2D playerRigidbody;
    public EdgeCollider2D playerCollider;
    public SpriteRenderer playerSprite;
    public Transform cameraTarget;
    public TrailRenderer playerTrail;

    private CameraController cameraController;
    private FallthroughPlatform fallthroughPlatforms;
    private InputMaster inputMaster;
    public AttemptStats currentAttempt;

    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        inputMaster = new InputMaster();

        // Make the camera follow the player
        cameraController = GameObject.FindGameObjectWithTag("CameraHolder").GetComponent<CameraController>();
        cameraController.SetTarget(cameraTarget);

        fallthroughPlatforms = GameObject.FindWithTag("Platforms").GetComponent<FallthroughPlatform>();

        // Starting state
        facingRight = true;
        dashAvailable = true;

        jumpBufferTimer = 0f;
        jumpCoyoteTimer = 0f;

        cameraTargetOffset = cameraTarget.localPosition;

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

        if (respawnInputDelayTimer <= 0f && currentAttempt.currentHealth > 0)
        {
            // WASD movement
            movementInput.x = inputMaster.Player.Horizontal.ReadValue<float>();
            movementInput.y = inputMaster.Player.Vertical.ReadValue<float>();
            if (movementInput.x < 0f) facingRight = false;
            if (movementInput.x > 0f) facingRight = true;

            // Jumping
            jumpHeld = inputMaster.Player.Jump.ReadValue<float>() != 0f;
            jumpQueued = inputMaster.Player.Jump.triggered ? true : jumpQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate
            if (jumpQueued) jumpBufferTimer = jumpBufferDuration; // Set jump buffer briefly as we just queued a new input

            // Dashing
            dashQueued = inputMaster.Player.Dash.triggered ? true : dashQueued; // True on the Update frame the button is pressed, false when triggered or at the end of FixedUpdate

            // Climbing
            climbHeldAndValid = (lastClimbValue <= 0.3f && inputMaster.Player.Climb.ReadValue<float>() > 0.3f) ? true : climbHeldAndValid; // True on the Update frame the button is pressed, false when released or after dashing/jumping from a ladder
            if (inputMaster.Player.Climb.ReadValue<float>() <= 0.3f) climbHeldAndValid = false; // False if released
            lastClimbValue = inputMaster.Player.Climb.ReadValue<float>();
        }
        else
        {
            // Inputs must be on cooldown due to respawning

            // Reduce timer
            respawnInputDelayTimer -= Time.deltaTime;
            respawnInputDelayTimer = Mathf.Max(respawnInputDelayTimer, 0f);

            // Set inputs to default states
            movementInput = Vector2.zero;
            jumpHeld = false;
            jumpQueued = false;
            dashQueued = false;
            climbHeldAndValid = false;
            lastClimbValue = 0f;
        }

        #endregion

        #region Animations

        // Face the direction of our last movement input
        playerSprite.flipX = (facingRight == false);

        // Determine which sprite to show
        if (currentAttempt.currentHealth <= 0) playerSprite.sprite = blinkSprite; // Dead
        else if (dashSuspenseTimer > 0f) playerSprite.sprite = blinkSprite; // Dashing
        else if (isConnectedToLadder) playerSprite.sprite = blinkSprite; // Climbing
        else if (playerRigidbody.velocity.y > 0.1f) playerSprite.sprite = jumpSprite; // Moving upwards (jump/dash)
        else if (playerRigidbody.velocity.y < -2f) playerSprite.sprite = fallSprite; // Moving downwards (fall/dash)
        else playerSprite.sprite = walkSprite; // Running

        #endregion

        #region Camera Vertical Panning

        if (isGrounded && movementInput.y != 0f && movementInput.x == 0f)
        {
            // When grounded and holding up or down only
            cameraPanDelayTimer -= Time.deltaTime;
            cameraPanDelayTimer = Mathf.Max(cameraPanDelayTimer, 0f);

            // After the delay has completed, pan the camera target up/down
            if (cameraPanDelayTimer <= 0f)
            {
                Vector3 target = cameraTargetOffset + (movementInput.y * cameraPanAmount * Vector3.up);
                Vector3 from = cameraTarget.localPosition;
                Vector3 newPos = Vector3.Lerp(from, target, cameraPanSpeed * Time.deltaTime);

                cameraTarget.localPosition = newPos;
            }
        }
        else
        {
            // Reset delay timer
            cameraPanDelayTimer = cameraPanDelay;

            // Revert to standard position
            Vector3 from = cameraTarget.localPosition;
            Vector3 newPos = Vector3.Lerp(from, cameraTargetOffset, cameraPanSpeed * Time.deltaTime);

            cameraTarget.localPosition = newPos;
        }

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
                    Die();
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

        // Allow coyote time for a short duration after, because we're touching the ground currently
        if (isGrounded)
        {
            jumpCoyoteTimer = jumpCoyoteDuration;
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

            // Reset jump buffer windows so jank doesn't happen when falling through platforms
            jumpCoyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        #endregion

        #region Ladder Interaction

        if (isConnectedToLadder == false && climbHeldAndValid && dashSuspenseTimer <= 0f)
        {
            // Connect the player to a new ladder

            #region Ladder Connection

            Vector2 boxCentre = playerTransform.position;
            boxCentre.y += playerSize.y / 2f;

            // If the player is touching a ladder
            List<Collider2D> results = new List<Collider2D>();
            if (Physics2D.OverlapBox(boxCentre, playerSize, 0f, ladderFilter, results) > 0)
            {
                // Ensure the player has no velocity carrying over
                playerRigidbody.AddForce(-playerRigidbody.velocity, ForceMode2D.Impulse);

                Vector3 snappedPosition = playerTransform.position;
                snappedPosition.x = Mathf.Round(results[0].ClosestPoint(playerTransform.position).x);
                ladderX = snappedPosition.x;

                isConnectedToLadder = true;
                dashAvailable = true;
            }

            #endregion
        }
        else if (isConnectedToLadder)
        {
            // When connected to a ladder

            #region Ladder Climbing

            // Push the player up/down the ladder
            float targetYVel = 0f;
            if (movementInput.y > 0f) targetYVel = climbUpSpeed;
            if (movementInput.y < 0f) targetYVel = -climbDownSpeed;

            Vector2 movePos = Vector2.zero;
            movePos.x = Mathf.Lerp(playerTransform.position.x, ladderX, 20f * Time.fixedDeltaTime);
            movePos.y = (targetYVel * Time.fixedDeltaTime) + playerTransform.position.y;

            // Take a step up or down the ladder
            playerRigidbody.MovePosition(movePos);

            #endregion

            #region Ladder Disconnection

            // Disconnect when pressing up and no ladder above
            Vector2 boxCentreAbove = playerTransform.position;
            boxCentreAbove.y += playerSize.y * 1.4f;
            if (movementInput.y > 0f && Physics2D.OverlapBox(boxCentreAbove, playerSize, 0f, ladderFilter.layerMask) == false)
            {
                // Apply a small bump up force when reaching the top of the ladder
                playerRigidbody.AddForce(Vector2.up * ladderBounceForce, ForceMode2D.Impulse);
                isConnectedToLadder = false;
                climbHeldAndValid = false;
            }

            // Disconnect when pressing down and no ladder below
            Vector2 boxCentreBelow = playerTransform.position;
            boxCentreBelow.y -= playerSize.y * 0.6f;
            if (movementInput.y < 0f && Physics2D.OverlapBox(boxCentreBelow, playerSize, 0f, ladderFilter.layerMask) == false)
            {
                // Disconnect the player from this ladder
                isConnectedToLadder = false;
                climbHeldAndValid = false;
            }

            // Disconnect when not holding the climb button
            if (climbHeldAndValid == false) isConnectedToLadder = false;

            #endregion
        }

        #endregion

        #region Horizontal Movement

        // The player is allowed to run when not dashing and not climbing
        if (dashSuspenseTimer <= 0f && isConnectedToLadder == false && currentAttempt.currentHealth > 0)
        {
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

            // Apply horizontal movement forces (acceleration/deceleration)
            playerRigidbody.AddForce(movementForce, ForceMode2D.Force);
        }

        #endregion

        #region Dashing

        // Restore dash when grounded
        if (isGrounded && dashCooldownTimer <= 0f) dashAvailable = true;

        // Decrease dash suspense timer
        if (dashSuspenseTimer > 0f)
        {
            dashSuspenseTimer -= Time.fixedDeltaTime;
            if (dashSuspenseTimer < 0f) dashSuspenseTimer = 0f;
        }

        // Decrease dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
            if (dashCooldownTimer < 0f) dashCooldownTimer = 0f;
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
        if (jumpHeld == false || isGrounded || playerRigidbody.velocity.y <= 0f) extendingJump = false;

        // Start new standard jump
        if (jumpQueued && (isGrounded || isConnectedToLadder))
        {
            Jump();
        }

        // Start new coyote jump
        if (jumpQueued && jumpCoyoteTimer > 0f)
        {
            Jump();
        }

        jumpCoyoteTimer -= Time.fixedDeltaTime;
        jumpCoyoteTimer = Mathf.Max(jumpCoyoteTimer, 0f);

        // Start new buffered jump
        if (jumpBufferTimer > 0f && (isGrounded || isConnectedToLadder))
        {
            Jump();
        }

        jumpBufferTimer -= Time.fixedDeltaTime;
        jumpBufferTimer = Mathf.Max(jumpBufferTimer, 0f);

        #endregion

        #region Gravity + Terminal Velocity

        // Only apply when off ground and ladders, and not dashing
        if (isGrounded == false && dashSuspenseTimer <= 0f && isConnectedToLadder == false)
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

        // Disconnect from any ladder we were attached to
        if (isConnectedToLadder) climbHeldAndValid = false;
        isConnectedToLadder = false;

        // Cancel timing windows
        jumpBufferTimer = 0f;
        jumpCoyoteTimer = 0f;
    }

    // Dash in a given direction
    void Dash(Vector2 direction)
    {
        dashAvailable = false;
        dashCooldownTimer = dashCooldown;

        // Nullify previous velocity
        playerRigidbody.AddForce(-playerRigidbody.velocity, ForceMode2D.Impulse);

        // Apply dashing force
        playerRigidbody.AddForce(direction.normalized * dashForce, ForceMode2D.Impulse);

        // Stop player from holding jump and dashing to make super jump upwards
        extendingJump = false;

        // Disconnect from any ladder we were attached to
        if (isConnectedToLadder) climbHeldAndValid = false;
        isConnectedToLadder = false;

        // Reset jump timing windows
        jumpBufferTimer = 0f;
        jumpCoyoteTimer = 0f;

        // For the rest of this FixedUpdate, pretend we're not on the ground
        isGrounded = false;
        isStandingOnPlatform = false;

        // Shake the screen
        cameraController.AddTrauma(0.5f);
        cameraController.SetShakeDirection(direction);
    }

    // When the player is out of lives
    public void Die()
    {
        // Ensure lives are set to 0
        currentAttempt.currentHealth = 0;

        // Shake the screen
        cameraController.AddTrauma(1f);
        cameraController.SetShakeDirection(Vector2.zero);

        // Bounce around the screen
        playerRigidbody.sharedMaterial = bounceMaterial;
        playerCollider.sharedMaterial = bounceMaterial;
        Vector2 randomDirection = new Vector2(Random.Range(0.4f, 0.8f) * (Random.Range(0, 2) == 0 ? 1 : -1), Random.Range(0.5f, 1f));
        randomDirection.Normalize();
        playerRigidbody.AddForce(randomDirection * 30f, ForceMode2D.Impulse);
    }

    // When the player has extra lives to try again
    void Respawn()
    {
        // Go back to the last safe position with no velocity
        playerTransform.position = respawnCheckpoint;
        playerRigidbody.AddForce(-playerRigidbody.velocity, ForceMode2D.Impulse);

        playerTrail.Clear();

        // Start a short timer until the player can control their inputs again
        respawnInputDelayTimer = respawnInputDelay;

        // Shake the screen
        cameraController.AddTrauma(1f);
        cameraController.SetShakeDirection(Vector2.zero);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float acceleration;
    public float deceleration;
    public float runSpeed;

    public float minHeight;
    public float maxHeight;

    public float gravityStrength;

    private Vector2 movementInput;
    private bool jumpHeld;
    private bool jumpQueued;

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
        jumpQueued = inputMaster.Player.Jump.triggered ? true : jumpQueued; // True on the Update frame the button is pressed, false when triggered in FixedUpdate

        #endregion
    }

    // FixedUpdate is called once per physics iteration
    void FixedUpdate()
    {
        #region Horizontal Movement

        // How fast the player should be moving after fully accelerating
        float targetVelocity = movementInput.x * runSpeed;

        #endregion
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

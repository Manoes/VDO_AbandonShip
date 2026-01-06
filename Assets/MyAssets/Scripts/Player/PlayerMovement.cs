using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float groundAcceleration = 70f;
    [SerializeField] private float groundDeceleration = 90f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;

    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private float jumpBufferTime = 0.10f;
    [SerializeField] private float jumpCutMultiplier = 0.55f;

    [Header("Gravity Feel")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float maxFallSpeed = 22f;

    [Header("Wall Jump")]
    [SerializeField] private bool enableWallJump = true;
    [SerializeField] private float wallSlideMaxFallSpeed = 4.5f;
    [SerializeField] private float wallJumpXVelocity = 9f;
    [SerializeField] private float wallJumpYVelocity = 14f;
    [SerializeField] private float wallJumpLockTime = 0.12f;

    [Header("Variable Jump (Mario-like Hold)")]
    [SerializeField] private float holdJumpGravityMultiplier = 0.55f;
    [SerializeField] private float maxHoldJumpTime = 0.18f;

    [Header("Collision Checks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.65f, 0.10f);
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.10f, 0.9f);
    [SerializeField] private float checkDistance = 0.05f;

    [Header("Anti Double-Jump")]
    [SerializeField] private float ignoreGroundedAfterJump = 0.08f;

    Rigidbody2D rigidbody;
    Collider2D col;
    PlayerAnimation playerAnimation;

    float inputX;

    bool jumpPressedThisFrame;
    bool jumpReleasedThisFrame;
    bool jumpHeld;

    bool wantJumpCut;
    bool wantJump;
    bool wantWallJump;

    float coyoteTimer;
    float jumpBufferTimer;
    float holdJumpTimer;
    bool isJumping;

    bool isGrounded;
    bool rawGrounded;
    bool onWallLeft;
    bool onWallRight;

    float wallJumpLockTimer;
    int wallDirection;

    float ignoreGroundedTimer;

    public bool IsGrounded => isGrounded;
    public bool IsOnWall => !isGrounded && wallDirection != 0;
    public int LastJumpFrame { get; private set; } = -9999;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");

        jumpPressedThisFrame = Input.GetButtonDown("Jump");
        jumpReleasedThisFrame = Input.GetButtonUp("Jump");
        jumpHeld = Input.GetButton("Jump");

        if (jumpPressedThisFrame) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        if (ignoreGroundedTimer > 0f) ignoreGroundedTimer -= Time.deltaTime;

        UpdateCollisionStates();

        // Timers
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        wantJumpCut = jumpReleasedThisFrame && rigidbody.linearVelocity.y > 0f;

        wantJump = jumpBufferTimer > 0f && coyoteTimer > 0f;
        wantWallJump = jumpBufferTimer > 0f && enableWallJump && !isGrounded && wallDirection != 0;
    }

    void FixedUpdate()
    {
        UpdateCollisionStates();

        MoveHorizontally(inputX);
        if(playerAnimation) playerAnimation.SetFacingFromInput(inputX);

        if (wantJump)
        {
            DoJump(jumpVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            ignoreGroundedTimer = ignoreGroundedAfterJump;
            LastJumpFrame = Time.frameCount;

            wantJump = false;
        }
        else if (wantWallJump)
        {
            float launchX = -wallDirection * wallJumpXVelocity;
            rigidbody.linearVelocity = new Vector2(launchX, wallJumpYVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            wallJumpLockTimer = wallJumpLockTime;
            jumpBufferTimer = 0f;
            ignoreGroundedTimer = ignoreGroundedAfterJump;
            LastJumpFrame = Time.frameCount;

            wantWallJump = false;
        }

        if (wantJumpCut)
        {
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, rigidbody.linearVelocity.y * jumpCutMultiplier);
            wantJumpCut = false;
        }

        if (enableWallJump && !isGrounded && wallDirection != 0 && rigidbody.linearVelocity.y < 0f)
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, Mathf.Max(rigidbody.linearVelocity.y, -wallSlideMaxFallSpeed));

        ApplyGravityFeel(jumpHeld);

        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.fixedDeltaTime;
    }

    void MoveHorizontally(float inputX)
    {
        if (wallJumpLockTimer > 0f)
            inputX = 0f;

        float targetSpeed = inputX * maxSpeed;
        float speedDiff = targetSpeed - rigidbody.linearVelocity.x;

        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate = isGrounded
            ? (accelerating ? groundAcceleration : groundDeceleration)
            : (accelerating ? airAcceleration : airDeceleration);

        float movement = accelRate * speedDiff;
        rigidbody.AddForce(new Vector2(movement, 0f), ForceMode2D.Force);

        float clampedX = Mathf.Clamp(rigidbody.linearVelocity.x, -maxSpeed, maxSpeed);
        rigidbody.linearVelocity = new Vector2(clampedX, rigidbody.linearVelocity.y);
    }

    void TryConsumeJump()
    {
        if (jumpBufferTimer <= 0f) return;

        // Ground Jump
        if (coyoteTimer > 0f)
        {
            DoJump(jumpVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            ignoreGroundedTimer = ignoreGroundedAfterJump;            
            LastJumpFrame = Time.frameCount;
            return;
        }

        // Wall Jump
        if (enableWallJump && !isGrounded && wallDirection != 0)
        {
            float launchX = -wallDirection * wallJumpXVelocity;
            rigidbody.linearVelocity = new Vector2(launchX, wallJumpYVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            wallJumpLockTimer = wallJumpLockTime;
            jumpBufferTimer = 0f;

            ignoreGroundedTimer = ignoreGroundedAfterJump;
            LastJumpFrame = Time.frameCount;
            return;
        }
    }

    void DoJump(float velY)
    {
        rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, velY);
    }

    void ApplyGravityFeel(bool holdingJump)
    {
        if (isJumping && holdingJump && rigidbody.linearVelocity.y > 0f)
        {
            float extraUp = Physics2D.gravity.y * (holdJumpGravityMultiplier - 1f) * rigidbody.mass;
            rigidbody.AddForce(Vector2.up * extraUp);
            return;
        }

        if (rigidbody.linearVelocity.y < 0f)
            rigidbody.AddForce(Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rigidbody.mass);

        if (rigidbody.linearVelocity.y < -maxFallSpeed)
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, -maxFallSpeed);
    }

    void UpdateCollisionStates()
    {
        Bounds bounds = col.bounds;

        // Always Compute RAW Grounded 
        Vector2 groundCenter = new Vector2(bounds.center.x, bounds.min.y - checkDistance);
        rawGrounded = Physics2D.OverlapBox(groundCenter, groundCheckSize, 0f, groundMask);

        // If we touch the Ground and not moving Up, Cancel the Ignore Grounded Lockout Immediatly
        if(rawGrounded && rigidbody.linearVelocity.y <= 0.01f)
            ignoreGroundedTimer = 0f;
        
        isGrounded = rawGrounded && ignoreGroundedTimer <= 0f;

        // Walls
        Vector2 leftCenter = new Vector2(bounds.min.x - checkDistance, bounds.center.y);
        Vector2 rightCenter = new Vector2(bounds.max.x + checkDistance, bounds.center.y);

        onWallLeft = Physics2D.OverlapBox(leftCenter, wallCheckSize, 0f, groundMask);
        onWallRight = Physics2D.OverlapBox(rightCenter, wallCheckSize, 0f, groundMask);

        wallDirection = 0;
        if (!isGrounded)
        {
            if (onWallLeft) wallDirection = -1;
            else if (onWallRight) wallDirection = +1;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!col) col = GetComponent<Collider2D>();
        if (!col) return;

        Bounds bounds = col.bounds;

        Gizmos.color = Color.green;
        Vector2 groundCenter = new Vector2(bounds.center.x, bounds.min.y - checkDistance);
        Gizmos.DrawWireCube(groundCenter, groundCheckSize);

        Gizmos.color = Color.cyan;
        Vector2 leftCenter = new Vector2(bounds.min.x - checkDistance, bounds.center.y);
        Vector2 rightCenter = new Vector2(bounds.max.x + checkDistance, bounds.center.y);
        Gizmos.DrawWireCube(leftCenter, wallCheckSize);
        Gizmos.DrawWireCube(rightCenter, wallCheckSize);
    }
}

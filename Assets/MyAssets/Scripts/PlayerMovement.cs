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

    Rigidbody2D rb;
    Collider2D col;
    JetpackAbility jetpack;

    float coyoteTimer;
    float jumpBufferTimer;
    float holdJumpTimer;
    bool isJumping;

    bool isGrounded;
    bool onWallLeft;
    bool onWallRight;

    float wallJumpLockTimer;
    int wallDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        jetpack = GetComponent<JetpackAbility>();
    }

    void Update()
    {
        UpdateCollisionStates();

        // Timers
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Jump Input
        if (Input.GetButtonDown("Jump"))
        {
            // If we are NOT in a Normal Jumpable State, try Jetpack first
            bool canNormalJumpSoon = coyoteTimer > 0f;
            bool canWallJump = enableWallJump && !isGrounded && wallDirection != 0;

            if (!canNormalJumpSoon && !canWallJump && jetpack != null)
            {
                if (jetpack.TryBoost(isGrounded))
                {
                    // Don't Buffer a Normal Jump; this press was used for boost
                    jumpBufferTimer = 0f;
                }
                else
                {
                    jumpBufferTimer = jumpBufferTime;
                }
            }
            else
            {
                jumpBufferTimer = jumpBufferTime;
            }
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Jump Cut (release)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // Decide Jump
        TryConsumeJump();

        // Ground Notify (optional)
        if (isGrounded && jetpack != null)
            jetpack.OnGrounded();

        // Stop "Jumping" State
        if (isGrounded || rb.linearVelocity.y <= 0f)
        {
            isJumping = false;
            holdJumpTimer = 0f;
        }
        else if (isJumping)
        {
            holdJumpTimer -= Time.deltaTime;
            if (holdJumpTimer <= 0f) isJumping = false;
        }

        // Wall Slide Clamp
        if (enableWallJump && !isGrounded && wallDirection != 0 && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideMaxFallSpeed));
        }

        ApplyGravityFeel();

        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        MoveHorizontally(inputX);
    }

    void MoveHorizontally(float inputX)
    {
        if (wallJumpLockTimer > 0f)
            inputX = 0f;

        float targetSpeed = inputX * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate = isGrounded
            ? (accelerating ? groundAcceleration : groundDeceleration)
            : (accelerating ? airAcceleration : airDeceleration);

        float movement = accelRate * speedDiff;
        rb.AddForce(new Vector2(movement, 0f), ForceMode2D.Force);

        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
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
            return;
        }

        // Wall Jump
        if (enableWallJump && !isGrounded && wallDirection != 0)
        {
            float launchX = -wallDirection * wallJumpXVelocity;
            rb.linearVelocity = new Vector2(launchX, wallJumpYVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            wallJumpLockTimer = wallJumpLockTime;
            jumpBufferTimer = 0f;
            return;
        }
    }

    void DoJump(float velY)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, velY);
    }

    void ApplyGravityFeel()
    {
        bool holdingJump = Input.GetButton("Jump");
        if (isJumping && holdingJump && rb.linearVelocity.y > 0f)
        {
            float extraUp = Physics2D.gravity.y * (holdJumpGravityMultiplier - 1f) * rb.mass;
            rb.AddForce(Vector2.up * extraUp);
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rb.mass);
        }

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    void UpdateCollisionStates()
    {
        Bounds bounds = col.bounds;

        Vector2 groundCenter = new Vector2(bounds.center.x, bounds.min.y - checkDistance);
        isGrounded = Physics2D.OverlapBox(groundCenter, groundCheckSize, 0f, groundMask);

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

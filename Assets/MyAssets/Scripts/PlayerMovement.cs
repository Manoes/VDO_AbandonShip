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

    Rigidbody2D rigidbody;
    Collider2D collider;

    // Jump Parameters
    float coyoteTimer;
    float jumpBufferTimer;
    float holdJumpTimer;
    bool isJumping;

    // Jump States
    bool isGrounded;
    bool onWallLeft;
    bool onWallRight;

    // Wall Jump Parameters
    float wallJumpLockTimer;
    int wallDirection;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
    }

    void Update()
    {
        UpdateCollisionStates();

        // Timers 
        if(isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if(Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        // Jump Out (release)
        if(Input.GetButtonUp("Jump") && rigidbody.linearVelocity.y > 0f)
        {
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, rigidbody.linearVelocity.y * jumpCutMultiplier);
        }

        // Decide Jump
        TryConsumeJump();

        // Stop "Jumping" State when we Start Falling or Touch Ground
        if(isGrounded || rigidbody.linearVelocity.y <= 0f)
        {
            isJumping = false;
            holdJumpTimer = 0f;
        }
        else if (isJumping)
        {
            holdJumpTimer -= Time.deltaTime;
            if(holdJumpTimer <= 0f) isJumping = false;
        }

        // Wall Slide Clamp
        if(enableWallJump && !isGrounded && wallDirection != 0 && rigidbody.linearVelocity.y < 0f)
        {
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, Mathf.Max(rigidbody.linearVelocity.y, -wallSlideMaxFallSpeed));
        }

        // Gravity Shaping + Fall Clamp
        ApplyGravityFeel();

        // Wall Jump Lock Timer
        if(wallJumpLockTimer > 0f) 
            wallJumpLockTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        MoveHorizontally(inputX);
    }

    void MoveHorizontally(float inputX)
    {
        // While Locked after Wall Jump, ignore Input so you don't Cancel the Launch
        if(wallJumpLockTimer > 0f) 
            inputX = 0f;
        
        float targetSpeed = inputX * maxSpeed;
        float speedDiff = targetSpeed - rigidbody.linearVelocity.x;

        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate;

        if(isGrounded)
            accelRate = accelerating ? groundAcceleration : groundDeceleration;
        else   
            accelRate = accelerating ? airAcceleration : airDeceleration;
        
        float movement = accelRate * speedDiff;
        rigidbody.AddForce(new Vector2(movement, 0f), ForceMode2D.Force);

        // Clamp X Speed
        float clampedX = Mathf.Clamp(rigidbody.linearVelocity.x, -maxSpeed, maxSpeed);
        rigidbody.linearVelocity = new Vector2(clampedX, rigidbody.linearVelocity.y);
    }

    void TryConsumeJump()
    {
        if(jumpBufferTimer <= 0f) return;

        // Ground Jump
        if(coyoteTimer > 0f)
        {
            DoJump(jumpVelocity);
            
            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            return;
        }

        // Wall Jump
        if(enableWallJump && !isGrounded && wallDirection != 0)
        {
            // Launch away from the Wall
            float launchX = -wallDirection * wallJumpXVelocity;
            rigidbody.linearVelocity = new Vector2(launchX, wallJumpYVelocity);

            isJumping = true;
            holdJumpTimer = maxHoldJumpTime;

            wallJumpLockTimer = wallJumpLockTime;
            jumpBufferTimer = 0f;
            return;
        }
    }

    void DoJump(float velY)
    {
        rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, velY);
    }

    void ApplyGravityFeel()
    {
        // Mario-like Jump: while Rising + Holding Jump + within Hold Window => Lighter Gravity
        bool holdingJump = Input.GetButton("Jump");
        if(isJumping && holdingJump && rigidbody.linearVelocity.y > 0f)
        {
            // Add Upward Force Equal to Reducing Gravity
            float extraUp = Physics2D.gravity.y * (holdJumpGravityMultiplier - 1f) * rigidbody.mass;
            rigidbody.AddForce(Vector2.up * extraUp);
            return; // Skip other Garvity Shaping this Frame
        }

        // Extra Gravity when Falling
        if(rigidbody.linearVelocity.y < 0f)
        {
            rigidbody.AddForce(Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rigidbody.mass);
        }

        // Clamp Fall Speed
        if(rigidbody.linearVelocity.y < -maxFallSpeed)
        {
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, -maxFallSpeed);
        }
    }

    void UpdateCollisionStates()
    {
        Bounds bounds = collider.bounds;

        // Ground Check Box under Collider
        Vector2 groundCenter = new Vector2(bounds.center.x, bounds.min.y - checkDistance);
        isGrounded = Physics2D.OverlapBox(groundCenter, groundCheckSize, 0f, groundMask);

        // Wall Checks
        Vector2 leftCenter = new Vector2(bounds.min.x - checkDistance, bounds.center.y);
        Vector2 rightCenter = new Vector2(bounds.max.x + checkDistance, bounds.center.y);

        onWallLeft = Physics2D.OverlapBox(leftCenter, wallCheckSize, 0f, groundMask);
        onWallRight = Physics2D.OverlapBox(rightCenter, wallCheckSize, 0f, groundMask);

        wallDirection = 0;
        if (!isGrounded)
        {
            if(onWallLeft) wallDirection = -1;
            else if(onWallRight) wallDirection = +1;
        }
    }

    void OnDrawGizmosSelected()
    {
        if(!collider) collider = GetComponent<Collider2D>();
        if(!collider) return;

        Bounds bounds = collider.bounds;

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

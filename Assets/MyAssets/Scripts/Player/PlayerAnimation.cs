using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Rigidbody2D rigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Facing")]
    [Tooltip("Min |vx| before we Update Facing (prevents Jitter)")]
    [SerializeField] private float faceDeadZone = 0.05f;
    [Tooltip("If True, use Velocity for Facing. Otherwise use Input")]
    [SerializeField] private bool faceByVelocity = true;
    [Tooltip("When Dead, Stop Updating Facing.")]
    [SerializeField] private bool lockFacingOnDeath = true;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string deadParam = "Dead";

    private Health health;
    private PlayerAudio playerAudio;

    int speedHash, groundedHash, deadHash;
    bool hasSpeed, hasGrounded, hasDead;

    int facing = 1; // 1 = Right, -1 = Left

    void Awake()
    {
        if(!movement) movement = GetComponent<PlayerMovement>();
        if(!rigidbody) rigidbody = GetComponent<Rigidbody2D>();
        if(!animator) animator = GetComponent<Animator>();
        if(!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if(!health) health = GetComponent<Health>();
        if(!playerAudio) 
        {
            playerAudio = GetComponent<PlayerAudio>();
            playerAudio.Init();
        }

        speedHash = Animator.StringToHash(speedParam);
        groundedHash = Animator.StringToHash(groundedParam);
        deadHash = Animator.StringToHash(deadParam);

        hasSpeed = HasParam(animator, speedHash, AnimatorControllerParameterType.Float);
        hasGrounded = HasParam(animator, groundedHash, AnimatorControllerParameterType.Bool);
        hasDead = HasParam(animator, deadHash, AnimatorControllerParameterType.Bool);       
    }

    // Update is called once per frame
    void Update()
    {
        if(!movement || !rigidbody) return;

        bool isDead = false;
        if(health) isDead = health.IsDead;

        if(!(lockFacingOnDeath && isDead))
            UpdateFacing();
        
        UpdateAnimator(isDead);
    }

    void UpdateFacing()
    {
        if(!spriteRenderer) return;

        float signal = faceByVelocity ? rigidbody.linearVelocity.x : 0f;

        if(Mathf.Abs(signal) > faceDeadZone)
            facing = signal < 0f ? -1 : 1;
        
        spriteRenderer.flipX = (facing < 0);
    }

    void UpdateAnimator(bool isDead)
    {
        if(!animator) return;

        if(hasSpeed) animator.SetFloat(speedHash, Mathf.Abs(rigidbody.linearVelocity.x));
        if(hasGrounded) animator.SetBool(groundedHash, movement.IsGrounded);
        if(hasDead) animator.SetBool(deadHash, isDead);
    }

    static bool HasParam(Animator animator, int hash, AnimatorControllerParameterType type)
    {
        if(!animator) return false;
        var parameters = animator.parameters;
        for(int i = 0; i < parameters.Length; i++)
            if(parameters[i].nameHash == hash && parameters[i].type == type)
                return true;
        return false;
    }

    public void SetFacingFromInput(float inputX)
    {
        if(Mathf.Abs(inputX) > 0.01f)
            facing = inputX < 0f ? -1 : 1;
        if(spriteRenderer) spriteRenderer.flipX = (facing < 0);
    }    
}

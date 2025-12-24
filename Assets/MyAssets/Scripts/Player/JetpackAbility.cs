using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class JetpackAbility : MonoBehaviour
{
    [Header("Charges")]
    [SerializeField] private int maxCharges = 1;
    [SerializeField] private float cooldown = 0.10f; // Prevents double-trigger on same press

    [Header("Boost Feel")]
    [SerializeField] private float boostUpSpeed = 16f; // set-to speed (not add force)
    [SerializeField] private bool onlyInAir = true;
    [SerializeField] private bool blockWhileOnWall = true;
    [SerializeField] private bool zeroOutDownVelocityOnBoost = true;

    [Header("Critical Gating")]
    [SerializeField] private bool requireReleaseBeforeBoost = true;

    [Header("VFX")]
    [SerializeField] private ParticleSystem jetpackParticles;
    [SerializeField] private float vfxBurstDuration = 0.12f;

    Rigidbody2D rb;
    PlayerMovement movement;

    int charges;
    float cooldownTimer;
    float vfxTimer;

    bool releaseArmed = true;

    void OnEnable()
    {
        charges = maxCharges;
        releaseArmed = true;
        cooldownTimer = 0f;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        charges = maxCharges;

        if (jetpackParticles)
            jetpackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // Release gating (actually enforced)
        if (requireReleaseBeforeBoost && Input.GetButtonUp("Jump"))
            releaseArmed = true;

        // stop VFX after short burst
        if (vfxTimer > 0f)
        {
            vfxTimer -= Time.deltaTime;
            if (vfxTimer <= 0f && jetpackParticles && jetpackParticles.isPlaying)
                jetpackParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Tap-to-boost
        if (Input.GetButtonDown("Jump"))
            TryBoost();
    }

    public void AddCharges(int amount)
    {
        charges = Mathf.Clamp(charges + amount, 0, maxCharges);
    }

    bool TryBoost()
    {
        if (charges <= 0) { Debug.Log("Jetpack: blocked - no charges"); return false; }
        if (cooldownTimer > 0f) { Debug.Log("Jetpack: blocked - cooldown"); return false; }

        if (requireReleaseBeforeBoost && !releaseArmed)
        {
            Debug.Log("Jetpack: blocked - need release");
            return false;
        }

        if (movement != null)
        {
            if (onlyInAir && movement.IsGrounded) { Debug.Log("Jetpack: blocked - grounded"); return false; }
            if (blockWhileOnWall && movement.IsOnWall) { Debug.Log("Jetpack: blocked - on wall"); return false; }

            // This is the KEY: don’t let the same press do jump/walljump AND jetpack.
            if (movement.LastJumpFrame == Time.frameCount) { Debug.Log("Jetpack: blocked - same frame as jump"); return false; }
        }

        Vector2 v = rb.linearVelocity;

        if (zeroOutDownVelocityOnBoost && v.y < 0f)
            v.y = 0f;

        if (v.y < boostUpSpeed)
            v.y = boostUpSpeed;

        rb.linearVelocity = v;

        charges--;
        cooldownTimer = cooldown;

        if (requireReleaseBeforeBoost)
            releaseArmed = false;

        if (jetpackParticles)
        {
            jetpackParticles.Play(true);
            vfxTimer = vfxBurstDuration;
        }

        return true;
    }

    public void StopVFX()
    {
        vfxTimer = 0f;
        if (jetpackParticles)
            jetpackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

using UnityEngine;

public class JetpackAbility : MonoBehaviour
{
    [Header("Boost")]
    [SerializeField] private float boostVelocityY = 20f;     // upward impulse target
    [SerializeField] private int maxCharges = 1;            // 1 feels best for arcade
    [SerializeField] private float cooldown = 0.08f;        // prevents double trigger same press

    [Header("Rules")]
    [SerializeField] private bool onlyInAir = true;
    [SerializeField] private bool resetChargesOnGround = false; // usually false; pickup-driven

    [Header("VFX")]
    [SerializeField] private ParticleSystem jetpackParticles;
    [Tooltip("How Many Particles we Emit per Boost. If 0, we just Play()")]
    [SerializeField] private int emitCount = 18;
    [SerializeField] private float vfxStopDelay = 0.08f;

    Rigidbody2D rb;
    int charges;
    float cooldownTimer;
    float stopVFXAt;

    public int Charges => charges;
    public bool CanBoost => charges > 0 && cooldownTimer <= 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Make the Effect behave like a burst 
        if (jetpackParticles)
        {
            var main = jetpackParticles.main;
            main.loop = false;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // stop VFX after short burst
        if (jetpackParticles && stopVFXAt > 0f && Time.deltaTime >= stopVFXAt)
        {
            stopVFXAt = 0f;
            // Stop Emitting new Particles, keep Existing ones Alive Naturally
            jetpackParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void AddCharges(int amount)
    {
        charges = Mathf.Clamp(charges + amount, 0, maxCharges);
    }

    public void OnGrounded()
    {
        if (resetChargesOnGround)
            charges = maxCharges;
    }

    public bool TryBoost(bool isGrounded)
    {
        if (!CanBoost) return false;
        if (onlyInAir && isGrounded) return false;

        // Apply upward boost without killing horizontal momentum
        Vector2 v = rb.linearVelocity;
        if (v.y < boostVelocityY)
            v.y = boostVelocityY;
        rb.linearVelocity = v;

        charges--;
        print($"[JetpackAbility] Charge used! Charges left: {charges}");
        cooldownTimer = cooldown;

        PlayBoostVFX();

        return true;
    }

    void PlayBoostVFX()
    {
        if(!jetpackParticles) return;

        // Make sure it can Restart cleanly even if it was Mid-Play
        jetpackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if(emitCount > 0)
        {
            jetpackParticles.Play(true);
            jetpackParticles.Emit(emitCount);

            // Stop Shortly after so it doesn't look like Continuous Burst
            stopVFXAt = Time.time + vfxStopDelay;
        }
        else
        {
            // Fallback: Just Play the Particle System
            jetpackParticles.Play(true);
            stopVFXAt = Time.time + vfxStopDelay;
        }
    }
}

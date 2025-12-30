using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beam;
    [SerializeField] private Transform leftEmitter;
    [SerializeField] private Transform rightEmitter;

    [Header("Damage")]
    [SerializeField] private float damageInterval = 0.15f;

    BoxCollider2D beamCollider;
    SpriteRenderer beamRenderer;
    float nextDamageTime;

    void Awake()
    {
        beamRenderer = beam.GetComponent<SpriteRenderer>();
        beamCollider = beam.GetComponent<BoxCollider2D>();

        beamCollider.isTrigger = true;
    }

    public void SetLengthWorld(float lengthWorld)
    {
        // Beam visual
        Vector3 s = beam.localScale;
        s.x = lengthWorld;
        beam.localScale = s;

        // Collider matches beam
        beamCollider.size = new Vector2(lengthWorld, beamCollider.size.y);
        beamCollider.offset = Vector2.zero;

        // Emitters snap to edges
        float half = lengthWorld * 0.5f;
        leftEmitter.localPosition  = new Vector3(-half, 0f, 0f);
        rightEmitter.localPosition = new Vector3( half, 0f, 0f);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + damageInterval;

        var health = other.GetComponent<Health>();
        if (health)
            health.TakeDamage(1);
    }
}

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class FallingTilemapPlatform : MonoBehaviour
{
    [SerializeField] private float fallDelay = 0.35f;
    [SerializeField] private float ignorePlayerCollisionSeconds = 0.05f;

    Rigidbody2D rb;
    CompositeCollider2D col;

    bool triggered;
    Coroutine routine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CompositeCollider2D>();

        // Must be NON-trigger if we rely on OnCollisionEnter2D.
        col.isTrigger = false;

        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
    }

    public void ResetPlatform()
    {
        triggered = false;
        if (routine != null) StopCoroutine(routine);
        routine = null;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;
        if (!collision.collider.CompareTag("Player")) return;

        triggered = true;
        routine = StartCoroutine(FallRoutine(collision.collider));
    }

    IEnumerator FallRoutine(Collider2D playerCol)
    {
        yield return new WaitForSeconds(fallDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;

        // Prevent the “collision kick / glitch” right when it becomes dynamic
        if (playerCol != null)
        {
            Physics2D.IgnoreCollision(playerCol, col, true);
            yield return new WaitForSeconds(ignorePlayerCollisionSeconds);
            Physics2D.IgnoreCollision(playerCol, col, false);
        }
    }
}

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private float fallDelay = 0.35f;

    [Header("Debug Visual")]
    [SerializeField] private bool useDebugColor = true;
    [SerializeField] private Color debugColor = Color.yellow;

    Rigidbody2D rigidbody;
    SpriteRenderer spriteRenderer;

    bool armed;
    bool isTriggered;
    Coroutine fallRoutine;
    Color originalColor;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer) originalColor = spriteRenderer.color;

        // Platforms should Start fixed in Place
        rigidbody.bodyType = RigidbodyType2D.Static;
    }

    public void SetArmed(bool value)
    {
        armed = value;
        isTriggered = false;

        // Cancel any Running Routine
        if(fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }

        // Reset Physics + Visuals
        rigidbody.bodyType = RigidbodyType2D.Static;

        if(spriteRenderer && useDebugColor)
            spriteRenderer.color = armed ? debugColor : originalColor; 
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(!armed || isTriggered) return;

        if(!collision.collider.CompareTag("Player"))
            return;
        
        isTriggered = true;
        fallRoutine = StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(fallDelay);
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
    }
}

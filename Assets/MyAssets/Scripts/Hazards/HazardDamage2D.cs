using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HazardDamage2D : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private string playerTag = "Player";

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = useTrigger;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(!useTrigger) return;
        TryHit(collision);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(useTrigger) return;
        TryHit(collision.collider);
    }

    void TryHit(Collider2D collider)
    {
        if(!collider.CompareTag(playerTag)) return;

        var healh = collider.GetComponentInParent<Health>();
        if(healh) healh.TakeDamage(damage);
    }
}

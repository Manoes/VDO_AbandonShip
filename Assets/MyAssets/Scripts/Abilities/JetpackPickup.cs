using UnityEngine;

public class JetpackPickup : MonoBehaviour
{
    [SerializeField] private int charges = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var jetpack = collision.GetComponent<JetpackAbility>();
        if (jetpack != null)
            jetpack.AddCharges(charges);

        Destroy(gameObject);
    }
}

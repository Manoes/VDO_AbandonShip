using UnityEngine;

public class JetpackPickup : MonoBehaviour
{
    [SerializeField] private int chargesGranted = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var thruster = collision.GetComponent<JetpackAbility>();
        if (thruster != null)
            thruster.AddCharges(chargesGranted);

        Destroy(gameObject);
    }
}

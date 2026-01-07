using UnityEngine;

public class JetpackPickup : MonoBehaviour
{
    [SerializeField] private int charges = 1;

    [Header("Sound FX")]
    [SerializeField] private AudioClip pickupSFX;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var jetpack = collision.GetComponent<JetpackAbility>();
        if (jetpack != null)
            jetpack.AddCharges(charges);
        
        AudioSource.PlayClipAtPoint(pickupSFX, transform.position, 1f);

        Destroy(gameObject);
    }
}

using UnityEngine;

public class DeathWallSound : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float fullVolumeDistance = 6f;
    [SerializeField] private float silentDistance = 60f;

    [Header("Audio & Volume")]
    [SerializeField] private AudioSource audioSource;
    [Range(0f, 1f)] [SerializeField] private float minvolume = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float maxvolume = 1f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
        
        if (!player)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO)
                player = playerGO.transform;
        }
    }

    void Update()
    {
        if (!player || !audioSource) return;

        float distance = Vector2.Distance(player.position, transform.position);

        float t = Mathf.InverseLerp(silentDistance, fullVolumeDistance, distance);
        float close = curve.Evaluate(t);

        audioSource.volume = Mathf.Lerp(minvolume, maxvolume, close);
    }
}

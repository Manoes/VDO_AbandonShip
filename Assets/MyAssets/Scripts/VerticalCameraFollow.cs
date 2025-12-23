using UnityEngine;

public class VerticalCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float yOffset = 1.5f;

    [Header("Follow Speeds")]
    [SerializeField] private float upSmoothTime = 0.20f;
    [SerializeField] private float downSmoothTime = 0.60f;

    float velY;
    float minY;

    void Start()
    {
        if(!player) enabled = false;
        minY = transform.position.y;
    }

    void LateUpdate()
    {
        float desiredY = player.position.y + yOffset;

        // Don't Follow Down Aggressively
        desiredY = Mathf.Max(desiredY, minY);

        float smooth = desiredY > transform.position.y ? upSmoothTime : downSmoothTime;
        float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref velY, smooth);

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        minY = Mathf.Max(minY, transform.position.y - 0.5f);
    }
}

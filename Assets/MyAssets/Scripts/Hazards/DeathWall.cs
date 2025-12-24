using UnityEngine;

public class DeathWall : MonoBehaviour
{   
    [SerializeField] private Transform player;

    [Header("Chase")]
    [SerializeField] private float desiredGap = 6.0f;
    [SerializeField] private float minSpeed = 0.6f;
    [SerializeField] private float maxSpeed = 4.0f;
    [SerializeField] private float catchUpStrength = 0.6f;

    void Update()
    {
        if(!player) return;

        float targetY = player.position.y - desiredGap;

        // If Wall is below Target -> Move Up, if already Close -> Crawl 
        float diff = targetY - transform.position.y;
        float speed = Mathf.Clamp(minSpeed + diff * catchUpStrength, minSpeed, maxSpeed);

        transform.position += Vector3.up * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player")) return;

        if(GameManager.Instance != null)
            GameManager.Instance.KillPlayer("DeathWall");
    }
}

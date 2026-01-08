using UnityEngine;

public class DeathWall : MonoBehaviour
{   
    [SerializeField] private Transform player;

    [Header("Chase")]
    [SerializeField] private float deathwallDelay = 1.0f;
    [SerializeField] private float killGracePeriod = 2.0f;
    [SerializeField] private float desiredGap = 6.0f;
    [SerializeField] private float minSpeed = 0.6f;
    [SerializeField] private float maxSpeed = 4.0f;
    [SerializeField] private float catchUpStrength = 0.6f;
    
    [Header("Safety")]
    [SerializeField] private bool clampDuringGrace = true;

    float spawnTime;

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if(!player) return;

        float elapsedTime = Time.time - spawnTime;

        // Delay Movement
        if(elapsedTime < deathwallDelay) return;

        float targetY = player.position.y - desiredGap;

        // Normal Chase
        float diff = targetY - transform.position.y;
        float speed = Mathf.Clamp(minSpeed + diff * catchUpStrength, minSpeed, maxSpeed);

        transform.position += Vector3.up * speed * Time.deltaTime;

        // Clamp During Grace so it doens't "Sit" on the Player
        if(clampDuringGrace && elapsedTime < killGracePeriod)
        {
            float maxY = targetY;
            if(transform.position.y > maxY)
                transform.position = new Vector3(transform.position.x, maxY, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player")) return;
        if(Time.time - spawnTime < killGracePeriod) return;

        if(GameManager.Instance != null)
            GameManager.Instance.KillPlayer("DeathWall");
    }

    private void Otay2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player")) return;
        if(Time.time - spawnTime < killGracePeriod) return;

        if(GameManager.Instance != null)
            GameManager.Instance.KillPlayer("DeathWall");
    }
}

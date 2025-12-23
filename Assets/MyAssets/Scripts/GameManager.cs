using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Camera as DeathLine")]
    [SerializeField] private float offscreenDeathMargin = 0.5f;

    // Getters (and Setters -> Private)
    public float Score { get; private set; }

    // References 
    Transform player;
    Camera cam;
    DeathWall deathWall;
    PlatformManager platformManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cam = Camera.main;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        player = playerGO ? playerGO.transform : null;

        var deathWallGO = GameObject.FindGameObjectWithTag("DeathWall");
        deathWall = deathWallGO.GetComponent<DeathWall>();

        platformManager = FindFirstObjectByType<PlatformManager>();

        // Push References into PlatformManager and Reset Platforms
        if(platformManager && player && deathWall && cam)
        {
            platformManager.Init(player, deathWall.transform, cam);
            platformManager.ResetRun();
        }
        else
            Debug.LogError("[GameManager] Missing References after Scene loaded.");
        
        Score = 0f;
    }

    void Update()
    {
        Score += Time.deltaTime;

        if (!player || !cam) return;

        float camBottomY = cam.transform.position.y - cam.orthographicSize;

        if (player.position.y < camBottomY - offscreenDeathMargin)
        {
            KillPlayer("Offscreen");
        }
    }

    public void KillPlayer(string reason = "")
    {
        // TODO: Freeze game, Show Death Screen, Animations...
        Debug.Log($"Player Died: {reason}, score={Score}.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

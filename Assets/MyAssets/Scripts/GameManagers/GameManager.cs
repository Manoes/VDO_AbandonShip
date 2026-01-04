using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Camera as DeathLine")]
    [SerializeField] private float offscreenDeathMargin = 0.5f;

    [Header("Score")]
    [SerializeField] private float scoreEventInterval = 0.1f;
    const string HighScoreKey = "HIGHSCORE";

    // Getters (and Setters -> Private)
    public float Score { get; private set; }
    public float HighScore { get; private set; }

    public UnityEvent<float, float> OnScoreChanged;

    // References 
    Transform player;
    Camera cam;
    DeathWall deathWall;
    PlatformManager platformManager;

    float scoreEventTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        HighScore = Mathf.Round(PlayerPrefs.GetFloat(HighScoreKey, 0f));
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Score = 0f;
        scoreEventTimer = scoreEventInterval;

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

        // Push Initial Score to UI
        OnScoreChanged?.Invoke(Score, HighScore);

        StartCoroutine(NotifyScoreNextFrame());
    }

    IEnumerator NotifyScoreNextFrame()
    {
        yield return null;  
        OnScoreChanged?.Invoke(Score, HighScore);
    }

    void Update()
    {
        Score += Time.deltaTime;

        // Update Highscore Life
        if(Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetFloat(HighScoreKey, HighScore);
        }

        // Update UI
        scoreEventTimer -= Time.deltaTime;
        if(scoreEventTimer <= 0f)
        {
            scoreEventTimer = Mathf.Max(0.02f, scoreEventInterval);
            OnScoreChanged?.Invoke(Score, HighScore);
        }

        if (!player || !cam) return;

        float camBottomY = cam.transform.position.y - cam.orthographicSize;

        if (player.position.y < camBottomY - offscreenDeathMargin)
            KillPlayer("Offscreen");
    }

    public void KillPlayer(string reason = "")
    {
        // TODO: Freeze game, Show Death Screen, Animations...
        Debug.Log($"Player Died: {reason}, score={Score}.");

        // Stop any Lingering Particles on the Current Player before Load
        if (player)
        {
            var jetpack = player.GetComponent<JetpackAbility>();
            if(jetpack) jetpack.StopVFX();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

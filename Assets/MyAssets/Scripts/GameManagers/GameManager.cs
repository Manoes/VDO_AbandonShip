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

    [Header("UI")]
    [SerializeField] private GameUIManager ui;
    [SerializeField] private float fallbackDeathAnimSeconds = 0.7f;

    // Getters (and Setters -> Private)
    public float Score { get; private set; }
    public float HighScore { get; private set; }

    public UnityEvent<float, float> OnScoreChanged;

    // References 
    Transform player;
    Camera cam;
    DeathWall deathWall;

    // Class References
    Health playerHealth;
    PlayerMovement playerMovement;
    JetpackAbility playerJetpack;
    Rigidbody2D playerRigidbody;
    Animator playerAnimator;
    PlayerAnimation playerAnimation;
    PlatformManager platformManager;

    float scoreEventTimer;
    bool isDyingOrGameOver;
    bool scoreRunning = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        HighScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
    }

    void OnEnable()
    {  
        SceneManager.sceneLoaded += OnSceneLoaded;

        BindUI(ui);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        BindUI(null);
    }

    void BindUI(GameUIManager newUI)
    {
        if(ui != null)
        {
            ui.RestartPressed -= RestartRun;
            ui.MenuPressed -= GoToMenu;
        }

        ui = newUI;

        if(ui != null)
        {
            ui.RestartPressed += RestartRun;
            ui.MenuPressed += GoToMenu;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindUI(FindFirstObjectByType<GameUIManager>());

        Score = 0f;
        scoreEventTimer = scoreEventInterval;

        cam = Camera.main;
        if(!cam)
            cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        player = playerGO ? playerGO.transform : null;

        isDyingOrGameOver = false;
        scoreRunning = true;

        if (ui)
            ui.HideGameOver();

        if (playerGO)
        {
            playerHealth = playerGO.GetComponent<Health>();
            playerMovement = playerGO.GetComponent<PlayerMovement>();
            playerJetpack = playerGO.GetComponent<JetpackAbility>();
            playerRigidbody = playerGO.GetComponent<Rigidbody2D>();
            playerAnimator = playerGO.GetComponent<Animator>();
            playerAnimation = playerGO.GetComponent<PlayerAnimation>();
        }

        var deathWallGO = GameObject.FindGameObjectWithTag("DeathWall");
        deathWall = deathWallGO.GetComponent<DeathWall>();

        platformManager = FindFirstObjectByType<PlatformManager>();

        if(playerRigidbody) playerRigidbody.simulated = false;
        if(playerMovement) playerMovement.enabled = false;
        if(playerJetpack) playerJetpack.enabled = false;

        // Push References into PlatformManager and Reset Platforms
        if(platformManager && player && deathWall && cam)
        {
            if(!cam) return;

            platformManager.Init(player, deathWall.transform, cam);
            platformManager.ResetRun();
        }

        StartCoroutine(EnablePlayerAfterPhysicsStep());

        // Push Initial Score to UI
        OnScoreChanged?.Invoke(Score, HighScore);
        StartCoroutine(NotifyScoreNextFrame());
    }

    IEnumerator EnablePlayerAfterPhysicsStep()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (playerRigidbody)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = true;
        }

        if(playerMovement) playerMovement.enabled = true;
        if(playerJetpack) playerJetpack.enabled = true;
    }

    IEnumerator NotifyScoreNextFrame()
    {
        yield return null;  
        OnScoreChanged?.Invoke(Score, HighScore);
    }

    void Update()
    {
        if(scoreRunning)
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
        // Stop any Lingering Particles on the Current Player before Load
        if (player)
        {
            var jetpack = player.GetComponent<JetpackAbility>();
            if(jetpack) jetpack.StopVFX();
        }

        OnPlayerDeath();
    }

    void OnPlayerDeath()
    {
        if(isDyingOrGameOver) return;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        isDyingOrGameOver = true;
        scoreRunning = false;

        if(playerJetpack) playerJetpack.StopVFX();
        if(playerMovement) playerMovement.enabled = false;
        if(playerJetpack) playerJetpack.enabled = false;

        if (playerRigidbody)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if(playerAnimation) playerAnimation.enabled = false;

        if(playerAnimator)
            playerAnimator.SetBool("Dead", true);
        
        // Wait for the Death Animation to Finish
        float timer = 0f;
        if (playerAnimator)
        {
            yield return null;
            while(timer < 3f)
            {
                var state = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if(state.IsName("Death") && state.normalizedTime >= 1f)
                    break;
                
                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
            yield return new WaitForSeconds(fallbackDeathAnimSeconds);
        
        if(ui) ui.ShowGameOver();
        Time.timeScale = 0f;
    }

    void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

using System.Collections;
using Unity.Mathematics;
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

    [Header("UI")]
    [SerializeField] private GameUIManager ui;

    [Header("Sound SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonPresSFX;

    public UnityEvent<float, float> OnScoreChanged = new UnityEvent<float, float>();

    // References 
    [SerializeField, InspectorReadOnly] Transform player;
    [SerializeField, InspectorReadOnly] Camera cam;
    [SerializeField, InspectorReadOnly] DeathWall deathWall;

    // Class References
    [SerializeField, InspectorReadOnly] Health playerHealth;
    [SerializeField, InspectorReadOnly] PlayerMovement playerMovement;
    [SerializeField, InspectorReadOnly] JetpackAbility playerJetpack;
    [SerializeField, InspectorReadOnly] Rigidbody2D playerRigidbody;
    [SerializeField, InspectorReadOnly] Animator playerAnimator;
    [SerializeField, InspectorReadOnly] PlayerAnimation playerAnimation;
    [SerializeField, InspectorReadOnly] PlatformManager platformManager;
    [SerializeField, InspectorReadOnly] CamShake camShake;

    float scoreEventTimer;
    bool isDyingOrGameOver;
    bool scoreRunning = true;
    bool inGameScene;   

    // Getters (and Setters -> Private)
    public float Score { get; private set; }
    public HighScoreService HighScores {get; private set;}
    public int FinalScore {get; private set;}
    public int RuntimeHighScore {get; private set;}
    public int SavedTopScore {get; private set;}    
    public bool PendingIsNewBest {get; private set;}  
    public bool PendingIsHighScore {get; private set;}   

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        OnScoreChanged ??= new UnityEvent<float, float>();
        HighScores = new HighScoreService();
    }

    void OnEnable()
    {           
        OnScoreChanged ??= new UnityEvent<float, float>();
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
        if (ui != null)
        {
            ui.RestartPressed -= RestartRun;
            ui.MenuPressed -= GoToMenu;
        }

        ui = newUI;

        if (ui != null)
        {
            ui.RestartPressed += RestartRun;
            ui.MenuPressed += GoToMenu;
        }
    }

    public void PlayUIButtonSFX()
    {        
        if(!audioSource)
            audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if(buttonPresSFX)
            audioSource.PlayOneShot(buttonPresSFX);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inGameScene = scene.name == "Game";
        
        // Always Stop Score unless we are in the Game Scene
        scoreRunning = inGameScene;
        isDyingOrGameOver = false;

        // Always Refresh Highscores from Disk
        HighScoreSystem.Reload();
        var top = HighScoreSystem.HighScoreService.GetTop();
        SavedTopScore = (top.Count > 0) ? top[0].score : 0;

        Score = 0f;
        scoreEventTimer = scoreEventInterval;
        RuntimeHighScore = SavedTopScore;
        PendingIsHighScore = false;
        PendingIsNewBest = false;

        BindUI(FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include));
        
        // Push Initial Score to UI
        if(ui)
            ui.SetScoreLabels(Score, RuntimeHighScore / 10f);
        
        OnScoreChanged?.Invoke(Score, RuntimeHighScore / 10f);
        StartCoroutine(NotifyScoreNextFrame());

        if(!inGameScene) return;

        cam = Camera.main;
        if (!cam)
        {            
            cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Include);            
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        player = playerGO ? playerGO.transform : null;

        isDyingOrGameOver = false;
        scoreRunning = true;

        if (ui)
            ui.HideGameOverUI();
        
        if(!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        if(camShake == null)
            camShake = FindFirstObjectByType<CamShake>(FindObjectsInactive.Include);

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

        if (playerRigidbody) playerRigidbody.simulated = false;
        if (playerMovement) playerMovement.enabled = false;
        if (playerJetpack) playerJetpack.enabled = false;

        // Push References into PlatformManager and Reset Platforms
        if (platformManager && player && deathWall && cam)
        {
            if (!cam) return;

            platformManager.Init(player, deathWall.transform, cam);
            platformManager.ResetRun();
        }

        StartCoroutine(EnablePlayerAfterPhysicsStep());
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

        if (playerMovement) playerMovement.enabled = true;
        if (playerJetpack) playerJetpack.enabled = true;
    }

    IEnumerator NotifyScoreNextFrame()
    {
        yield return null;
        OnScoreChanged?.Invoke(Score, RuntimeHighScore / 10f);
    }

    void Update()
    {
        if(!inGameScene) return;

        if (scoreRunning)
            Score += Time.deltaTime;

        int score10 = Mathf.FloorToInt(Score * 10f);
        RuntimeHighScore = Mathf.Max(SavedTopScore, score10);

        // Update Highscore Live
        if (score10 > RuntimeHighScore)
            RuntimeHighScore = score10;

        // Update UI
        scoreEventTimer -= Time.deltaTime;
        if (scoreEventTimer <= 0f)
        {
            scoreEventTimer = Mathf.Max(0.02f, scoreEventInterval);
            ui.SetScoreLabels(Score, RuntimeHighScore / 10f);
            OnScoreChanged?.Invoke(Score, RuntimeHighScore / 10f);
        }

        if (!player || !cam) return;

        float camBottomY = cam.transform.position.y - cam.orthographicSize;

        if (player.position.y < camBottomY - offscreenDeathMargin)
            KillPlayer("Offscreen");
    }

    public void KillPlayer(string reason = "")
    {    
        if (isDyingOrGameOver) return;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        isDyingOrGameOver = true;
        scoreRunning = false;

        if (playerJetpack) playerJetpack.StopVFX();
        if (playerMovement) playerMovement.enabled = false;
        if (playerJetpack) playerJetpack.enabled = false;

        if (playerRigidbody)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (playerAnimation) playerAnimation.enabled = false;

        if (playerAnimator)
            playerAnimator.SetBool("Dead", true);
        
        // Freeze Score + Store Final Displayed Score
        FinalScore = Mathf.FloorToInt(Score * 10f);
        PendingIsHighScore = HighScoreSystem.HighScoreService.IsHighScore(FinalScore);

        PendingIsNewBest = FinalScore > SavedTopScore;
        
        camShake?.Shake(2.5f, 3f);

        // Wait for the Death Animation to Finish
        float timer = 0f;
        if (playerAnimator)
        {
            yield return null;
            while (timer < 1f)
            {
                var state = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Death") && state.normalizedTime >= 1f)
                    break;

                timer += Time.deltaTime;
                yield return null;
            }
        }

        // Show UI
        if (ui)
        {
            ui.ShowGameOverUI();

            if (PendingIsHighScore && PendingIsNewBest)
                ui.ShowNewHighScore();
        }
        
        Time.timeScale = 0f;
    }

    public void SubmitHighScore(string name)
    {
        if(!PendingIsHighScore) return;

        HighScoreSystem.HighScoreService.AddHighScore(name, FinalScore);
        PendingIsHighScore = false;

        var top = HighScoreSystem.HighScoreService.GetTop();
        RuntimeHighScore = (top.Count > 0) ? top[0].score : RuntimeHighScore;

        OnScoreChanged?.Invoke(Score, RuntimeHighScore / 10f);
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

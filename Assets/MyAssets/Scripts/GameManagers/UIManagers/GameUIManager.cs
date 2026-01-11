using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    [Header("UI Animations")]
    [SerializeField] private float breatheOffset = 6f;
    [SerializeField] private float breatheDuration = 3.5f;

    Tween breatheTween;
    ArcadeNameEntryUI nameEntry;

    // --- Root Element ---
    private VisualElement root;

    // --- HUD - Visual Element Refs ---
    private VisualElement[] fuelSprites;
    private VisualElement[] healthSprites;

    // --- Hud - Label Refs ---
    private Label scoreLabel;
    private Label highscoreLabel;

    // --- Game Over - Container Ref ---
    private VisualElement gameOverContainer;
    private VisualElement gameOverHighScoreContainer;

    // --- Game Over - Labels ---
    private Label gameOverScoreLabel;
    private Label gameOverHighscoreLabel;

    // --- Game Over - New High Score Button ---
    private Button saveHighScoreButton;
    private VisualElement saveNewHighScoreContainer;
    private Label saveNewHighScoreLabel;
    private Label namePreviewLabel;
    private Button cancelNewHighScoreButton;
    private Button saveNewHighScoreButton;

    // --- Game Over - Button Actions ---
    public event Action RestartPressed;
    public event Action MenuPressed;

    void OnEnable()
    {
        var gameManager = GameManager.Instance;

        if (gameManager)
        {
            gameManager.OnScoreChanged.AddListener(SetScoreLabels);
            SetScoreLabels(gameManager.Score, gameManager.RuntimeHighScore / 10f);
        }
    }

    void OnDisable()
    {
        if(GameManager.Instance)
            GameManager.Instance.OnScoreChanged.RemoveListener(SetScoreLabels);
    }

    void Awake()
    {
        root = document.rootVisualElement;    

        nameEntry = GetComponent<ArcadeNameEntryUI>();
        if(nameEntry != null)
        {
            nameEntry.OnSubmit += HandleNameSubmit;

            nameEntry.BindNavBlockRoot(root);

            nameEntry.OnModeChanged += HandleNameEntryModeChanged;
        }

        CacheHUDRefs();
        CacheGameOverRefs();
        CacheNameEntryRefs();

        HideGameOverUI();
        HideNewHighScore();
        HideNameEntryUI();
    }

    void HandleNameEntryModeChanged(ArcadeNameEntryUI.Mode mode)
    {
        if(mode == ArcadeNameEntryUI.Mode.Editing)
        {
            SetUIFocusLocked(true);
        }
        else
        {
            SetUIFocusLocked(false);

            saveNewHighScoreButton?.Focus();
        }
    }

    void CacheHUDRefs()
    {
        // --- HUD - Container ---
        var hudContainer = root.Q<VisualElement>("HUDContainer");

        // --- HUD - Labels ---
        scoreLabel = hudContainer.Q<Label>("Score");
        highscoreLabel = hudContainer.Q<Label>("HighScore");

        // --- HUD - Fuel Sprites ---
        fuelSprites = new VisualElement[]
        {
            hudContainer.Q<VisualElement>("Jetpack_Fuel0"),
            hudContainer.Q<VisualElement>("Jetpack_Fuel1"),
            hudContainer.Q<VisualElement>("Jetpack_Fuel2"),
            hudContainer.Q<VisualElement>("Jetpack_Fuel3"),
            hudContainer.Q<VisualElement>("Jetpack_Fuel4"),
            hudContainer.Q<VisualElement>("Jetpack_Fuel5")
        };

        // --- HUD - Health Sprites ---
        healthSprites = new VisualElement[]
        {
            hudContainer.Q<VisualElement>("HealthIcon1"),
            hudContainer.Q<VisualElement>("HealthIcon2"),
            hudContainer.Q<VisualElement>("HealthIcon3")
        };
    }

    private void CacheGameOverRefs()
    {        
        gameOverContainer = root.Q<VisualElement>("GameOverContainer");

        // --- Game Over - Labels ---
        gameOverScoreLabel = gameOverContainer.Q<Label>("ScoreLabel");
        gameOverHighscoreLabel = gameOverContainer.Q<Label>("HighScoreLabel");
        saveNewHighScoreContainer = root.Q<VisualElement>("SaveNewHighScoreContainer");

        // --- Game Over - Buttons ---

        // Restart Game
        var restartButton = gameOverContainer.Q<Button>("Restart");
        if(restartButton != null)
            restartButton.clicked += () => 
            {   
                GameManager.Instance.PlayUIButtonSFX();
                RestartPressed?.Invoke();
            };

        // Back to Menu
        var menuButton = gameOverContainer.Q<Button>("BackToMenu");
        if(menuButton != null)
            menuButton.clicked += () => 
            {   
                GameManager.Instance.PlayUIButtonSFX();
                MenuPressed?.Invoke();
            };
        
        // Show New High Score UI
        saveHighScoreButton = gameOverContainer.Q<Button>("SaveHighScore");
        if(saveHighScoreButton != null)
            saveHighScoreButton.clicked += () => 
            {   
                GameManager.Instance.PlayUIButtonSFX();
                ShowNameEntryUI();
            };
        
        // Game Over Label
        var gameOverLabel = gameOverContainer.Q<Label>("GameOver");
        if(gameOverLabel != null)
            StartBeathing(gameOverLabel);
        
        // New High Score UI Elements (Show Only when New High Score Achieved)
        var gameOverNewHighScoreLabel = gameOverContainer.Q<Label>("NewHighScoreLabel"); 
        gameOverHighScoreContainer = gameOverContainer.Q<VisualElement>("NewHighScore");
        if(gameOverNewHighScoreLabel != null)
            StartBeathing(gameOverNewHighScoreLabel);   
    }

    private void CacheNameEntryRefs()
    {
        saveNewHighScoreContainer = root.Q<VisualElement>("SaveNewHighScoreContainer");
        if(saveNewHighScoreContainer == null) return;

        saveNewHighScoreLabel = saveNewHighScoreContainer.Q<Label>("NewHighScore");
        namePreviewLabel = saveNewHighScoreContainer.Q<Label>("NamePreview");
        
        saveNewHighScoreButton = saveNewHighScoreContainer.Q<Button>("SaveNewHighScore");
        cancelNewHighScoreButton = saveNewHighScoreContainer.Q<Button>("CancelHighScore"); 

        // Cancel New High Score Button        
        if(cancelNewHighScoreButton != null)
            cancelNewHighScoreButton.clicked += () =>
            {
                GameManager.Instance.PlayUIButtonSFX();
                HideNameEntryUI();
                ShowGameOverUI();
            };
        
        // Bind Name Entry UI to UI Elements
        if(nameEntry != null)
            nameEntry.BindUI(namePreviewLabel, saveNewHighScoreButton);
    }

    void StartBeathing(VisualElement element)
    {
        float y = 0f;

        breatheTween = DOTween.To(
            () => y,
            value => 
            {
                y = value;
                element.style.translate = new Translate(0f, y, 0f);
            },
            breatheOffset,
            breatheDuration
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true);
    }

    // --- HUD API ---

    // --- Fuel ---
    public void SetFuel(int currentFuel)
    {
        if(fuelSprites == null || fuelSprites.Length == 0) return;

        currentFuel = Mathf.Clamp(currentFuel, 0, fuelSprites.Length - 1);

        for(int i= 0; i < fuelSprites.Length; i++)
        {
            if(fuelSprites[i] == null) continue;
            fuelSprites[i].EnableInClassList("hide", i != currentFuel);
        }
    }

    // --- Health ---
    public void SetHealth(int currentHP)
    {
        if(healthSprites == null || healthSprites.Length == 0) return;

        int hp = Mathf.Clamp(currentHP, 0, healthSprites.Length);

        for(int i = 0; i < healthSprites.Length; i++)
        {
            if(healthSprites[i] == null) continue;
            healthSprites[i].EnableInClassList("hide", !(i < hp));
        }
    }

    // --- Score ---
    public void SetScoreLabels(float score, float highscore)
    {
        int score10 = Mathf.FloorToInt(score * 10);
        int highscore10 = Mathf.FloorToInt(highscore * 10);

        string scoreText = score10.ToString("D8");
        string highscoreText = highscore10.ToString("D8");

        if(scoreLabel != null) scoreLabel.text = scoreText;
        if(highscoreLabel != null) highscoreLabel.text = highscoreText;

        if(gameOverScoreLabel != null) gameOverScoreLabel.text = scoreText;
        if(gameOverHighscoreLabel != null) gameOverHighscoreLabel.text = highscoreText;

        // Save New High Score
        if(saveNewHighScoreLabel != null && GameManager.Instance != null)
            saveNewHighScoreLabel.text = GameManager.Instance.FinalScore.ToString("D8");   
    }

    // --- Game Over API ---

    public void ShowGameOverUI()
    {
        if(gameOverContainer != null)
            gameOverContainer.RemoveFromClassList("hide");
    }

    public void HideGameOverUI()
    {
        if(gameOverContainer != null)
            gameOverContainer.AddToClassList("hide");
    }

    public void ShowNewHighScore()
    {
        if(gameOverHighScoreContainer != null)
            gameOverHighScoreContainer.RemoveFromClassList("hide");
        
        if(saveHighScoreButton != null)
            saveHighScoreButton.RemoveFromClassList("hide");
    }

    public void HideNewHighScore()
    {
        if(gameOverHighScoreContainer != null)
            gameOverHighScoreContainer.AddToClassList("hide");
        
        if(saveHighScoreButton != null)
            saveHighScoreButton.AddToClassList("hide");
    }

    void SetUIFocusLocked(bool locked)
    {
        // Anything you don't want Highlighted during Name-Entry
        SetFocusabe(saveNewHighScoreButton, !locked);
        SetFocusabe(cancelNewHighScoreButton, !locked);
    }

    static void SetFocusabe(VisualElement element, bool focusable)
    {
        if (element == null) return;
        element.focusable = focusable;
        element.tabIndex = focusable ? 0 : -1;
    }

    void ShowNameEntryUI()
    {
        HideGameOverUI();

        if(saveNewHighScoreContainer != null)
            saveNewHighScoreContainer.RemoveFromClassList("hide");
        
        SetUIFocusLocked(true);

        if(nameEntry != null)
            nameEntry.SetActive(true);
    }

    void HideNameEntryUI()
    {
        if(saveNewHighScoreContainer != null)
            saveNewHighScoreContainer.AddToClassList("hide");
        
        if(nameEntry != null)
            nameEntry.SetActive(false);
        
        SetUIFocusLocked(false);
    }

    void HandleNameSubmit(string name)
    {
        // Save Score into JSON Table
        var gameManager = GameManager.Instance;
        if(gameManager != null)
            gameManager.SubmitHighScore(name);
        
        HideNameEntryUI();
        HideNewHighScore();
        ShowGameOverUI();
    }
}

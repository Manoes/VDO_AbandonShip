using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

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

    // --- Game Over - Labels ---
    private Label gameOverScoreLabel;
    private Label gameOverHighscoreLabel;

    // --- Game Over - Button Actions ---
    public event Action RestartPressed;
    public event Action MenuPressed;

    void OnEnable()
    {
        var gameManager = GameManager.Instance;

        if (gameManager)
        {
            gameManager.OnScoreChanged.AddListener(SetScoreLabels);
            SetScoreLabels(gameManager.Score, gameManager.HighScore);
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
        CacheHUDRefs();
        CacheGameOverRefs();
        HideGameOver();
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

        // --- Game Over - Buttons ---
        var restartButton = gameOverContainer.Q<Button>("Restart");
        if(restartButton != null)
            restartButton.clicked += () => RestartPressed?.Invoke();

        var menuButton = gameOverContainer.Q<Button>("BackToMenu");
        if(menuButton != null)
            menuButton.clicked += () => MenuPressed?.Invoke();
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
    }

    // --- Game Over API ---

    public void ShowGameOver()
    {
        if(gameOverContainer != null)
            gameOverContainer.RemoveFromClassList("hide");
    }

    public void HideGameOver()
    {
        if(gameOverContainer != null)
            gameOverContainer.AddToClassList("hide");
    }

}

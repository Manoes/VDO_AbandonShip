using UnityEngine;
using UnityEngine.UIElements;

public class HudController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    // --- Visual Element Refs ---
    private VisualElement[] fuelSprites;
    private VisualElement[] healthSprites;

    // --- Label Refs ---
    private Label scoreLabel;
    private Label highscoreLabel;

    void Awake()
    {
        var root = document.rootVisualElement;

        scoreLabel = root.Q<Label>("Score");
        highscoreLabel = root.Q<Label>("HighScore");

        // Fuel Sprites
        fuelSprites = new VisualElement[]
        {
            root.Q<VisualElement>("Jetpack_Fuel0"),
            root.Q<VisualElement>("Jetpack_Fuel1"),
            root.Q<VisualElement>("Jetpack_Fuel2"),
            root.Q<VisualElement>("Jetpack_Fuel3"),
            root.Q<VisualElement>("Jetpack_Fuel4"),
            root.Q<VisualElement>("Jetpack_Fuel5")
        };

        // Health Sprites
        healthSprites = new VisualElement[]
        {
            root.Q<VisualElement>("HealthIcon1"),
            root.Q<VisualElement>("HealthIcon2"),
            root.Q<VisualElement>("HealthIcon3")
        };
    }

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
        int score10 = Mathf.CeilToInt(score * 10);
        int highscore10 = Mathf.CeilToInt(highscore * 10);

        scoreLabel.text = score10.ToString("D8");
        highscoreLabel.text = highscore10.ToString("D8");
    }

}

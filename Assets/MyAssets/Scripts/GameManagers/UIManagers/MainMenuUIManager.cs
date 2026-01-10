using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    [Header("Sound SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonSFX;

    [Header("UI Animations")]
    [SerializeField] private float breatheOffset = 6f;
    [SerializeField] private float breatheDuration = 3.5f;
    [SerializeField] private Animator animator;

    Tween breatheTween;

    // Containers and Labels
    VisualElement mainMenuContainer;
    VisualElement highScoresContainer;

    // High Scores
    Label[] highScoreLabels;
    Label[] nameLabels;   


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        var root = document.rootVisualElement;

        // Main Menu Container
        mainMenuContainer = root.Q<VisualElement>("MainMenuContainer");

        // Top HighScores Container
        highScoresContainer = root.Q<VisualElement>("HighScoresContainer");

        // High Score Labels
        highScoreLabels = new Label[]
        {
            highScoresContainer.Q<Label>("TopHighScore1"),
            highScoresContainer.Q<Label>("TopHighScore2"),
            highScoresContainer.Q<Label>("TopHighScore3"),
            highScoresContainer.Q<Label>("TopHighScore4"),
            highScoresContainer.Q<Label>("TopHighScore5"),
        };

        // Name Labels
        nameLabels = new Label[]
        {
            highScoresContainer.Q<Label>("Name1"),
            highScoresContainer.Q<Label>("Name2"),
            highScoresContainer.Q<Label>("Name3"),
            highScoresContainer.Q<Label>("Name4"),
            highScoresContainer.Q<Label>("Name5"),
        };

        // Buttons - Main Menu
        var playButton = mainMenuContainer.Q<Button>("Play");
        var quitButton = mainMenuContainer.Q<Button>("QuitGame");
        var showHighScoresButton = mainMenuContainer.Q<Button>("HighScores");

        // Labels and Button - HighScores
        var backButton = highScoresContainer.Q<Button>("BackToMainMenu");

        if(playButton != null)
            playButton.clicked += () => StartCoroutine(PlayAndStartGame());
        if(quitButton != null)
            quitButton.clicked += () => StartCoroutine(PlayAndQuitGame());        
        if(showHighScoresButton != null)
            showHighScoresButton.clicked += () => ShowHighScores();
        if(backButton != null)
            backButton.clicked += () => BackToMainMenu();

        // Subtitle Breathe Animation
        var subtitle = root.Q<Label>("SubText");
        var highScoreTitle = highScoresContainer.Q<Label>("HighscoresTitle");
        if(subtitle != null || highScoreTitle != null)
        {
            StartBeathing(subtitle);
            StartBeathing(highScoreTitle);
        }

        ShowMainMenu();
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

    IEnumerator PlayAndStartGame()
    {
        PlayUISound();
        PlayExplodeShipAnimation();
        foreach(var clip in animator.runtimeAnimatorController.animationClips)
        {
            if(clip.name == "ExplodeShip")
            {
                yield return new WaitForSecondsRealtime(clip.length);
                break;
            }
        }
        StartGame();
    }

    void PlayExplodeShipAnimation()
    {
        if(animator != null)
            animator.Play("ExplodeShip");
    }

    void ShowMainMenu()
    {
        mainMenuContainer.RemoveFromClassList("hide");
        highScoresContainer.AddToClassList("hide");
    }

    void ShowHighScores()
    {
        PlayUISound();
        mainMenuContainer.AddToClassList("hide");
        highScoresContainer.RemoveFromClassList("hide");
        RefreshHighScoresUI();
    }

    void BackToMainMenu()
    {
        PlayUISound();
        mainMenuContainer.RemoveFromClassList("hide");
        highScoresContainer.AddToClassList("hide");
    }

    IEnumerator PlayAndQuitGame()
    {
        PlayUISound();
        yield return new WaitForSecondsRealtime(buttonSFX.length);
        QuitGame();
    }

    void PlayUISound()
    {        
        audioSource.PlayOneShot(buttonSFX);
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    void QuitGame()
    {        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshHighScoresUI()
    {
        HighScoreSystem.Reload();
        var top = HighScoreSystem.HighScoreService.GetTop();

        int rows = Mathf.Min(highScoreLabels.Length, nameLabels.Length);

        for(int i = 0; i < rows; i++)
        {
            string name = (i < top.Count) ? top[i].name : "---";
            int score = (i < top.Count) ? top[i].score : 0;

            if(nameLabels[i] != null) nameLabels[i].text = name;
            if(highScoreLabels[i] != null) highScoreLabels[i].text = score.ToString("D8");
        }
    }    
}

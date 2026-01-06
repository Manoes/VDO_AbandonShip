using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    void Awake()
    {
        var root = document.rootVisualElement;

        // Buttons
        var playButton = root.Q<Button>("Play");
        var quitButton = root.Q<Button>("QuitGame");

        if(playButton != null)
            playButton.clicked += StartGame;
        if(quitButton != null)
            quitButton.clicked += QuitGame;
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
}

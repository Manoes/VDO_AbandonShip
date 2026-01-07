using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    [Header("Sound SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonSFX;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        var root = document.rootVisualElement;

        // Buttons
        var playButton = root.Q<Button>("Play");
        var quitButton = root.Q<Button>("QuitGame");

        if(playButton != null)
            playButton.clicked += () => StartCoroutine(PlayAndStartGame());
        if(quitButton != null)
            quitButton.clicked += () => StartCoroutine(PlayAndQuitGame());
    }

    IEnumerator PlayAndStartGame()
    {
        PlayUISound();
        yield return new WaitForSecondsRealtime(buttonSFX.length);
        StartGame();
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

    
}

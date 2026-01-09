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

        // Subtitle Breathe Animation
        var subtitle = root.Q<Label>("SubText");
        if(subtitle != null)
            StartBeathing(subtitle);
    }

    void StartBeathing(VisualElement element)
    {
        breatheTween?.Kill();

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

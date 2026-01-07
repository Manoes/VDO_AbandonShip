using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Sound SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip jetpackSFX;    
    [SerializeField] private AudioClip hurtSFX;

    public void Init()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayHurtSound()
    {
        audioSource.PlayOneShot(hurtSFX);
    }

    public void StopAllSounds()
    {
        audioSource.Stop();
    }

    public void PlayJumpSound()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(jumpSFX);
    }

    public void PlayWalkSound()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(walkSFX);
    }

    public void PlayDeathSound()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(deathSFX);
    }

    public void PlayJetpackSound()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(jetpackSFX);
    }
}

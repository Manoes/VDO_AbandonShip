using Unity.Cinemachine;
using UnityEngine;

public class CamShake : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;

    CinemachineBasicMultiChannelPerlin noise;
    float shakeTimer;
    float shakeDuration;
    float startAmplitude;

    void Awake()
    {
        if(!cam) cam = GetComponent<CinemachineCamera>();
        noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void Update()
    {
        if(shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            noise.AmplitudeGain = Mathf.Lerp(startAmplitude, 0f, 1f - (shakeTimer / shakeDuration));
        }
        else if(noise.AmplitudeGain != 0f)
        {
            noise.AmplitudeGain = 0f;
        }
    }

    public void Shake(float duration, float amplitude)
    {
        if(!noise) return;

        shakeDuration = duration;
        shakeTimer = duration;
        startAmplitude = amplitude;

        noise.AmplitudeGain = amplitude;
    }
}

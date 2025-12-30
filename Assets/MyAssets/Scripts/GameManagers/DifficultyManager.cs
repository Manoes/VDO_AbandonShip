using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [SerializeField] private DifficultyProfile profile;

    void Awake()
    {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float GetFallingChance(float score)
    {
        if(!profile) return 0f;
        if(score < profile.FallingStartScore) return 0f;

        float time = (score - profile.FallingStartScore) / Mathf.Max(0.0001f, profile.FallingRampDuration);
        time = Mathf.Clamp01(time);
        return Mathf.Lerp(0f, profile.FallingMaxChance, time);
    }

    public float GetJetpackChance(float score)
    {
        if(!profile) return 0f;

        float time = Mathf.Clamp01(score / Mathf.Max(0.0001f, profile.JetpackChanceDecayScore));
        return Mathf.Lerp(profile.JetpackChanceStart, profile.JetpackChanceMin, time);
    }

    public void GetGapRange(float score, out float minGap, out float maxGap)
    {
        if(!profile) { minGap = 3f; maxGap = 5.5f; return; }

        float time = Mathf.Clamp01(score / Mathf.Max(0.0001f, profile.GapRampScore));
        minGap = Mathf.Lerp(profile.GapMinStart, profile.GapMinEnd, time);
        maxGap = Mathf.Lerp(profile.GapMaxStart, profile.GapMaxEnd, time);
    }

    public float GetSpikesChance(float score)
    {
        if(!profile) return 0f;
        if(score < profile.SpikesStartScore) return 0f;

        float time = (score - profile.SpikesStartScore) / Mathf.Max(0.0001f, profile.SpikesRampDuration);
        time = Mathf.Clamp01(time);
        return Mathf.Lerp(0f, profile.SpikesMaxChance, time);
    }

    public float GetLaserChance(float score)
    {
        if(!profile) return 0f;
        if(score < profile.LaserStartScore) return 0f;

        float time = (score - profile.LaserStartScore) / Mathf.Max(0.0001f, profile.LaserRampDuration);
        time = Mathf.Clamp01(time);
        return Mathf.Lerp(0f, profile.LaserMaxChance, time);
    }

    public bool SpikesEnabled(float score) => profile && score >= profile.SpikesStartScore;
    public bool LaserEnabled(float score) => profile && score >= profile.LaserStartScore;
}

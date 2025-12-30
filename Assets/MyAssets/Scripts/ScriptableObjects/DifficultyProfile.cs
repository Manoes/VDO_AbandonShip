using UnityEngine;

[CreateAssetMenu(menuName = "Game/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Falling Platform")]
    [SerializeField] private float fallingStartScore = 15f;
    [SerializeField] private float fallingRampDuration = 45f;
    [SerializeField] private float fallingMaxChance = 0.35f;

    [Header("Jetpack Pickup Chance (Decreasing with Score)")]
    [SerializeField] private float jetpackChanceStart = 0.70f;
    [SerializeField] private float jetpackChanceMin = 0.20f;
    [SerializeField] private float jetpackChanceDecayScore = 50f;

    [Header("Platform Gaps (Increasing Slowly)")]
    [SerializeField] private float gapMinStart = 3.0f;
    [SerializeField] private float gapMaxStart = 5.5f;
    [SerializeField] private float gapMinEnd = 4.0f;
    [SerializeField] private float gapMaxEnd = 7.0f;
    [SerializeField] private float gapRampScore = 120f;

    [Header("Spikes")]
    [SerializeField] private float spikesStartScore = 80f;
    [SerializeField] private float spikesRampDuration = 60f;
    [SerializeField] private float spikesMaxChance = 0.35f;

    [Header("Laser")]
    [SerializeField] private float laserStartScore = 100f;
    [SerializeField] private float laserRampDuration = 80f;
    [SerializeField] private float laserMaxChance = 0.20f;

    public float FallingStartScore => fallingStartScore; 
    public float FallingRampDuration => fallingRampDuration; 
    public float FallingMaxChance => fallingMaxChance; 
    public float JetpackChanceStart => jetpackChanceStart; 
    public float JetpackChanceMin => jetpackChanceMin; 
    public float JetpackChanceDecayScore => jetpackChanceDecayScore;
    public float GapMinStart => gapMinStart; 
    public float GapMaxStart => gapMaxStart; 
    public float GapMinEnd => gapMinEnd;
    public float GapMaxEnd => gapMaxEnd; 
    public float GapRampScore => gapRampScore; 
    public float SpikesStartScore => spikesStartScore; 
    public float SpikesRampDuration => spikesRampDuration; 
    public float SpikesMaxChance => spikesMaxChance;     
    public float LaserStartScore => laserStartScore;
    public float LaserRampDuration => laserRampDuration;
    public float LaserMaxChance => laserMaxChance;
    
}

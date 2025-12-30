using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invulnSeconds = 0.6f;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public UnityEvent<int, int> OnHealthChanged;
    public UnityEvent OnDeath;

    float invulnTimer;

    void Awake()
    {
        CurrentHP = maxHP;
    }

    void Update()
    {
        if(invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    public void ResetHealth()
    {
        CurrentHP = maxHP;
        invulnTimer = 0f;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public bool TakeDamage(int amount)
    {
        if(IsDead) return false;
        if(invulnTimer > 0f) return false;

        CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(1, amount));
        invulnTimer = invulnSeconds;

        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if(CurrentHP <= 0)
            OnDeath?.Invoke();

        return true; 
    }

    public void Heal(int amount)
    {
        if(IsDead) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(1, amount));
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }
}

using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invulnSeconds = 0.6f;
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Damage Visual")]
    [SerializeField] private SpriteRenderer[] renderersToBlink;
    [SerializeField] private bool flashWhileOnHit = true;
    [SerializeField] private float whiteFlashTime = 0.06f;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;

    float invulnTimer;
    bool invuln;
    Coroutine blinkCo;

    void Awake()
    {
        CurrentHP = maxHP;
        if(renderersToBlink == null || renderersToBlink.Length == 0)
            renderersToBlink = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if(invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    public void ResetHealth()
    {
        CurrentHP = maxHP;
        invulnTimer = 0f;
        OnHealthChanged?.Invoke(CurrentHP);
    }

    public bool TakeDamage(int amount)
    {
        if(IsDead) return false;
        if(amount <= 0) return false;
        if(invulnTimer > 0f || invuln) return false;

        CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(1, amount));
        invulnTimer = invulnSeconds;

        OnHealthChanged?.Invoke(CurrentHP);

        Debug.Log($"[Health] Player taking Damage for {amount} Damage. {CurrentHP}/{maxHP}");

        if(CurrentHP <= 0)
            OnDeath?.Invoke();

        if(blinkCo != null) StopCoroutine(blinkCo);
        blinkCo = StartCoroutine(InvulnBlink());
        return true; 
    }

    IEnumerator InvulnBlink()
    {
        invuln = true;

        // Optional White Flash
        if (flashWhileOnHit)
        {
            var originalColors = new Color[renderersToBlink.Length];
            for(int i = 0; i < renderersToBlink.Length; i++)
            {
                if(!renderersToBlink[i]) continue;
                originalColors[i] = renderersToBlink[i].color;
                renderersToBlink[i].color = Color.white;
            }
            yield return new WaitForSeconds(whiteFlashTime);
            for(int i = 0; i < renderersToBlink.Length; i++)
            {
                if(!renderersToBlink[i]) continue;
                renderersToBlink[i].color = originalColors[i];
            }
        }

        float timer = 0f;
        bool visible = true;

        while(timer < invulnSeconds)
        {
            visible = !visible;
            for(int i = 0; i < renderersToBlink.Length; i++)
                if(renderersToBlink[i])
                    renderersToBlink[i].enabled = visible;
            
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        // Restore 
        for(int i = 0; i < renderersToBlink.Length; i++)
            if(renderersToBlink[i])
                renderersToBlink[i].enabled = true;
        
        invuln = false;
        blinkCo = null;
    }

    public void Heal(int amount)
    {
        if(IsDead) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(1, amount));
        OnHealthChanged?.Invoke(CurrentHP);
    }
}

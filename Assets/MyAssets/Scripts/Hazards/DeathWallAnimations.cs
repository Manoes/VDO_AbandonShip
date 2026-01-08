using System.ComponentModel.Design;
using UnityEngine;

public class DeathWallAnimations : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator[] explosionType1Animator;
    [SerializeField] private Animator[] explosionType2Animator;

    void Awake()
    {        
        for (int i = 0; i < explosionType1Animator.Length; i++)
        {
            if (explosionType1Animator[i] == null)
                explosionType1Animator[i] = GetComponent<Animator>();
            
            explosionType1Animator[i].Play("Explosion_Type 1", -1, Random.Range(0.1f, 3f));
        }

        for (int i = 0; i < explosionType2Animator.Length; i++)
        {
            if (explosionType2Animator[i] == null)
                explosionType2Animator[i] = GetComponent<Animator>();
            
            explosionType2Animator[i].Play("Explosion_Type 2", -1, Random.Range(0.1f, 3f));
        }
    }
}

using UnityEngine;

public class LaserHazard : MonoBehaviour
{    
    [SerializeField] private int damage = 1;

    [Header("References")]
    [SerializeField] private Transform beam;
    [SerializeField] private Transform leftEmitter;
    [SerializeField] private Transform rightEmitter;

    public void SetLengthWorld(float lengthWorld)
    {
        // Beam Centered
        if(beam)
            beam.localScale = new Vector3(lengthWorld, beam.localScale.y, beam.localScale.z);

        // Emitters snap to edges
        if(leftEmitter) 
            leftEmitter.localPosition = new Vector3(-lengthWorld * 0.5f, 0f, 0f);
        
        if(rightEmitter) 
            rightEmitter.localPosition = new Vector3(lengthWorld * 0.5f, 0f, 0f);
    }
}

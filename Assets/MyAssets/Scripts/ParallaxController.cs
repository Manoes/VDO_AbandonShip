using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class ParallaxController : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [Tooltip("0 = Move with Camera, 1 = Not Move")]
    [SerializeField] private float parallaxEffect;
    [SerializeField] private float wrapMargin = 0.5f;
    
    private float startPos, height, camStartY;

    void Awake()
    {
        if(!cam) cam = Camera.main.transform;
    }

    void Start()
    {
        startPos = transform.position.y;
        camStartY = cam.position.y;

        height = GetWorldHeight(gameObject);

        if(height < 0.0001f) height = 0f;
    }

    void FixedUpdate()
    {
        float camY = cam.position.y;

        // Parallax: Layer follows a Fraction of the Camera's Displacements from Start        
        float camDelta = camY - camStartY;
        float layerY = startPos + camDelta * (1f - parallaxEffect);

        transform.position = new Vector3(transform.position.x, layerY, transform.position.z);

        if(height <= 0f) return;

        float difference = camY - transform.position.y;

        // If Background has reached the end of its length, Adjust
        if(difference > (height * 0.5f + wrapMargin))
        {
            startPos += height;
        }
        else if(difference < -(height * 0.5f + wrapMargin))
        {
            startPos -= height;
        }
    }

    static float GetWorldHeight(GameObject root)
    {
        // Sprite: bounds are world-space
        var sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr) return sr.bounds.size.y;

        // Tilemap: localBounds are local-space -> convert using lossyScale
        var tm = root.GetComponentInChildren<Tilemap>();
        if (tm)
        {
            tm.CompressBounds();
            float localY = tm.localBounds.size.y;
            float scaleY = tm.transform.lossyScale.y;
            return Mathf.Abs(localY * scaleY);
        }

        // Any renderer fallback
        var r = root.GetComponentInChildren<Renderer>();
        if (r) return r.bounds.size.y;

        return 0f;
    }
}

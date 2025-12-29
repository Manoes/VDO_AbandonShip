using System;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [Serializable]
    public class Layer
    {
        public Transform transform;
        [Range(0f, 1f)] 
        public float factor = 0.25f;
        public bool effectX = false;
        public bool effectY = true;
    }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Layer[] layers;

    private Vector3 lastCamPos;

    void Awake()
    {
        if(!cameraTransform) cameraTransform = Camera.main.transform;
        lastCamPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 camDelta = cameraTransform.position - lastCamPos;
        for(int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if(!layer.transform) return;

            float directionX = layer.effectX ? camDelta.x * layer.factor : 0f;
            float directionY = layer.effectY ? camDelta.y * layer.factor : 0f;

            layer.transform.position += new Vector3(directionX, directionY);
        }

        lastCamPos = cameraTransform.position;
    }
}

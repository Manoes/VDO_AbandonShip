using Unity.Cinemachine;
using UnityEngine;

public class CamOnlyFollowUp : CinemachineExtension
{
    [SerializeField] private Transform target;
    [SerializeField] private float yOffset = 1.5f;
    [SerializeField] private bool lockX = true;
    [SerializeField] private float fixedX = 0f;

    float minY;
    bool initialized;

    protected override void Awake()
    {
        base.Awake();
        if(lockX) fixedX = transform.position.x;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if(stage != CinemachineCore.Stage.Body) return;
        if(!target) return;

        if (!initialized)
        {
            minY = state.RawPosition.y;
            initialized = true;
        }

        float desiredY = target.position.y + yOffset;
        if(desiredY > minY) minY = desiredY;

        Vector3 position = state.RawPosition;
        position.y = minY;
        if(lockX) position.x = fixedX;

        state.RawPosition = position;
    }
}

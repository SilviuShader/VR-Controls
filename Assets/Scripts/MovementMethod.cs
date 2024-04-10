using UnityEngine;
using Valve.VR;

public class MovementMethod : MonoBehaviour
{
    private const float                  MAX_IN_RANGE_ANGLE = 45.0f;
                                         
    public        Transform              InputSpace         = default;
    public        Transform              RotateTransform    = default;
    public        LayerMask              TargetsLayerMask   = -1;
    public        float                  MaxRaycastDistance = 1000.0f;
    [Range(1.0f, 90.0f)]
    public        float                  TurnAngle          = 15.0f;

    [SerializeField]
    private       SteamVR_Action_Boolean _turnLeftAction    = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TurnLeft");
    [SerializeField]
    private       SteamVR_Action_Boolean _turnRightAction   = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TurnRight");


    public Transform GetInputSpace()      => InputSpace ? InputSpace : transform;
    public Transform GetRotateTransform() => RotateTransform ? RotateTransform : transform;

    public Vector3 InputSpaceRightXZ()
    {
        var right = GetInputSpace().right;
        right.y = 0f;
        right.Normalize();

        return right;
    }

    public Vector3 InputSpaceForwardXZ()
    {
        var forward = GetInputSpace().forward;
        forward.y = 0f;
        forward.Normalize();

        return forward;
    }

    public bool RaycastReferenceObject(Vector3 position, Vector3 forward, out Vector3 center, out float curvature)
    {
        center = Vector3.zero;
        curvature = 0.0f;

        var interestObjects = InterestObject.InterestObjects;
        if (interestObjects.Count <= 0)
            return false;

        var bestObject = interestObjects[0];
        var bestCosine = -1.0f;

        foreach (var obj in interestObjects)
        {
            var cosine = CosineBetweenViewer(position, forward, obj.transform.position);
            if (bestCosine < cosine)
            {
                bestObject = obj;
                bestCosine = cosine;
            }
        }

        if (bestCosine >= Mathf.Cos(MAX_IN_RANGE_ANGLE * Mathf.Deg2Rad))
        {
            center = bestObject.transform.position;
            curvature = 1.0f - (Mathf.Acos(bestCosine) / (MAX_IN_RANGE_ANGLE * Mathf.Deg2Rad));
            return true;
        }

        return false;
    }

    public void UpdateTurns()
    {
        if(_turnLeftAction.GetStateUp(SteamVR_Input_Sources.LeftHand))
            GetRotateTransform().Rotate(0.0f, -TurnAngle, 0.0f);

        if (_turnRightAction.GetStateUp(SteamVR_Input_Sources.LeftHand))
            GetRotateTransform().Rotate(0.0f, TurnAngle, 0.0f);
    }

    private float CosineBetweenViewer(Vector3 viewerPos, Vector3 viewerForward, Vector3 objPos)
    {
        var direction = objPos - viewerPos;
        direction.y = 0;
        direction.Normalize();

        viewerForward.y = 0;
        viewerForward.Normalize();

        return Vector3.Dot(direction, viewerForward);
    }
}

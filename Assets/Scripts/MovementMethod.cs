using UnityEngine;

public class MovementMethod : MonoBehaviour
{
    private const float     DIST_BETWEEN_SURFACE_SAMPLES = 0.01f;
    private const float     MAX_IN_RANGE_ANGLE           = 45.0f;

    public        Transform InputSpace                   = default;
    public        Transform RotateTransform              = default;
    public        LayerMask TargetsLayerMask             = -1;
    public        float     MaxRaycastDistance           = 1000.0f;


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

    float CosineBetweenViewer(Vector3 viewerPos, Vector3 viewerForward, Vector3 objPos)
    {
        var direction = objPos - viewerPos;
        direction.y = 0;
        direction.Normalize();

        viewerForward.y = 0;
        viewerForward.Normalize();

        return Vector3.Dot(direction, viewerForward);
    }

    //void Estimate3DCircle(Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 c, out float radius)
    //{
    //    var v1 = p2 - p1;
    //    var v2 = p3 - p1;
    //    float v1v1, v2v2, v1v2;
    //    v1v1 = Vector3.Dot(v1, v1);
    //    v2v2 = Vector3.Dot(v2, v2);
    //    v1v2 = Vector3.Dot(v1, v2);

    //    float b = 0.5f / (v1v1 * v2v2 - v1v2 * v1v2);
    //    float k1 = b * v2v2 * (v1v1 - v1v2);
    //    float k2 = b * v1v1 * (v2v2 - v1v2);
    //    c = p1 + v1 * k1 + v2 * k2; // center

    //    radius = (c - p1).magnitude;
    //}
}

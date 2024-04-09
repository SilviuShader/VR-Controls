using UnityEngine;

public class MovementMethod : MonoBehaviour
{
    private const float     DIST_BETWEEN_SURFACE_SAMPLES = 0.1f;

    public        Transform InputSpace         = default;
    public        Transform RotateTransform    = default;
    public        LayerMask TargetsLayerMask   = -1;
    public        float     MaxRaycastDistance = 1000.0f;


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

    public bool RaycastReferenceObject(Vector3 position, Vector3 forward, out Vector3 center)
    {
        center = Vector3.zero;
        if (Physics.Raycast(position, forward, out var hitInfo, MaxRaycastDistance, TargetsLayerMask))
        {
            var forward1 = Quaternion.AngleAxis(-Mathf.Atan(DIST_BETWEEN_SURFACE_SAMPLES / hitInfo.distance) * Mathf.Rad2Deg, Vector3.up) * forward;
            var forward2 = Quaternion.AngleAxis(Mathf.Atan(DIST_BETWEEN_SURFACE_SAMPLES / hitInfo.distance) * Mathf.Rad2Deg, Vector3.up) * forward;

            if (Physics.Raycast(position, forward1, out var hitInfo1, MaxRaycastDistance, TargetsLayerMask) &&
                Physics.Raycast(position, forward2, out var hitInfo2, MaxRaycastDistance, TargetsLayerMask))
            {
                if (hitInfo1.transform == hitInfo.transform &&
                    hitInfo2.transform == hitInfo.transform)
                {
                    var d1 = hitInfo.point - hitInfo1.point;
                    var d2 = hitInfo2.point - hitInfo.point;

                    if (Mathf.Approximately(Vector3.Dot(d1.normalized, d2.normalized), 1.0f))
                        return false;

                    Estimate3DCircle(hitInfo1.point, hitInfo.point, hitInfo2.point, out center, out _);
                    return true;
                }

                return false;
            }
        }

        return false;
    }

    void Estimate3DCircle(Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 c, out float radius)
    {
        var v1 = p2 - p1;
        var v2 = p3 - p1;
        float v1v1, v2v2, v1v2;
        v1v1 = Vector3.Dot(v1, v1);
        v2v2 = Vector3.Dot(v2, v2);
        v1v2 = Vector3.Dot(v1, v2);

        float b = 0.5f / (v1v1 * v2v2 - v1v2 * v1v2);
        float k1 = b * v2v2 * (v1v1 - v1v2);
        float k2 = b * v1v1 * (v2v2 - v1v2);
        c = p1 + v1 * k1 + v2 * k2; // center

        radius = (c - p1).magnitude;
    }
}

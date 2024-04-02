using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class CircularContinuousMovement : MonoBehaviour
{
    private const float                  DIST_BETWEEN_SURFACE_SAMPLES = 0.1f;

    [SerializeField]
    private       SteamVR_Action_Vector2 _moveAction                  = SteamVR_Input.GetVector2Action("Move");
    [SerializeField]                                                  
    private       Transform              _inputSpace                  = default;
    [SerializeField]                                                  
    private       Transform              _rotateTransform             = default;
    [SerializeField]                                                  
    private       float                  _maxSpeed                    = 10.0f;
    [SerializeField]                                                  
    private       float                  _maxAcceleration             = 10.0f;
    [SerializeField]                                                  
    private       LayerMask              _layerMask                   = -1;
    [SerializeField]                                                  
    private       float                  _maxRaycastDistance          = 1000.0f;
                                                                      
    private       Rigidbody              _rigidbody;                  
                                                                      
    private       Vector2                _currentBodyPosition         = Vector3.zero;
    private       float                  _deltaAngle;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentBodyPosition = new Vector2(transform.position.x, transform.position.z);
    }

    private void Update()
    {
        var inputSpace = transform;
        if (_inputSpace)
            inputSpace = _inputSpace;
        
        var inputAxis = Vector2.ClampMagnitude(_moveAction[SteamVR_Input_Sources.RightHand].axis, 1.0f);

        var forward = inputSpace.forward;
        forward.y = 0f;
        forward.Normalize();

        var right = inputSpace.right;
        right.y = 0f;
        right.Normalize();

        Vector2 displacement;
        var deltaAngle = 0.0f;
        var displacementMagnitude = _maxSpeed * Time.deltaTime;

        _currentBodyPosition = ProjectXZ(_rigidbody.position);

        if (RaycastReferenceObject(inputSpace.position, forward, out var targetPoint))
        {
            CurvatureDisplacement(
                ProjectXZ(targetPoint),
                _currentBodyPosition,
                displacementMagnitude,
                inputAxis.x,
                inputAxis.y,
                out displacement,
                out deltaAngle);
        }
        else
        {
            displacement = (ProjectXZ(right) * inputAxis.x + ProjectXZ(forward) * inputAxis.y) * displacementMagnitude;
        }

        _currentBodyPosition += displacement;
        _deltaAngle += deltaAngle;
    }

    private void FixedUpdate()
    {
        var rotateTransform = transform;
        if (_rotateTransform)
            rotateTransform = _rotateTransform;
        
        _rigidbody.MovePosition(UnProjectXZ(_currentBodyPosition, _rigidbody.position.y));
        rotateTransform.Rotate(Vector3.up, -_deltaAngle);
        _deltaAngle = 0.0f;
    }

    private void CurvatureDisplacement(
        Vector2 center, 
        Vector2 position, 
        float movementMagnitude, 
        float curvatureFactor, 
        float forwardFactor,
        out Vector2 displacement, out float rotationAngle)
    {
        var fromCenter = position - center;
        var radius = fromCenter.magnitude;
        
        var angle = (movementMagnitude / radius) * curvatureFactor;

        rotationAngle = angle * Mathf.Rad2Deg;
        fromCenter = Quaternion.AngleAxis(rotationAngle, Vector3.forward) * fromCenter;

        var rotatedPosition = center + fromCenter;

        var greatCircleDist = angle * radius;
        var remainingDist = Mathf.Clamp(movementMagnitude - greatCircleDist, 0.0f, movementMagnitude);

        displacement = (rotatedPosition - remainingDist * fromCenter.normalized * forwardFactor) - position;
    }

    private bool RaycastReferenceObject(Vector3 position, Vector3 forward, out Vector3 center)
    {
        center = Vector3.zero;
        if (Physics.Raycast(position, forward, out var hitInfo, _maxRaycastDistance, _layerMask))
        {
            var forward1 = Quaternion.AngleAxis(-Mathf.Atan(DIST_BETWEEN_SURFACE_SAMPLES / hitInfo.distance) * Mathf.Rad2Deg, Vector3.up) * forward;
            var forward2 = Quaternion.AngleAxis(Mathf.Atan(DIST_BETWEEN_SURFACE_SAMPLES / hitInfo.distance) * Mathf.Rad2Deg, Vector3.up) * forward;

            if (Physics.Raycast(position, forward1, out var hitInfo1, _maxRaycastDistance, _layerMask) &&
                Physics.Raycast(position, forward2, out var hitInfo2, _maxRaycastDistance, _layerMask))
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

    private static Vector2 ProjectXZ(Vector3 vec) => new(vec.x, vec.z);
    private static Vector3 UnProjectXZ(Vector2 vec, float y) => new(vec.x, y, vec.y);
}

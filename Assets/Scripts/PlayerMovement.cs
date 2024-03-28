using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private SteamVR_Action_Vector2 _moveAction          = SteamVR_Input.GetVector2Action("Move");
    [SerializeField]                                    
    private Transform              _inputSpace          = default;
    [SerializeField]
    private Transform              _rotateTransform     = default;
    [SerializeField]                                    
    private float                  _maxSpeed            = 10.0f;
    [SerializeField]                                    
    private float                  _maxAcceleration     = 10.0f;
    [SerializeField]
    private LayerMask              _layerMask         = -1;
    [SerializeField]
    private float                  _maxRaycastDistance  = 1000.0f;
                                                        
    private Vector2                _inputAxis           = Vector2.zero;
    //private Vector2                _desiredVelocity     = Vector3.zero;
    //private Vector2                _velocity            = Vector3.zero;
    private Rigidbody              _rigidbody;
    
    private Vector2                _currentBodyPosition = Vector3.zero;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentBodyPosition = new Vector2(transform.position.x, transform.position.z);
    }

    private void Update()
    {
        var inputSpace = transform;
        var rotateTransform = transform;
        if (_inputSpace)
            inputSpace = _inputSpace;
        if (_rotateTransform)
            rotateTransform = _rotateTransform;

        _inputAxis = Vector2.ClampMagnitude(_moveAction[SteamVR_Input_Sources.RightHand].axis, 1.0f);

        var forward = inputSpace.forward;
        forward.y = 0f;
        forward.Normalize();

        var right = inputSpace.right;
        right.y = 0f;
        right.Normalize();

        var displacement = Vector2.zero;
        var deltaAngle = 0.0f;
        var displacementMagnitude = _maxSpeed * Time.deltaTime;

        if (RaycastReferenceObject(inputSpace.position, forward, out var targetPoint))
        //if (Physics.Raycast(inputSpace.position, forward, out var hitInfo, _maxRaycastDistance, _layerMask))
        {
            //var targetPoint = hitInfo.point;

            //RaycastReferenceObject(inputSpace.position, forward);

            CurvatureDisplacement(
                new Vector2(targetPoint.x, targetPoint.z),
                _currentBodyPosition,
                displacementMagnitude,
                _inputAxis.x,
                _inputAxis.y,
                out displacement,
                out deltaAngle);
        }
        else
        {
            displacement = (new Vector2(right.x, right.z) * _inputAxis.x + new Vector2(forward.x, forward.z) * _inputAxis.y) * displacementMagnitude;
        }

        //_desiredVelocity = (_inputAxis.x * new Vector2(right.x, right.z) + _inputAxis.y * new Vector2(forward.x, forward.z)) * _maxSpeed;
        //var maxSpeedChange = _maxAcceleration * Time.deltaTime;
        //_velocity = Vector3.MoveTowards(_velocity, _desiredVelocity, maxSpeedChange);

        _currentBodyPosition += displacement;

        rotateTransform.Rotate(Vector3.up, -deltaAngle);
        _rigidbody.MovePosition(new Vector3(_currentBodyPosition.x, 0.0f, _currentBodyPosition.y));
    }

    //private void FixedUpdate() =>
    //    _currentBodyPosition = _rigidbody.position; // TODO: Use vec2

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
        var deltaAngle = 1.0f; // TODO: Proper math here
        if (Physics.Raycast(position, forward, out var hitInfo, _maxRaycastDistance, _layerMask))
        {
            
            var forward1 = Quaternion.AngleAxis(-deltaAngle / hitInfo.distance, Vector3.up) * forward;
            var forward2 = Quaternion.AngleAxis(deltaAngle / hitInfo.distance, Vector3.up) * forward;

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

}

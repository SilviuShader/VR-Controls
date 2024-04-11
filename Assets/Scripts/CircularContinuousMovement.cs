using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class CircularContinuousMovement : MovementMethod
{
    [SerializeField]
    private       SteamVR_Action_Vector2 _moveAction                  = SteamVR_Input.GetVector2Action("Move");
    [SerializeField]                                                  
    private       float                  _maxSpeed                    = 10.0f;
    [SerializeField]                                                  
    private       float                  _maxAcceleration             = 10.0f;
                                                                      
    private       Rigidbody              _rigidbody;                  
                                                                      
    private       Vector2                _currentBodyPosition         = Vector3.zero;
    private       float                  _deltaAngle;
    private       float                  _speed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentBodyPosition = new Vector2(transform.position.x, transform.position.z);
    }

    private void Start() =>
        _rigidbody.constraints = ((~RigidbodyConstraints.FreezePosition) & _rigidbody.constraints);
    

    private void UpdateStep()
    {
        var rotateTransform = GetRotateTransform();
        var inputSpace = GetInputSpace();
        var inputAxis = Vector2.ClampMagnitude(_moveAction[SteamVR_Input_Sources.RightHand].axis, 1.0f);

        var forward = InputSpaceForwardXZ();
        var right = InputSpaceRightXZ();

        var deltaAngle = 0.0f;

        var previousPosition = _currentBodyPosition;
        _currentBodyPosition = MathHelper.ProjectXZ(_rigidbody.position);
        rotateTransform.rotation = _rigidbody.rotation;

        var previousDisplacement = _currentBodyPosition - previousPosition;
        if (previousDisplacement.magnitude >= 0.001f) // TODO: Hard-coded constant
        {
            //_speed *= Vector2.Dot(MathHelper.ProjectXZ(previousDisplacement.normalized), inputAxis.normalized);
            //if (_speed <= 0.0f)
            //    _speed = 0.0f;
        }

        _speed = Mathf.MoveTowards(_speed, _maxSpeed * inputAxis.magnitude, _maxAcceleration * Time.deltaTime);
        var displacementMagnitude = _speed * Time.deltaTime;

        var straightDisplacement = (MathHelper.ProjectXZ(right) * inputAxis.x + MathHelper.ProjectXZ(forward) * inputAxis.y) * displacementMagnitude; ;
        var curvedDisplacement = straightDisplacement;

        if (RaycastReferenceObject(inputSpace.position, forward, out var targetPoint, out var curvature))
        {
            Curvature.CurvatureDisplacement(
                MathHelper.ProjectXZ(targetPoint),
                _currentBodyPosition,
                displacementMagnitude,
                inputAxis.x,
                inputAxis.y,
                out curvedDisplacement,
                out deltaAngle);
        }

        var displacement = MathHelper.ProjectXZ(
            Vector3.Slerp(
                MathHelper.UnProjectXZ(straightDisplacement, 0.0f), 
                MathHelper.UnProjectXZ(curvedDisplacement, 0.0f), curvature));

        _currentBodyPosition += displacement;
        _deltaAngle += deltaAngle;

        if (UpdateTurns())
            _rigidbody.rotation = GetRotateTransform().rotation;
    }

    private void FixedUpdate()
    {
        UpdateStep();

        _rigidbody.MovePosition(MathHelper.UnProjectXZ(_currentBodyPosition, _rigidbody.position.y));
        _rigidbody.MoveRotation(Quaternion.Euler(0.0f, -_deltaAngle, 0.0f) * _rigidbody.rotation);
        _deltaAngle = 0.0f;
    }
}

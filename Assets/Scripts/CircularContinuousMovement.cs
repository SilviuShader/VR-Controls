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

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentBodyPosition = new Vector2(transform.position.x, transform.position.z);
    }

    private void Update()
    {
        var inputSpace = GetInputSpace();
        var inputAxis = Vector2.ClampMagnitude(_moveAction[SteamVR_Input_Sources.RightHand].axis, 1.0f);

        var forward = InputSpaceForwardXZ();
        var right = InputSpaceRightXZ();

        Vector2 displacement;
        var deltaAngle = 0.0f;
        var displacementMagnitude = _maxSpeed * Time.deltaTime;

        _currentBodyPosition = MathHelper.ProjectXZ(_rigidbody.position);

        if (RaycastReferenceObject(inputSpace.position, forward, out var targetPoint))
        {
            Curvature.CurvatureDisplacement(
                MathHelper.ProjectXZ(targetPoint),
                _currentBodyPosition,
                displacementMagnitude,
                inputAxis.x,
                inputAxis.y,
                out displacement,
                out deltaAngle);
        }
        else
        {
            displacement = (MathHelper.ProjectXZ(right) * inputAxis.x + MathHelper.ProjectXZ(forward) * inputAxis.y) * displacementMagnitude;
        }

        _currentBodyPosition += displacement;
        _deltaAngle += deltaAngle;
    }

    private void FixedUpdate()
    {
        var rotateTransform = GetRotateTransform();
        
        _rigidbody.MovePosition(MathHelper.UnProjectXZ(_currentBodyPosition, _rigidbody.position.y));
        rotateTransform.Rotate(Vector3.up, -_deltaAngle);
        _deltaAngle = 0.0f;
    }
}

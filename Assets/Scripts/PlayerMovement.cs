using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private SteamVR_Action_Vector2 _moveAction      = SteamVR_Input.GetVector2Action("Move");
    [SerializeField]
    private Transform              _inputSpace      = default;
    [SerializeField]
    private float                  _maxSpeed        = 10.0f;
    [SerializeField]
    private float                  _maxAcceleration = 10.0f;

    private Vector3                _desiredVelocity = Vector3.zero;
    private Rigidbody              _rigidbody;

    private void Awake() =>
        _rigidbody = GetComponent<Rigidbody>();

    private void Update()
    {
        var inputSpace = transform;
        if (_inputSpace)
            inputSpace = _inputSpace;

        var axis = Vector2.ClampMagnitude(_moveAction[SteamVR_Input_Sources.RightHand].axis, 1.0f);

        var forward = inputSpace.forward;
        forward.y = 0f;
        forward.Normalize();

        var right = inputSpace.right;
        right.y = 0f;
        right.Normalize();

        _desiredVelocity = (right * axis.x + forward * axis.y) * _maxSpeed;
    }

    private void FixedUpdate()
    {
        var velocity = _rigidbody.velocity;
        var maxSpeedChange = _maxAcceleration * Time.deltaTime;
        velocity = Vector3.MoveTowards(velocity, _desiredVelocity, maxSpeedChange);
        _rigidbody.velocity = velocity;
    }
}

using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class CircularTeleport : MovementMethod
{
    private const float                  FLOOR_BIAS               = 0.01f;
                                                                  
    [SerializeField]                                              
    private       SteamVR_Action_Boolean _teleportAction          = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport");
    [SerializeField]                                              
    private       bool                   _useGravity              = true;
    [SerializeField]                                              
    private       Transform              _rightHand               = default;
    [SerializeField]                                              
    private       float                  _teleportDistance        = 10.0f;
    [SerializeField]                                              
    private       float                  _arcDuration             = 3.0f;
    [SerializeField]                                              
    private       int                    _segmentsCount           = 60;
    [SerializeField]                                              
    private       LayerMask              _teleportLayerMask       = -1;
    [SerializeField]                                              
    private       Transform              _targetIndicatorPrefab   = default;
    [SerializeField]                     
    private       Material               _arcMaterial;
    [SerializeField]
    private       float                  _timeToChangeTargetState = 1.0f;
    [SerializeField]
    private       float                  _maxCurvature             = 0.5f;
                                         
    private       float                  _currentCurvature        = 0.0f;
    private       float                  _scale                   = 1.0f;
    private       Transform              _targetIndicator         = null;
    private       LineRenderer[]         _lineRenderers           = null;
    private       Rigidbody              _rigidbody               = null;
                                                                  
    private       bool                   _gotLookingTarget        = false;
    private       Vector3                _lookingTargetPosition   = Vector3.zero;
    private       float                  _accumulatedStateTime    = 0.0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _targetIndicator = Instantiate(_targetIndicatorPrefab);
        _targetIndicator.gameObject.SetActive(false);

        _lineRenderers = new LineRenderer[_segmentsCount];
        for (int i = 0; i < _segmentsCount; i++)
        {
            var lineObject = new GameObject("LineRenderer_" + i);
            _lineRenderers[i] = lineObject.AddComponent<LineRenderer>();
            _lineRenderers[i].positionCount = 2;
            _lineRenderers[i].startWidth = _lineRenderers[i].endWidth = 0.1f; // TODO: Hard-coded constant
            _lineRenderers[i].material = _arcMaterial;

            lineObject.transform.SetParent(transform);
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale    = Vector3.one;
        }
    }

    private void Start() =>
        _rigidbody.constraints = RigidbodyConstraints.FreezePosition | _rigidbody.constraints;

    private void Update()
    {
        _scale = transform.lossyScale.x;
        var inputSpace = GetInputSpace();
        var forward = InputSpaceForwardXZ();

        _accumulatedStateTime += Time.deltaTime;
        if (_accumulatedStateTime >= _timeToChangeTargetState)
            _accumulatedStateTime = _timeToChangeTargetState;

        if (RaycastReferenceObject(inputSpace.position, forward, out var targetPoint, out var curvature))
        {
            _currentCurvature = _maxCurvature * curvature;
            _lookingTargetPosition = targetPoint; // TODO: Move towards this if the target was changed
 
            if (!_gotLookingTarget)
                _accumulatedStateTime = _timeToChangeTargetState - _accumulatedStateTime;
            _gotLookingTarget = true;
        }
        else
        {
            if (_gotLookingTarget)
                _accumulatedStateTime = _timeToChangeTargetState - _accumulatedStateTime;
            _gotLookingTarget = false;
        }

        if (_teleportAction.GetState(SteamVR_Input_Sources.RightHand))
        {
            DrawArc();
        }
        else
        {
            HideArc();
            if (_teleportAction.GetStateUp(SteamVR_Input_Sources.RightHand))
                TryTeleport();
        }

        UpdateTurns();
    }

    private void DrawArc()
    {
        var arcTime = FindProjectileCollision(out var _);
        if (arcTime == float.MaxValue)
        {
            HideArc();
        }
        else
        {
            _targetIndicator.gameObject.SetActive(true);
            foreach (var lineRenderer in _lineRenderers)
                lineRenderer.gameObject.SetActive(true);

            _targetIndicator.position = GetArcPositionAtTime(arcTime, out _) + Vector3.up * FLOOR_BIAS;

            for (var i = 0; i < _segmentsCount; i++)
            {
                var t = (float)i / _segmentsCount;
                var nextT = ((float)(i + 1)) / _segmentsCount;
                var time = Mathf.Lerp(0.1f, arcTime, t); // TODO: Hard-coded constant for the start time
                var nextTime = Mathf.Lerp(0.1f, arcTime, nextT); // TODO: Hard-coded constant for start time
                _lineRenderers[i].SetPosition(0, GetArcPositionAtTime(time, out _));
                _lineRenderers[i].SetPosition(1, GetArcPositionAtTime(nextTime, out _));
            }
        }
    }

    private void HideArc()
    {
        _targetIndicator.gameObject.SetActive(false);
        foreach (var lineRenderer in _lineRenderers)
            lineRenderer.gameObject.SetActive(false);
    }

    private void TryTeleport()
    {
        var arcTime = FindProjectileCollision(out var _);
        if (arcTime == float.MaxValue)
            return;

        var oldPlanePosition = MathHelper.ProjectXZ(_rigidbody.position);

        _rigidbody.position = GetArcPositionAtTime(arcTime, out _) + Vector3.up * FLOOR_BIAS;
        if (_gotLookingTarget)
        {
            var lookTargetPlanePosition = MathHelper.ProjectXZ(_lookingTargetPosition);
            var currentPlanePosition = MathHelper.ProjectXZ(_rigidbody.position);
            GetRotateTransform().Rotate(
                Vector3.up, 
                -Vector2.SignedAngle(oldPlanePosition - lookTargetPlanePosition, currentPlanePosition - lookTargetPlanePosition));
        }
        // TODO: Also implement the ability to rotate via the opposite controller.s
    }
    
    private float FindProjectileCollision(out RaycastHit hitInfo)
    {
        var timeStep = _arcDuration / _segmentsCount;
        var segmentStartTime = 0.0f;

        hitInfo = new RaycastHit();

        var segmentStartPos = GetArcPositionAtTime(segmentStartTime, out _);
        for (int i = 0; i < _segmentsCount; ++i)
        {
            var segmentEndTime = segmentStartTime + timeStep;
            var segmentEndPos = GetArcPositionAtTime(segmentEndTime, out _);

            if (Physics.Linecast(segmentStartPos, segmentEndPos, out hitInfo, _teleportLayerMask))
            {
                var segmentDistance = Vector3.Distance(segmentStartPos, segmentEndPos);
                var hitTime = segmentStartTime + (timeStep * (hitInfo.distance / segmentDistance));
                return hitTime;
            }

            segmentStartTime = segmentEndTime;
            segmentStartPos = segmentEndPos;
        }

        return float.MaxValue;
    }

    private Vector3 GetArcPositionAtTime(float time, out float deltaRotation)
    {
        var gravity = _useGravity ? Physics.gravity : Vector3.zero;
        var position = _rightHand.position;

        var straightForwardInTime = _rightHand.forward * _teleportDistance * time;

        var bodyPosition = GetInputSpace().position;
        
        var up = Vector3.up;
        var forward = _lookingTargetPosition - bodyPosition;
        forward.y = 0.0f;
        forward.Normalize();
        var right = Vector3.Cross(up, forward);

        var xzForward = new Vector2(Vector3.Dot(_rightHand.forward, right), Vector3.Dot(_rightHand.forward, forward)).normalized;

        var displacedRotation = 0.0f;
        var displacementMagnitude = _teleportDistance * time;
        // TODO: Use rotation angle to rotate player
        Curvature.CurvatureDisplacement(
            MathHelper.ProjectXZ(_lookingTargetPosition), 
            MathHelper.ProjectXZ(position), 
            displacementMagnitude,
            xzForward.x,
            xzForward.y, 
            out var xzDisplacement, 
            out displacedRotation);

        var curvedForwardInTime = Vector3.up * (_rightHand.forward.y * _teleportDistance * time) +
                                  MathHelper.UnProjectXZ(xzDisplacement, 0.0f);

        var t = (_gotLookingTarget
            ? (_accumulatedStateTime / _timeToChangeTargetState)
            : 1.0f - (_accumulatedStateTime / _timeToChangeTargetState)) * _currentCurvature;

        //var forwardInTime = Vector3.Slerp(
        //    straightForwardInTime, 
        //    curvedForwardInTime,
        //    t);

        var lengthInTime = Mathf.Lerp(straightForwardInTime.magnitude, curvedForwardInTime.magnitude, t);
        var forwardInTime = (Quaternion.Euler(0.0f, t * displacedRotation, 0.0f) * straightForwardInTime).normalized * lengthInTime;

        deltaRotation = displacedRotation * t;

        var arcPos = position + (forwardInTime + (0.5f * time * time) * gravity) * _scale;
        return arcPos;
    }
}

using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class CircularTeleport : MonoBehaviour
{
    private const float                  FLOOR_BIAS             = 0.01f;

    [SerializeField]
    private       SteamVR_Action_Boolean _teleportAction        = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport");
    [SerializeField]                                    
    private       bool                   _useGravity            = true;
    [SerializeField]                                            
    private       Transform              _rightHand             = default;
    [SerializeField]                                            
    private       float                  _teleportDistance      = 10.0f;
    [SerializeField]                                            
    private       float                  _arcDuration           = 3.0f;
    [SerializeField]                                            
    private       int                    _segmentsCount         = 60;
    [SerializeField]                                            
    private       LayerMask              _teleportLayerMask     = -1;
    [SerializeField]                     
    private       Transform              _targetIndicatorPrefab = default;
    [SerializeField]                     
    private       Material               _arcMaterial;
                                         
    private       float                  _scale                 = 1.0f;
    private       Transform              _targetIndicator       = null;
    private       LineRenderer[]         _lineRenderers         = null;
    private       Rigidbody              _rigidbody             = null;

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

    private void Update()
    {
        _scale = transform.lossyScale.x;

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

            _targetIndicator.position = GetArcPositionAtTime(arcTime) + Vector3.up * FLOOR_BIAS;

            for (var i = 0; i < _segmentsCount; i++)
            {
                var t = (float)i / _segmentsCount;
                var nextT = ((float)(i + 1)) / _segmentsCount;
                var time = Mathf.Lerp(0.1f, arcTime, t); // TODO: Hard-coded constant for the start time
                var nextTime = Mathf.Lerp(0.1f, arcTime, nextT); // TODO: Hard-coded constant for start time
                _lineRenderers[i].SetPosition(0, GetArcPositionAtTime(time));
                _lineRenderers[i].SetPosition(1, GetArcPositionAtTime(nextTime));
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
        
        _rigidbody.position = GetArcPositionAtTime(arcTime) + Vector3.up * FLOOR_BIAS;
    }
    
    private float FindProjectileCollision(out RaycastHit hitInfo)
    {
        var timeStep = _arcDuration / _segmentsCount;
        var segmentStartTime = 0.0f;

        hitInfo = new RaycastHit();

        var segmentStartPos = GetArcPositionAtTime(segmentStartTime);
        for (int i = 0; i < _segmentsCount; ++i)
        {
            var segmentEndTime = segmentStartTime + timeStep;
            var segmentEndPos = GetArcPositionAtTime(segmentEndTime);

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

    private Vector3 GetArcPositionAtTime(float time)
    {
        var gravity = _useGravity ? Physics.gravity : Vector3.zero;

        var xzForward = MathHelper.ProjectXZ(_rightHand.forward); // TODO: make this relative to player view (dot products)

        var position = _rightHand.position;
        var displacementMagnitude = _teleportDistance * time;
        // TODO: Replace center
        // TODO: Replace curvature factor, forward factor
        Vector2 xzDisplacement;
        float rotationAngle; // TODO: Use this to rotate player
        Curvature.CurvatureDisplacement(Vector2.zero, MathHelper.ProjectXZ(position), displacementMagnitude, xzForward.x, xzForward.y, out xzDisplacement, out rotationAngle); 
        
        var arcPos = position + (Vector3.up * (_rightHand.forward.y * _teleportDistance * time) + MathHelper.UnProjectXZ(xzDisplacement, 0.0f) + (0.5f * time * time) * gravity) * _scale;
        return arcPos;
    }
}

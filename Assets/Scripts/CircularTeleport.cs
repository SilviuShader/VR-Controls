using UnityEngine;

public class CircularTeleport : MonoBehaviour
{
    private const float          FLOOR_BIAS             = 0.01f;
                                                        
    [SerializeField]                                    
    private       bool           _useGravity            = true;
    [SerializeField]                                    
    private       Transform      _rightHand             = default;
    [SerializeField]                                    
    private       float          _teleportDistance      = 10.0f;
    [SerializeField]                                    
    private       float          _arcDuration           = 3.0f;
    [SerializeField]                                    
    private       int            _segmentsCount         = 60;
    [SerializeField]                                    
    private       LayerMask      _teleportLayerMask     = -1;
    [SerializeField]             
    private       Transform      _targetIndicatorPrefab = default;
    [SerializeField]             
    private       Material       _arcMaterial;
                                 
    private       float          _scale                 = 1.0f;
    private       Transform      _targetIndicator;
    private       LineRenderer[] _lineRenderers         = null;

    private void Awake()
    {
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

        DrawArc();
    }

    private void DrawArc()
    {
        var arcTime = FindProjectileCollision(out var hit);
        if (arcTime == float.MaxValue)
        {
            _targetIndicator.gameObject.SetActive(false);
            foreach (var lineRenderer in _lineRenderers)
                lineRenderer.gameObject.SetActive(false);
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
        var arcPos = _rightHand.position + ((_rightHand.forward * _teleportDistance * time) + (0.5f * time * time) * gravity) * _scale;
        return arcPos;
    }
}

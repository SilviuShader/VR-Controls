using UnityEngine;
using UnityEngine.UIElements;

public class CircularTeleport : MonoBehaviour
{
    [SerializeField]
    private bool      _useGravity        = true;
    [SerializeField]                     
    private Transform _rightHand         = default;
    [SerializeField]                     
    private float     _teleportDistance  = 10.0f;
    [SerializeField]                     
    private float     _arcDuration       = 3.0f;
    [SerializeField]                     
    private float     _segmentsCount     = 60;
    [SerializeField]
    private LayerMask _teleportLayerMask = -1;

    private float     _scale             = 1.0f;

    private void Update()
    {
        _scale = transform.lossyScale.x;
    }

    private void OnDrawGizmos()
    {
        if (_rightHand == null)
            return;

        var projectileTime = FindProjectileCollision(out var hit);
        if (projectileTime == float.MaxValue)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(GetArcPositionAtTime(projectileTime), 1.0f);
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

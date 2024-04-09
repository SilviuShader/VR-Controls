using UnityEngine;

static class Curvature
{
    public static void CurvatureDisplacement(
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
}


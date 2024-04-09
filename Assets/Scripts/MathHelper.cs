using UnityEngine;

static class MathHelper
{
    public static Vector2 ProjectXZ(Vector3 vec) => new(vec.x, vec.z);
    public static Vector3 UnProjectXZ(Vector2 vec, float y) => new(vec.x, y, vec.y);
}

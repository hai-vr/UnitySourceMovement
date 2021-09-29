using UnityEngine;

public class Trace
{
    public Vector3 startPos;
    public Vector3 endPos;
    public float fraction;
    public bool startSolid;
    public Collider hitCollider;
    public Vector3 hitPoint;
    public Vector3 planeNormal;
    public float distance;

    public void _Pristine()
    {
        startPos = default;
        endPos = default;
        fraction = default;
        startSolid = default;
        hitCollider = default;
        hitPoint = default;
        planeNormal = default;
        distance = default;
    }
}

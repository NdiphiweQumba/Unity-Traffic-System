using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    public WayPoint PreviousWayPoint;
    public WayPoint CurrentWayPoint;
    public WayPoint NextWayPoint;

    [Range(0f, 5f)]
    public float Width = 1f;

    public Vector3 GetPosition()
    {
        Vector3 minbound = transform.position + transform.right * Width / 2f;
        Vector3 maxbound = transform.position - transform.right * Width / 2f;

        return Vector3.Lerp(minbound, maxbound, Random.Range(0f, 1f));
    }
}

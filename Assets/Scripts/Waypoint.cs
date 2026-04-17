using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Collider[] nextWaypoints;

    public float offsetLimitLeft, offsetLimitRight;

    //NEW
    public bool lapEnd = false;

    public bool startBeforeLine = false;
    public Collider firstLapWaypoint;

    public int lapWaypointValue;
    public Collider nextLapWaypoint;
}

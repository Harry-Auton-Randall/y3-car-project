using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Collider[] nextWaypoints;

    public bool aimStraight = false;
    public float offsetLimitLeft, offsetLimitRight;

    public bool lapEnd = false;

    public bool startBeforeLine = false;
    public Collider firstLapWaypoint;

    public int lapWaypointValue;
    public Collider nextLapWaypoint;
}

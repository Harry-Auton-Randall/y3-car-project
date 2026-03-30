using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Collider[] nextWaypoints;

    //NEW
    public bool lapEnd = false;

    public bool startBeforeLine = false;
    public Collider firstLapWaypoint;

    public int lapWaypointValue;
    public Collider nextLapWaypoint;
}

using UnityEngine;
//using System.IO;

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

    //void Awake()
    //{
    //    obj = transform.Find("PathingNode");
    //}
    //Transform obj;
    //bool objSet = false;
    //string filePath = "C:/Users/hauto/Desktop/y3carstuff.txt";
    //public void UpdateRoute(Vector3 pos, Vector3 rot)
    //{
    //    if (!objSet)
    //    {
    //        obj.position = pos;
    //        if (obj.localPosition.z >= 0)
    //        {
    //            objSet = true;
    //            obj.transform.forward = rot.normalized;
    //            obj.localPosition = new Vector3(obj.localPosition.x, 0, 0);
    //            obj.localRotation = Quaternion.Euler(0, obj.localEulerAngles.y, 0);

    //            string line = (this.gameObject.name + " - " + obj.localPosition.x + ", " + (obj.localEulerAngles.y < 180 ? obj.localEulerAngles.y : obj.localEulerAngles.y - 360));
    //            File.AppendAllText(filePath, line + System.Environment.NewLine);
    //        }
    //    }
    //}
}

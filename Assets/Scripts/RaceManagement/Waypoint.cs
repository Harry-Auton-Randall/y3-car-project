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

    public float aiTurnSpeedMult = 1;

    void Update()
    {
        for (int i = 0; i < nextWaypoints.Length; i++)
        {
            DebugArc.Draw(nextWaypoints[i].transform.Find("PathingNode").position, this.transform.Find("PathingNode").position, nextWaypoints[i].transform.Find("PathingNode").forward * -1);
        }
    }








    //AI-generated, just used for debugging
    public static class DebugArc
    {
        public static bool Draw(
            Vector3 start,
            Vector3 end,
            Vector3 startDirection
        )
        {
            int segments = 32;
            float duration = 0;

            Color c = Color.green;

            Vector3 chord = end - start;
            float chordLength = chord.magnitude;

            if (chordLength < 0.0001f)
                return false;

            Vector3 tangent = startDirection.normalized;

            // Plane normal defined by chord + tangent
            Vector3 planeNormal = Vector3.Cross(tangent, chord).normalized;

            // If tangent is parallel to chord, no unique circular arc exists
            if (planeNormal.sqrMagnitude < 0.0001f)
            {
                Debug.DrawLine(start, end, c, duration);
                return true;
            }

            // Radius direction at start is perpendicular to tangent in the arc plane
            Vector3 radiusDir = Vector3.Cross(planeNormal, tangent).normalized;

            // Solve circle center:
            // center lies on line: start + radiusDir * r
            // and must satisfy equal distance to end
            float denom = 2f * Vector3.Dot(end - start, radiusDir);

            if (Mathf.Abs(denom) < 0.0001f)
                return false;

            float radius = chord.sqrMagnitude / denom;

            Vector3 center = start + radiusDir * radius;

            Vector3 from = start - center;
            Vector3 to = end - center;

            float signedAngle = Vector3.SignedAngle(from, to, planeNormal);

            Vector3 prev = start;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;

                Quaternion rot = Quaternion.AngleAxis(signedAngle * t, planeNormal);

                Vector3 point = center + rot * from;

                Debug.DrawLine(prev, point, c, duration);

                prev = point;
            }

            return true;
        }
    }

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

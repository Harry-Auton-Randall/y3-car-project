using UnityEngine;

public class CarControlAI : MonoBehaviour
{
    CarMovement carMovement;
    Rigidbody rb;
    float timeStill;

    float motorIn;
    float steerIn;

    Vector3 waypointDirection;
    float waypointAngle;

    float maxSteering;
    float targetSpeed;
    float targetSpeedFraction;

    Collider[] nextWaypoints;
    Transform[] targetWaypoints;
    public int waypointsAhead = 2;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        rb = GetComponent<Rigidbody>();
    }

    public void UpdateWaypoint(Collider currentWaypointIn, Collider[] nextWaypointsIn)
    {
        if (nextWaypoints == null)
        {
            RecalcWaypoints(nextWaypointsIn);
        }
        else if (currentWaypointIn.transform == targetWaypoints[0])
        {
            nextWaypoints = nextWaypointsIn;
            for (int i=0;i<waypointsAhead - 1; i++)
            {
                targetWaypoints[i] = targetWaypoints[i+1];
            }
            targetWaypoints[waypointsAhead - 1] = FindNextWaypoint
                (targetWaypoints[waypointsAhead - 2].GetComponent<Waypoint>().nextWaypoints);
        }
        else
        {
            RecalcWaypoints(nextWaypointsIn);
        }
    }
    void RecalcWaypoints(Collider[] nextWaypointsIn2)
    {
        targetWaypoints = new Transform[waypointsAhead];
        nextWaypoints = nextWaypointsIn2;
        targetWaypoints[0] = FindNextWaypoint(nextWaypoints);
        for (int i=1; i<waypointsAhead;i++)
        {
            targetWaypoints[i] = FindNextWaypoint(targetWaypoints[i - 1].GetComponent<Waypoint>().nextWaypoints);
        }
    }
    Transform FindNextWaypoint(Collider[] current)
    {
        if (current.Length == 1)
        {
            return current[0].transform;
        }
        else
        {
            return current[Random.Range(0, current.Length)].transform;
        }
    }

    void Update()
    {
        //check how long the car's been stationary
        if (rb.linearVelocity.magnitude < 0.5f)
        {
            timeStill += Time.deltaTime;
        }
        else
        {
            timeStill = 0;
        }

        //ResetPosition if still for more than 3 seconds
        if (timeStill >= 3f)
        {
            carMovement.ResetPosition();
            timeStill = 0;
        }

        //get the angle between the car's rotation and the waypoint's position
        waypointDirection = transform.InverseTransformPoint(targetWaypoints[0].position);
        waypointDirection.y = 0;
        waypointAngle = Vector3.SignedAngle(Vector3.forward, waypointDirection, Vector3.up);

        //change car inputs depending on waypointAngle
        //Steering
        maxSteering = carMovement.steerRange * carMovement.steerRangeFraction;

        if (waypointAngle > maxSteering)
        {
            steerIn = 1;
        }
        else if (waypointAngle < maxSteering * -1)
        {
            steerIn = -1;
        }
        else
        {
            steerIn = waypointAngle / maxSteering;
        }

        //Motor - first sets targetSpeed, then accelerates/brakes if its below/above that speed
        if (Mathf.Abs(waypointAngle) <= 15f)
        {
            targetSpeed = carMovement.maxSpeed;
        }
        else if (Mathf.Abs(waypointAngle) > 90f)
        {
            targetSpeed = carMovement.maxSpeed * 0.1f;
        }
        else
        {
            targetSpeedFraction = ((-0.9f / 75f) * Mathf.Abs(waypointAngle)) + 1.18f;
            targetSpeed = targetSpeedFraction * carMovement.maxSpeed;
        }

        if (carMovement.currentSpeed <= targetSpeed)
        {
            motorIn = 1;
        }
        else
        {
            motorIn = -1;
        }

        carMovement.SetMotorIn(motorIn);
        carMovement.SetSteerIn(steerIn);
    }
}

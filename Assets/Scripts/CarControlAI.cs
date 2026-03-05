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
    Transform targetWaypoint;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        rb = GetComponent<Rigidbody>();
    }

    public void UpdateWaypoint(Collider[] nextWaypointsIn)
    {
        nextWaypoints = nextWaypointsIn;
        if (nextWaypoints.Length == 1)
        {
            targetWaypoint = nextWaypoints[0].transform;
        }
        else
        {
            targetWaypoint = nextWaypoints[Random.Range(0, nextWaypoints.Length)].transform;
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
        waypointDirection = transform.InverseTransformPoint(targetWaypoint.position);
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

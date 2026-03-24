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

    Vector3[] waypointTurningEnds;
    float[] waypointTurningRadii;
    float[] waypointTurningSpeeds;
    float[] waypointTurningAngles; //NEW
    float[] waypointTurningDists; //NEW

    float carTurningRadius; //NEW
    float carTurningSpeed; //NEW
    float carTurningAngle; //NEW
    float carTurningDist; //NEW

    float turningDistTotal; //NEW

    public int waypointsAhead = 2;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        rb = GetComponent<Rigidbody>();
    }

    public void UpdateWaypoint(Collider currentWaypointIn, Collider[] nextWaypointsIn)
    {
        if (nextWaypoints == null) //start of race / no nextWaypoints
        {
            RecalcWaypoints(nextWaypointsIn);
        }
        else if (currentWaypointIn.transform == targetWaypoints[0]) //correct waypoint hit
        {
            nextWaypoints = nextWaypointsIn;
            for (int i=0;i<waypointsAhead - 1; i++)
            {
                targetWaypoints[i] = targetWaypoints[i+1];
            }
            targetWaypoints[waypointsAhead - 1] = FindNextWaypoint
                (targetWaypoints[waypointsAhead - 2].GetComponent<Waypoint>().nextWaypoints);

            if (waypointsAhead > 2)
            {
                for (int i=0; i<waypointsAhead - 2; i++)
                {
                    waypointTurningEnds[i] = waypointTurningEnds[i + 1];
                    waypointTurningRadii[i] = waypointTurningRadii[i + 1];
                    waypointTurningSpeeds[i] = waypointTurningSpeeds[i + 1];
                    waypointTurningAngles[i] = waypointTurningAngles[i + 1]; //NEW
                    waypointTurningDists[i] = waypointTurningDists[i + 1]; //NEW
                }
            }

            waypointTurningEnds[waypointsAhead - 2] = CalculateTurningEnd
                (targetWaypoints[waypointsAhead - 2], targetWaypoints[waypointsAhead - 1]);

            waypointTurningRadii[waypointsAhead - 2] = CalculateTurningRadius
                (waypointTurningEnds[waypointsAhead - 2]);

            waypointTurningSpeeds[waypointsAhead - 2] = CalculateTurningSpeed
                (waypointTurningRadii[waypointsAhead - 2]);

            waypointTurningAngles[waypointsAhead - 2] = CalculateTurningAngle
                (waypointTurningEnds[waypointsAhead - 2], waypointTurningRadii[waypointsAhead - 2]); //NEW

            if (waypointTurningAngles[waypointsAhead - 2] == 0) //NEW IF/ELSE
            {
                waypointTurningDists[waypointsAhead - 2] = waypointTurningEnds[waypointsAhead - 2].z;
            }
            else
            {
                waypointTurningDists[waypointsAhead - 2] = CalculateTurningDist
                    (waypointTurningAngles[waypointsAhead - 2], waypointTurningRadii[waypointsAhead - 2]);
            }
        }
        else //incorrect waypoint hit
        {
            RecalcWaypoints(nextWaypointsIn);
        }
    }

    void RecalcWaypoints(Collider[] nextWaypointsIn2)
    {
        //initialises targetWaypoints, for start of race
        targetWaypoints = new Transform[waypointsAhead];

        waypointTurningEnds = new Vector3[waypointsAhead - 1];
        waypointTurningRadii = new float[waypointsAhead - 1];
        waypointTurningSpeeds = new float[waypointsAhead - 1];
        waypointTurningAngles = new float[waypointsAhead - 1]; //NEW
        waypointTurningDists = new float[waypointsAhead - 1]; //NEW

        nextWaypoints = nextWaypointsIn2;
        targetWaypoints[0] = FindNextWaypoint(nextWaypoints);
        for (int i=1; i<waypointsAhead;i++)
        {
            targetWaypoints[i] = FindNextWaypoint
                (targetWaypoints[i - 1].GetComponent<Waypoint>().nextWaypoints);
        }

        for (int i=0; i<waypointsAhead - 1; i++)
        {
            waypointTurningEnds[i] = CalculateTurningEnd
                (targetWaypoints[i], targetWaypoints[i + 1]);

            waypointTurningRadii[i] = CalculateTurningRadius(waypointTurningEnds[i]);

            waypointTurningSpeeds[i] = CalculateTurningSpeed(waypointTurningRadii[i]);

            waypointTurningAngles[i] = CalculateTurningAngle
                (waypointTurningEnds[i], waypointTurningRadii[i]); //NEW

            if (waypointTurningAngles[i] == 0) //NEW IF/ELSE
            {
                waypointTurningDists[i] = waypointTurningEnds[i].z;
            }
            else
            {
                waypointTurningDists[i] = CalculateTurningDist
                    (waypointTurningAngles[i], waypointTurningRadii[i]);
            }
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

    Vector3 CalculateTurningEnd(Transform startPos, Transform endPos)
    {
        return startPos.InverseTransformPoint(endPos.position);
    }

    float CalculateTurningRadius(Vector3 localEndPos)
    {
        return Mathf.Abs(
            (Mathf.Pow(localEndPos.x, 2) + Mathf.Pow(localEndPos.z, 2))
            / (2 * localEndPos.x));
    }

    float CalculateTurningSpeed(float turnRadius)
    {
        return 2.95258f * Mathf.Pow(turnRadius, 0.542118f);
    }

    float CalculateTurningAngle(Vector3 localEndPos, float turnRadius)
    {
        if (turnRadius == Mathf.Infinity)
        {
            return 0f;
        }
        else
        {
            //Finds start and end positions relative to the turning centre
            localEndPos.x = Mathf.Abs(localEndPos.x);
            localEndPos.x -= turnRadius;
            localEndPos.y = 0;

            float tempAngle = Vector3.SignedAngle(-Vector3.right, localEndPos, Vector3.up);
            if (tempAngle < 0f)
            {
                tempAngle += 360f;
            }
            return tempAngle;
        }
    }

    float CalculateTurningDist(float turnAngle, float turnRadius)
    {
        return (Mathf.PI * 2 * turnRadius * (turnAngle / 360f));
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

        //Calculate the car's turning values - waypointDirection is the turningEnd
        carTurningRadius = CalculateTurningRadius(waypointDirection);
        carTurningSpeed = CalculateTurningSpeed(carTurningRadius);
        carTurningAngle = CalculateTurningAngle(waypointDirection, carTurningRadius);
        if (carTurningAngle == 0)
        {
            carTurningDist = waypointDirection.z;
        }
        else
        {
            carTurningDist = CalculateTurningDist
                (carTurningAngle, carTurningRadius);
        }

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

        //Calculates how fast the car would be at each waypoint if it started braking now
        //Checks against each waypoint's turningSpeed, changes targetSpeed if going too fast
        turningDistTotal = carTurningDist;
        if (Mathf.Pow(waypointTurningSpeeds[0], 2) < 
            Mathf.Pow(carMovement.currentSpeed, 2) - (16 * turningDistTotal))
        {
            if (waypointTurningSpeeds[0] < targetSpeed)
            {
                targetSpeed = waypointTurningSpeeds[0];
            }
        }
        if (waypointsAhead > 2)
        {
            for (int i = 0; i < waypointsAhead - 2; i++)
            {
                turningDistTotal += waypointTurningDists[i];
                if (Mathf.Pow(waypointTurningSpeeds[i+1], 2) <
                    Mathf.Pow(carMovement.currentSpeed, 2) - (16 * turningDistTotal))
                {
                    if (waypointTurningSpeeds[i+1] < targetSpeed)
                    {
                        targetSpeed = waypointTurningSpeeds[i+1];
                    }
                }
            }
        }

        //Also checks targetSpeed against carTurningSpeed,
        //so the car doesn't speed up halfway through a corner
        if (targetSpeed > carTurningSpeed)
        { 
            targetSpeed = carTurningSpeed;
        }

        //Also prevents the car from going too slow
        if (targetSpeed < carMovement.maxSpeed * 0.1f)
        {
            targetSpeed = carMovement.maxSpeed * 0.1f;
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

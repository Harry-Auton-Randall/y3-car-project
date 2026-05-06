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

    Transform targetWaypointRandomPos;
    float targetWaypointOffset;
    public float waypointOffsetMult = 1f; //NEW

    Vector3[] waypointTurningEnds;
    float[] waypointTurningRadii;
    float[] waypointTurningSpeeds;
    float[] waypointTurningAngles;
    float[] waypointTurningDists;

    float carTurningRadius;
    float carTurningSpeed;
    float carTurningAngle;
    float carTurningDist;

    float turningDistTotal;

    public int waypointsAhead = 4;

    bool reversing = false;

    Ray[] frontBackRays = new Ray[6];
    LayerMask waypointMask;
    RaycastHit rayHit;
    float frontRayDist = 2.3f;

    Ray waypointRotationRay;
    Vector3 waypointToCarPosition;
    float waypointToCarAngle;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        rb = GetComponent<Rigidbody>();
        waypointMask = (1 << LayerMask.NameToLayer("Waypoint"));
        targetWaypointRandomPos = new GameObject("AiCarTargetWaypointPos").transform; //NEW
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
                    waypointTurningAngles[i] = waypointTurningAngles[i + 1];
                    waypointTurningDists[i] = waypointTurningDists[i + 1];
                }
            }

            waypointTurningEnds[waypointsAhead - 2] = CalculateTurningEnd
                (targetWaypoints[waypointsAhead - 2], targetWaypoints[waypointsAhead - 1]);

            waypointTurningRadii[waypointsAhead - 2] = CalculateTurningRadius
                (waypointTurningEnds[waypointsAhead - 2]);

            waypointTurningSpeeds[waypointsAhead - 2] = CalculateTurningSpeed
                (waypointTurningRadii[waypointsAhead - 2]);

            waypointTurningAngles[waypointsAhead - 2] = CalculateTurningAngle
                (waypointTurningEnds[waypointsAhead - 2], waypointTurningRadii[waypointsAhead - 2]);

            if (waypointTurningAngles[waypointsAhead - 2] == 0)
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

        targetWaypointOffset = Random.Range(
            -(targetWaypoints[0].GetComponent<Waypoint>().offsetLimitLeft),
            targetWaypoints[0].GetComponent<Waypoint>().offsetLimitRight); //NEW
    }

    void RecalcWaypoints(Collider[] nextWaypointsIn2)
    {
        //initialises targetWaypoints, for start of race
        targetWaypoints = new Transform[waypointsAhead];

        waypointTurningEnds = new Vector3[waypointsAhead - 1];
        waypointTurningRadii = new float[waypointsAhead - 1];
        waypointTurningSpeeds = new float[waypointsAhead - 1];
        waypointTurningAngles = new float[waypointsAhead - 1];
        waypointTurningDists = new float[waypointsAhead - 1];

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
                (waypointTurningEnds[i], waypointTurningRadii[i]);

            if (waypointTurningAngles[i] == 0)
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
        //Draw rays - CHANGED
        frontBackRays[0] = new Ray(transform.position + (transform.right * -0.89f), transform.forward);
        frontBackRays[1] = new Ray(transform.position, transform.forward);
        frontBackRays[2] = new Ray(transform.position + (transform.right * 0.89f), transform.forward);

        frontBackRays[3] = new Ray(transform.position + (transform.right * -0.89f), transform.forward * -1);
        frontBackRays[4] = new Ray(transform.position, transform.forward * -1);
        frontBackRays[5] = new Ray(transform.position + (transform.right * 0.89f), transform.forward * -1);

        waypointRotationRay = new Ray(transform.position, targetWaypoints[0].forward);

        //Makes the rays visible in Scene view
        for (int i=0;i<6;i++)
        {
            Debug.DrawRay(frontBackRays[i].origin, frontBackRays[i].direction * frontRayDist, Color.yellow); //CHANGED
        }
        Debug.DrawRay(waypointRotationRay.origin, 
            waypointRotationRay.direction * 1000, Color.yellow);

        //check how long the car's been stationary
        if (rb.linearVelocity.magnitude < 0.5f)
        {
            timeStill += Time.deltaTime;
        }
        else
        {
            timeStill = 0;
        }

        //If still for more than 0.5 seconds, and the car's front
        //is touching something, begin reversing
        if (timeStill >= 0.5f)
        {
            for (int i=0;i<3;i++)
            {
                if (Physics.Raycast(frontBackRays[i], out rayHit, frontRayDist, ~(waypointMask))) //CHANGED
                {
                    reversing = true;
                    break;
                }
            }
        }
        //Disables reversing if the car's rear touches something
        for (int i = 3; i < 6; i++)
        {
            if (Physics.Raycast(frontBackRays[i], out rayHit, frontRayDist, ~(waypointMask))) //CHANGED
            {
                reversing = false;
                break;
            }
        }

        //ResetPosition if still for more than 3 seconds
        if (timeStill >= 3f)
        {
            carMovement.ResetPosition();
            timeStill = 0;
            reversing = false;
        }

        //targetWaypointRandomPos is set to the transform of the targetWaypoint,
        // + some random deviation on its x axis
        targetWaypointRandomPos.position = targetWaypoints[0].position
            + (targetWaypoints[0].right * targetWaypointOffset * waypointOffsetMult); //CHANGED
        targetWaypointRandomPos.rotation = targetWaypoints[0].rotation;

        //Find the car's position/angle relative to the next waypoint
        waypointToCarPosition = targetWaypointRandomPos
            .InverseTransformPoint(transform.position); //CHANGED
        waypointToCarPosition.y = 0;
        waypointToCarAngle = Vector3.Angle(Vector3.forward * -1, waypointToCarPosition);

        //get the angle between the car's rotation and waypointDirection
        //waypointDirection is either its position or the direction its facing
        if (Physics.Raycast(waypointRotationRay, out rayHit, Mathf.Infinity, waypointMask)
            && rayHit.transform == targetWaypoints[0] && waypointToCarAngle > 30)
        {
            waypointDirection = transform.InverseTransformPoint(rayHit.point);
        }
        else
        {
            waypointDirection = transform.InverseTransformPoint
                (targetWaypointRandomPos.position); //CHANGED
        }
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
            reversing = false;
        }
        //Invert steering if going backwards
        if (carMovement.currentSpeed < 0)
        {
            steerIn *= -1;
        }

        //If reversing, override all speed calculations and set motorIn to -1
        if (reversing)
        {
            motorIn = -1;
        }
        else
        {
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
                    if (Mathf.Pow(waypointTurningSpeeds[i + 1], 2) <
                        Mathf.Pow(carMovement.currentSpeed, 2) - (16 * turningDistTotal))
                    {
                        if (waypointTurningSpeeds[i + 1] < targetSpeed)
                        {
                            targetSpeed = waypointTurningSpeeds[i + 1];
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
        }

        carMovement.SetMotorIn(motorIn);
        carMovement.SetSteerIn(steerIn);
    }
}

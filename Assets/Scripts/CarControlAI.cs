using UnityEngine;
using System.Collections.Generic;

[System.Serializable] public struct UpcomingWaypointInfo
{
    public Transform baseT;
    public Transform pathingNode;
    public Waypoint script;
    public Vector3 startRelativePos, endRelativePos;
    public float turnRadius, turnSpeed, turnAngle, turnDist;
    public float offsetDist;

    public UpcomingWaypointInfo(Transform turnStart, Transform turnEnd, Transform turnEndExtra)
    {
        //initialises everything, so "this" can be used
        turnRadius = 0;
        turnSpeed = 0;
        turnAngle = 0;
        turnDist = 0;
        startRelativePos = Vector3.zero;
        endRelativePos = Vector3.zero;
        offsetDist = 0;

        baseT = turnEnd;
        pathingNode = turnEndExtra;
        script = turnEnd.GetComponent<Waypoint>();
        this.UpdateInfo(turnStart);
    }
    public UpcomingWaypointInfo(Transform turnStart, Transform turnEnd)
    {
        //initialises everything, so "this" can be used
        turnRadius = 0;
        turnSpeed = 0;
        turnAngle = 0;
        turnDist = 0;
        startRelativePos = Vector3.zero;
        endRelativePos = Vector3.zero;
        offsetDist = 0;

        baseT = turnEnd;
        pathingNode = turnEnd.Find("PathingNode");
        script = turnEnd.GetComponent<Waypoint>();
        this.UpdateInfo(turnStart);
    }
    public void UpdateInfo(Transform turnStart)
    {
        endRelativePos = CarControlAI.CalculateTurningEnd(turnStart, pathingNode);
        startRelativePos = CarControlAI.CalculateTurningEnd(pathingNode, turnStart);
        startRelativePos.x *= -1; //calculated as if the end waypoint is facing backwards
        startRelativePos.z *= -1;
        turnRadius = CarControlAI.CalculateTurningRadius(startRelativePos);
        turnAngle = CarControlAI.CalculateTurningAngle(startRelativePos, turnRadius);
        turnDist = CarControlAI.CalculateTurningCircumference(turnAngle, turnRadius);
    }
    public void SetTurnSpeed(float speedIn) { turnSpeed = speedIn; }
    public void SetOffsetDist(float startPoint, float leftLimit, float rightLimit)
    {
        float maxOffsetDistLeft = startPoint - (turnDist / 6f);
        float maxOffsetDistRight = startPoint + (turnDist / 6f);
        offsetDist = Random.Range(
            Mathf.Max(leftLimit, maxOffsetDistLeft), 
            Mathf.Min(rightLimit, maxOffsetDistRight)
        );

    }
    public void SetOffsetDist(float startPoint)
    {
        SetOffsetDist(startPoint, script.offsetLimitLeft * -1, script.offsetLimitRight);
    }
}

public class CarControlAI : MonoBehaviour
{
    public float aiSkill = 1; //0 = worst, 1 = best
    float lowSkillTurnSpeedMult = 0.75f;

    CarMovement carMovement;
    Rigidbody rb;
    float timeStill;

    float motorIn;
    float steerIn;

    Vector3 waypointDirection;
    float waypointAngle;

    //stuff for AI steering version 2
    //Vector3 waypointDirectionGlobal;
    //Transform waypointDirectionTransform;
    //Vector3 waypointDirectionToCarPosition;
    //float waypointDirectionToCarRadius;
    //float waypointDirectionToCarAngle;
    //Quaternion steeringTargetRot;
    //float steeringTargetRotY;

    float maxSteering;
    float targetSpeed;
    float targetSpeedFraction;

    float speedLimit = 999;

    Collider[] nextWaypoints;
    Transform[] targetWaypoints;

    Transform targetWaypointRandomPos;
    float targetWaypointOffset = 0;
    public float waypointOffsetMult = 1f;
    bool waypointAimStraight;

    Quaternion carRotRelativeToWaypoint;
    float carRotRelativeToWaypointY;
    Transform frontWheelMidpoint;
    Vector3 frontWheelMidpointDefaultPos;

    List<UpcomingWaypointInfo> upcomingWaypoints = new List<UpcomingWaypointInfo>();
    UpcomingWaypointInfo steeringArc;

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
    public float brakingSpeed;

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
        //waypointMask = (1 << LayerMask.NameToLayer("Waypoint")) | (1 << LayerMask.NameToLayer("Wall"));
        targetWaypointRandomPos = new GameObject("AiCarTargetWaypointPos").transform;
        frontWheelMidpoint = transform.Find("FrontWheelMidpoint").transform;
        frontWheelMidpointDefaultPos = frontWheelMidpoint.localPosition;

        //waypointDirectionTransform = new GameObject("AiCarWaypointDirection").transform;
    }
    void Start()
    {
        if (carMovement.newBraking)
        {
            brakingSpeed = 11;
        }
        else
        {
            brakingSpeed = 8;
        }
    }

    public void UpdateWaypoint(Collider newCurrentWaypoint, Collider[] nextWaypointsIn)
    {
        UpcomingWaypointInfo tempUWI;

        //checks for incorrect waypoint hit, aka the car's taken a wrong turn and must recalculate its route
        //Clears the whole list, which will be re-filled. Doesn't delete the first entry (its offsetDist is needed)
        if (upcomingWaypoints.Count > 0 && newCurrentWaypoint.transform != upcomingWaypoints[0].baseT)
        {
            upcomingWaypoints.RemoveRange(1, upcomingWaypoints.Count - 1);
        }
        //Trims the list if bigger than waypointsAhead allows
        while (upcomingWaypoints.Count > waypointsAhead)
        {
            upcomingWaypoints.RemoveAt(waypointsAhead);
        }
        //If the list's empty, add an entry with the end set to the new waypoint
        //Its other contents don't matter, because it gets deleted after the while loop. Its just needed to start said while loop
        if (upcomingWaypoints.Count == 0)
        {
            upcomingWaypoints.Add(new UpcomingWaypointInfo(this.transform, newCurrentWaypoint.transform));
        }
        //Add as many new entries as needed to fill up to waypointsAhead (+1 to account for the 0th entry not being deleted yet)
        while(upcomingWaypoints.Count < waypointsAhead + 1)
        {
            int uWIndex = upcomingWaypoints.Count - 1;
            //If the route ends (no more nextWaypoints), stop adding entries
            if (upcomingWaypoints[uWIndex].script.nextWaypoints == null
                || upcomingWaypoints[uWIndex].script.nextWaypoints.Length == 0)
            {
                break;
            }
            upcomingWaypoints.Add(new UpcomingWaypointInfo(
                upcomingWaypoints[uWIndex].pathingNode,
                FindNextWaypoint(upcomingWaypoints[uWIndex].script.nextWaypoints))
            );
            tempUWI = upcomingWaypoints[uWIndex + 1];
            tempUWI.SetOffsetDist(upcomingWaypoints[uWIndex].offsetDist);
            upcomingWaypoints[uWIndex + 1] = tempUWI;
        }
        //Adds all the turnSpeeds
        
        for (int i = 1; i < upcomingWaypoints.Count;i++)
        {
            tempUWI = upcomingWaypoints[i];
            tempUWI.SetTurnSpeed(CalculateTurningSpeed(tempUWI.turnRadius, tempUWI.script.aiTurnSpeedMult * Mathf.Lerp(lowSkillTurnSpeedMult, 1, aiSkill)));
            //tempUWI.SetOffsetDist(upcomingWaypoints[i - 1].offsetDist);
            upcomingWaypoints[i] = tempUWI;
        }

        //Remove the 0th entry, because its the waypoint that was just passed and doesn't need to be targeted
        upcomingWaypoints.RemoveAt(0);

        ////targetWaypointOffset is measured in metres
        ////Can change by 1m per 6m of waypoint distance
        //float waypointMaxOffsetDistLeft = targetWaypointOffset - (upcomingWaypoints[0].turnDist / 10f);
        //float waypointMaxOffsetDistRight = targetWaypointOffset + (upcomingWaypoints[0].turnDist / 10f);
        //targetWaypointOffset = Random.Range(
        //    Mathf.Max(-1 * upcomingWaypoints[0].script.offsetLimitLeft, waypointMaxOffsetDistLeft),
        //    Mathf.Min(upcomingWaypoints[0].script.offsetLimitRight, waypointMaxOffsetDistRight)
        //);

        return;

        if (nextWaypoints == null)
        {
            RecalcWaypoints(nextWaypointsIn);
        }
        else if (newCurrentWaypoint.transform != targetWaypoints[0]) //incorrect waypoint hit
        {
            RecalcWaypoints(nextWaypointsIn);
        }
        else //correct waypoint hit
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
                (waypointTurningRadii[waypointsAhead - 2], 1);

            waypointTurningAngles[waypointsAhead - 2] = CalculateTurningAngle
                (waypointTurningEnds[waypointsAhead - 2], waypointTurningRadii[waypointsAhead - 2]);

            if (waypointTurningAngles[waypointsAhead - 2] == 0)
            {
                waypointTurningDists[waypointsAhead - 2] = waypointTurningEnds[waypointsAhead - 2].z;
            }
            else
            {
                waypointTurningDists[waypointsAhead - 2] = CalculateTurningCircumference
                    (waypointTurningAngles[waypointsAhead - 2], waypointTurningRadii[waypointsAhead - 2]);
            }
        }

        targetWaypointOffset = Random.Range(
            -(targetWaypoints[0].GetComponent<Waypoint>().offsetLimitLeft),
            targetWaypoints[0].GetComponent<Waypoint>().offsetLimitRight);

        waypointAimStraight = targetWaypoints[0].GetComponent<Waypoint>().aimStraight;
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

            waypointTurningSpeeds[i] = CalculateTurningSpeed(waypointTurningRadii[i], 1);

            waypointTurningAngles[i] = CalculateTurningAngle
                (waypointTurningEnds[i], waypointTurningRadii[i]);

            if (waypointTurningAngles[i] == 0)
            {
                waypointTurningDists[i] = waypointTurningEnds[i].z;
            }
            else
            {
                waypointTurningDists[i] = CalculateTurningCircumference
                    (waypointTurningAngles[i], waypointTurningRadii[i]);
            }
        }
    }

    static Transform FindNextWaypoint(Collider[] nextWaypoints)
    {
        return (nextWaypoints.Length == 1) ? nextWaypoints[0].transform
            : nextWaypoints[Random.Range(0, nextWaypoints.Length)].transform;
        //if (nextWaypoints.Length == 1)
        //{
        //    return nextWaypoints[0].transform;
        //}
        //else
        //{
        //    return nextWaypoints[Random.Range(0, nextWaypoints.Length)].transform;
        //}
    }

    public static Vector3 CalculateTurningEnd(Transform startPos, Transform endPos)
    {
        return startPos.InverseTransformPoint(endPos.position);
    }

    public static float CalculateTurningRadius(Vector3 localEndPos)
    {
        return Mathf.Abs(
            (Mathf.Pow(localEndPos.x, 2) + Mathf.Pow(localEndPos.z, 2))
            / (2 * localEndPos.x));
    }

    public static float CalculateTurningSpeed(float turnRadius, float mult)
    {
        return mult * (2.95258f * Mathf.Pow(turnRadius, 0.542118f));
    }

    public static float CalculateTurningAngle(Vector3 localEndPos, float turnRadius)
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

    public static float CalculateTurningCircumference(float turnAngle, float turnRadius)
    {
        return (Mathf.PI * 2 * turnRadius * (turnAngle / 360f));
    }

    void Update()
    {
        //Draw rays
        frontBackRays[0] = new Ray(transform.position + (transform.right * -0.89f), transform.forward);
        frontBackRays[1] = new Ray(transform.position, transform.forward);
        frontBackRays[2] = new Ray(transform.position + (transform.right * 0.89f), transform.forward);

        frontBackRays[3] = new Ray(transform.position + (transform.right * -0.89f), transform.forward * -1);
        frontBackRays[4] = new Ray(transform.position, transform.forward * -1);
        frontBackRays[5] = new Ray(transform.position + (transform.right * 0.89f), transform.forward * -1);

        waypointRotationRay = new Ray(transform.position, upcomingWaypoints[0].baseT.forward);

        //Makes the rays visible in Scene view
        for (int i=0;i<6;i++)
        {
            Debug.DrawRay(frontBackRays[i].origin, frontBackRays[i].direction * frontRayDist, Color.yellow);
        }
        Debug.DrawRay(waypointRotationRay.origin, 
            waypointRotationRay.direction * 10, Color.yellow);

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
                if (Physics.Raycast(frontBackRays[i], out rayHit, frontRayDist, ~(waypointMask)))
                {
                    reversing = true;
                    break;
                }
            }
        }
        //Disables reversing if the car's rear touches something
        for (int i = 3; i < 6; i++)
        {
            if (Physics.Raycast(frontBackRays[i], out rayHit, frontRayDist, ~(waypointMask)))
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

        //BEGINNING OF NEW STUFF

        if (upcomingWaypoints == null || upcomingWaypoints.Count == 0)
        {
            motorIn = SetMotor(0, carMovement.currentSpeed);
            steerIn = 0;
            return;
        }
        //Updates first upcomingWaypoint entry with the car's stuff
        UpcomingWaypointInfo tempUWI = upcomingWaypoints[0];
        tempUWI.UpdateInfo(this.transform);
        tempUWI.SetTurnSpeed(CalculateTurningSpeed(tempUWI.turnRadius, tempUWI.script.aiTurnSpeedMult * Mathf.Lerp(lowSkillTurnSpeedMult, 1, aiSkill)));
        upcomingWaypoints[0] = tempUWI;

        //STEERING
        //If close enough to the target waypoint, steer towards the next one. Prevents sharp turning when close to the target waypoint
        bool closeToNextWaypoint;
        if (Physics.Raycast(waypointRotationRay, out rayHit, 10, waypointMask) && rayHit.transform == upcomingWaypoints[0].baseT && upcomingWaypoints.Count > 1)
        {
            targetWaypointRandomPos.position = upcomingWaypoints[1].pathingNode.position;
            targetWaypointRandomPos.position += (upcomingWaypoints[1].offsetDist * upcomingWaypoints[1].baseT.right *
                ((waypointOffsetMult + 2*(1 - aiSkill)) / 3f)); //Average of randomness due to racer count, and randomness due to AI skill. The latter is weighted twice as much.
            targetWaypointRandomPos.rotation = upcomingWaypoints[1].pathingNode.rotation;
            closeToNextWaypoint = true;
        }
        else
        {
            targetWaypointRandomPos.position = upcomingWaypoints[0].pathingNode.position;
            targetWaypointRandomPos.position += (upcomingWaypoints[0].offsetDist * upcomingWaypoints[0].baseT.right *
                ((waypointOffsetMult + 2*(1 - aiSkill)) / 3f));
            targetWaypointRandomPos.rotation = upcomingWaypoints[0].pathingNode.rotation;
            closeToNextWaypoint = false;
        }

        //Move frontWheelMidpoint to where it's predicted to be in a moment
        frontWheelMidpoint.localPosition = frontWheelMidpointDefaultPos;
        frontWheelMidpoint.localPosition += Vector3.forward * 0.1f * carMovement.currentSpeed;

        //Casts an arc from the target back towards the car, and aligns the car's wheels with it
        steeringArc = new UpcomingWaypointInfo(frontWheelMidpoint, targetWaypointRandomPos, targetWaypointRandomPos);

        if (steeringArc.startRelativePos.x < 0) { steeringArc.turnAngle *= -1; }

        carRotRelativeToWaypoint = Quaternion.Inverse(steeringArc.baseT.rotation) * this.transform.rotation;
        carRotRelativeToWaypointY = carRotRelativeToWaypoint.eulerAngles.y;
        if (carRotRelativeToWaypointY > 180)
        {
            carRotRelativeToWaypointY -= 360;
        }

        steerIn = SetSteering(
            steeringArc.turnAngle - carRotRelativeToWaypointY,
            carMovement.steerRange * carMovement.steerRangeFraction
            );

        if (reversing && Mathf.Abs(steerIn) < 1) { reversing = false; }
        if (carMovement.currentSpeed < 0) { steerIn *= -1; }

        Waypoint.DebugArc.Draw(steeringArc.baseT.position, frontWheelMidpoint.position, steeringArc.baseT.forward * -1);


        //waypointDirection = transform.InverseTransformPoint(upcomingWaypoints[0].pathingNode.position);
        //waypointDirection.y = 0;
        //steerIn = SetSteering(
        //    Vector3.SignedAngle(Vector3.forward, waypointDirection, Vector3.up), 
        //    carMovement.steerRange * carMovement.steerRangeFraction
        //    );

        //ACCELERATION
        steeringArc.turnSpeed = CalculateTurningSpeed(steeringArc.turnRadius, upcomingWaypoints[0].script.aiTurnSpeedMult * Mathf.Lerp(1, lowSkillTurnSpeedMult, aiSkill));

        if (reversing) { motorIn = SetMotor(-speedLimit, carMovement.currentSpeed); }
        else
        {
            float lowestSpeed = speedLimit;
            float totalDist = 0;
            float finalV;

            if (steeringArc.turnSpeed < lowestSpeed) { lowestSpeed = steeringArc.turnSpeed; }

            //For each upcoming waypoint, figures out if its going too fast for its turn and needs to brake
            //(uses SUVAT equation to find its final velocity if it spend the entire distance braking)
            if (upcomingWaypoints.Count > 1)
            {
                for (int i = 1; i < upcomingWaypoints.Count; i++)
                {
                    totalDist += upcomingWaypoints[i-1].turnDist;
                    finalV = GetFinalVelocity(carMovement.currentSpeed, brakingSpeed, totalDist);
                    if (finalV >= upcomingWaypoints[i].turnSpeed && upcomingWaypoints[i].turnSpeed < lowestSpeed)
                    {
                        lowestSpeed = upcomingWaypoints[i].turnSpeed;
                    }
                }
            }

            motorIn = SetMotor(lowestSpeed, carMovement.currentSpeed);
        }

        carMovement.SetMotorIn(motorIn);
        carMovement.SetSteerIn(steerIn);

        return;


        //targetWaypointRandomPos is set to the transform of the targetWaypoint,
        // + some random deviation on its x axis
        targetWaypointRandomPos.position = targetWaypoints[0].position
            + (targetWaypoints[0].right * targetWaypointOffset * waypointOffsetMult);
        targetWaypointRandomPos.rotation = targetWaypoints[0].rotation;

        //Find the car's position/angle relative to the next waypoint
        waypointToCarPosition = targetWaypointRandomPos
            .InverseTransformPoint(transform.position);
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
                (targetWaypointRandomPos.position);
        }
        //waypointDirectionGlobal = transform.TransformPoint(waypointDirection);
        waypointDirection.y = 0;
        waypointAngle = Vector3.SignedAngle(Vector3.forward, waypointDirection, Vector3.up);

        //Calculate the car's turning values - waypointDirection is the turningEnd
        carTurningRadius = CalculateTurningRadius(waypointDirection);
        carTurningSpeed = CalculateTurningSpeed(carTurningRadius, 1);
        carTurningAngle = CalculateTurningAngle(waypointDirection, carTurningRadius);
        if (carTurningAngle == 0)
        {
            carTurningDist = waypointDirection.z;
        }
        else
        {
            carTurningDist = CalculateTurningCircumference
                (carTurningAngle, carTurningRadius);
        }

        //change car inputs depending on waypointAngle
        //Steering

        maxSteering = carMovement.steerRange * carMovement.steerRangeFraction;
        // VERSION 1
        //Attempt to follow a more natural curve towards target waypoint
        if (carTurningAngle <= 10 && !reversing && !waypointAimStraight &&
            (Mathf.Abs(targetWaypoints[0].transform.eulerAngles.y - this.transform.eulerAngles.y) > 3))
        {
            steerIn = ((Mathf.Atan(2.4f / carTurningRadius)) * Mathf.Rad2Deg) / maxSteering;
            steerIn = Mathf.Clamp(steerIn, -1f, 1f);
            if (waypointAngle < 0)
            {
                steerIn *= -1;
            }
        }
        //Aim directly at target waypoint
        else if (waypointAngle > maxSteering)
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

        // VERSION 2
        ////figure out the turning angle from the targetWaypoint to the car
        //waypointDirectionTransform.position = waypointDirectionGlobal;
        //waypointDirectionTransform.rotation = targetWaypoints[0].rotation;
        //waypointDirectionToCarPosition = CalculateTurningEnd(waypointDirectionTransform, this.transform) * -1;
        //waypointDirectionToCarRadius = CalculateTurningRadius(waypointDirectionToCarPosition);
        //waypointDirectionToCarAngle = CalculateTurningAngle(waypointDirectionToCarPosition, waypointDirectionToCarRadius);
        ////figures out if its a left or right turn
        //if (waypointDirectionTransform.InverseTransformPoint(this.transform.position).x > 0)
        //{
        //    waypointDirectionToCarAngle *= -1;
        //}

        ////finds the global rotation that the car should be aiming for, relative to the car itself
        //steeringTargetRot = targetWaypoints[0].rotation;
        //steeringTargetRot *= Quaternion.AngleAxis(waypointDirectionToCarAngle, Vector3.up); //rotates around local Y axis
        //steeringTargetRot *= Quaternion.Inverse(transform.rotation); //makes it relative to the car
        //steeringTargetRotY = steeringTargetRot.eulerAngles.y;
        //if (steeringTargetRotY > 180)
        //{
        //    steeringTargetRotY -= 360;
        //}

        ////actual steering
        ////Ignores steering and goes straight if 1. steeringTargetRotY and waypointAngle are on opposite sides and 2. the car's aiming at the targetWaypoint
        //if (!
        //    ((Physics.Raycast(waypointRotationRay, out rayHit, Mathf.Infinity, waypointMask) && rayHit.transform == targetWaypoints[0])
        //    && (waypointAngle * steeringTargetRotY < 0)))
        //{
        //    if (steeringTargetRotY >= maxSteering)
        //    {
        //        steerIn = 1;
        //    }
        //    else if (steeringTargetRotY <= maxSteering * -1)
        //    {
        //        steerIn = -1;
        //    }
        //    else
        //    {
        //        steerIn = steeringTargetRotY / maxSteering;
        //        reversing = false;
        //    }
        //}
        //else
        //{
        //    steerIn = 0;
        //}
        ////Invert steering if going backwards
        //if (carMovement.currentSpeed < 0)
        //{
        //    steerIn *= -1;
        //}

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
                Mathf.Pow(carMovement.currentSpeed, 2) - (2 * brakingSpeed * turningDistTotal))
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
                        Mathf.Pow(carMovement.currentSpeed, 2) - (2 * brakingSpeed * turningDistTotal))
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

    static float GetFinalVelocity(float u, float a, float s)
    {
        float v2 = (u * u) - (2 * a * s);
        return Mathf.Sqrt(v2);
    }

    static float SetSteering(float desiredAngle, float maxAngle)
    {
        return (desiredAngle > maxAngle) ? 1 :
               (desiredAngle < -maxAngle) ? -1 :
               (desiredAngle / maxAngle);

        //if (desiredAngle > maxAngle)
        //{
        //    return 1;
        //}
        //else if (desiredAngle < maxAngle * -1)
        //{
        //    return -1;
        //}
        //else
        //{
        //    return desiredAngle / maxAngle;
        //}
    }

    static float SetMotor(float desiredSpeed, float currentSpeed)
    {
        float currentRelativeToDesired = currentSpeed - desiredSpeed;
        float proportional = 1;

        return (currentRelativeToDesired > proportional) ? -1 :
               (currentRelativeToDesired < -proportional) ? 1 :
               -(currentRelativeToDesired / proportional);
    }
}

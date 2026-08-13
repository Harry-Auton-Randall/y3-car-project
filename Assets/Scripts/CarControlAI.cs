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

    float motorIn;
    float steerIn;

    List<UpcomingWaypointInfo> upcomingWaypoints = new List<UpcomingWaypointInfo>();
    UpcomingWaypointInfo steeringArc;
    public int waypointsAhead = 4;

    Quaternion carRotRelativeToWaypoint;
    float carRotRelativeToWaypointY;
    Transform frontWheelMidpoint;
    Vector3 frontWheelMidpointDefaultPos;

    float maxSteering;
    float targetSpeed;
    float speedLimit = 999;
    float turningDistTotal;
    float brakingSpeed = 11;

    Transform targetWaypointRandomPos;
    public float waypointOffsetMult = 1f;

    Ray[] frontBackRays = new Ray[6];
    Ray waypointRotationRay;
    LayerMask waypointMask;
    RaycastHit rayHit;
    float frontRayDist = 2.3f;

    bool reversing = false;
    float timeStill;

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

    public void UpdateWaypoint(Collider newCurrentWaypoint)
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
            upcomingWaypoints[i] = tempUWI;
        }

        //Remove the 0th entry, because its the waypoint that was just passed and doesn't need to be targeted
        upcomingWaypoints.RemoveAt(0);
    }


    static Transform FindNextWaypoint(Collider[] nextWaypoints)
    {
        return (nextWaypoints.Length == 1) ? nextWaypoints[0].transform
            : nextWaypoints[Random.Range(0, nextWaypoints.Length)].transform;
    }

    void Update()
    {

        if (upcomingWaypoints == null || upcomingWaypoints.Count == 0)
        {
            motorIn = SetMotor(0, carMovement.currentSpeed);
            steerIn = 0;

            carMovement.SetMotorIn(motorIn);
            carMovement.SetSteerIn(steerIn);
            return;
        }

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
                //((waypointOffsetMult + 2*(1 - aiSkill)) / 3f)); //Average of randomness due to racer count, and randomness due to AI skill. The latter is weighted twice as much.
                Mathf.Max(waypointOffsetMult, 1 - aiSkill));
            targetWaypointRandomPos.rotation = upcomingWaypoints[1].pathingNode.rotation;
            closeToNextWaypoint = true;
        }
        else
        {
            targetWaypointRandomPos.position = upcomingWaypoints[0].pathingNode.position;
            targetWaypointRandomPos.position += (upcomingWaypoints[0].offsetDist * upcomingWaypoints[0].baseT.right *
                //((waypointOffsetMult + 2*(1 - aiSkill)) / 3f));
                Mathf.Max(waypointOffsetMult, 1 - aiSkill));
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

        //ACCELERATION
        steeringArc.turnSpeed = CalculateTurningSpeed(steeringArc.turnRadius, upcomingWaypoints[0].script.aiTurnSpeedMult * Mathf.Lerp(lowSkillTurnSpeedMult, 1, aiSkill));

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
    }

    static float SetMotor(float desiredSpeed, float currentSpeed)
    {
        float currentRelativeToDesired = currentSpeed - desiredSpeed;
        float proportional = 1;

        return (currentRelativeToDesired > proportional) ? -1 :
               (currentRelativeToDesired < -proportional) ? 1 :
               -(currentRelativeToDesired / proportional);
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
}

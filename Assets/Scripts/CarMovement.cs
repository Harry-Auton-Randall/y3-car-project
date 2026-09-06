using UnityEngine;
using System.Linq;

[System.Serializable] public struct WheelInfo
{
    public CarMovement.WheelEnd wheelEnd;
    public WheelCollider wheelCollider;
    public Transform wheelModel, brakeModel;
    public bool steerable;
    public float motorTractionMult;
    public float brakeTractionMult;

    public WheelInfo(
        CarMovement.WheelEnd wheelEndIn, 
        WheelCollider wheelColliderIn, 
        Transform wheelModelIn, 
        Transform brakeModelIn, 
        bool steerableIn
    ){
        wheelEnd = wheelEndIn;
        wheelCollider = wheelColliderIn;
        wheelModel = wheelModelIn;
        brakeModel = brakeModelIn;
        steerable = steerableIn;
        motorTractionMult = 1;
        brakeTractionMult = 1;
    }
}

public class CarMovement : MonoBehaviour
{
    public enum WheelDrive { Front, Rear, All};
    public enum WheelEnd { Front, Rear, None };
    public static bool IsWheelDriven(WheelEnd wheelRow, WheelDrive wheelDrive)
    {
        switch (wheelRow)
        {
            default:
            case WheelEnd.None:
                return false;
            case WheelEnd.Front:
                return wheelDrive == WheelDrive.Front || wheelDrive == WheelDrive.All;
            case WheelEnd.Rear:
                return wheelDrive == WheelDrive.Rear || wheelDrive == WheelDrive.All;
        }
    }

    //NEW HANDLING STUFF

    public bool steerRangeFalloffAtSpeed = true;

    //Car stats
    float torqueMotor;
    float torqueMotorTotal = 4000f;
    WheelDrive motorWheelDrive = WheelDrive.All;
    float torqueBrake;
    float torqueBrakeTotal = 6000f;
    WheelDrive brakeWheelDrive = WheelDrive.All;

    public float steerRange = 30.0f;
    public float steerRangeMinMult = 0.05f;

    public bool isPlayer;

    float maxSpeed = 90.0f;
    float maxSpeedReverse = 15.0f;

    public float boostMult = 1;
    float boostMultCurrent;
    bool boostOverheat;

    Collider currentWaypoint;
    public Collider[] nextWaypoints;
    Vector3 resetPosition = new Vector3(0, 3, 0);
    Quaternion resetRotation = Quaternion.identity;
    int waypointLayer;

    //Current variables
    public float motorIn, steerIn;
    public float currentSpeed;
    float currentSpeedLogic;

    float currentSpeedFraction;
    public float steerRangeFraction;

    //References to components/children
    Rigidbody rb;
    public WheelInfo[] wheelInfos;
    WheelInfo tempWI;

    //For the wheels
    Vector3 wheelPos;
    Quaternion wheelRot;

    //Respawning stuff
    public float respawnTimeTotal = 3;
    float respawnTime;
    bool respawnImmunity;
    LayerMask carMask;
    int carCollisions;

    LapManager lapManager;

    //Lap stuff
    int lap = 1;
    int lapWaypoint;
    Collider nextLapWaypoint;
    float nextLapWaypointDist;
    bool lapZero;
    Waypoint nextLapWaypointInfo;
    
    public int lapPub, lapWaypointPub;
    public float nextLapWaypointDistPub;
    public int positionPub;

    //lap start/end stuff
    bool raceStarted = false;
    bool finished = false;
    int id, totalLaps;

    //Lap time stuff
    float lapTimePrevious;
    public float lapTimeCurrent, lapTimeTotal, lapTimeBest;

    //Sfx stuff
    AudioSource audioSource;
    float[] gearSpeeds = new float[] { 3.4992f, 5.832f, 9.72f, 16.2f, 27, 45 };
    float revs;
    float revsGrad;
    int gear = 0;
    float averageWheelLinearVelocity;

    bool areAllWheelsAirborne = false;
    bool areDrivenWheelsAirborne = false;

    bool firstFrame = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        audioSource = transform.Find("EngineAudio").GetComponent<AudioSource>();
        lapManager = GameObject.Find("/LapManager").GetComponent<LapManager>();

        for(int i=0;i<wheelInfos.Length;i++)
        {
            wheelInfos[i].motorTractionMult = 1;
            wheelInfos[i].brakeTractionMult = 1;
            wheelInfos[i].wheelCollider.ConfigureVehicleSubsteps(5f, 10, 20);
        }

        int motorDrivenWheelsCount = wheelInfos.Count(x => IsWheelDriven(x.wheelEnd, motorWheelDrive));
        int brakeDrivenWheelsCount = wheelInfos.Count(x => IsWheelDriven(x.wheelEnd, brakeWheelDrive));
        torqueMotor = torqueMotorTotal / motorDrivenWheelsCount;
        torqueBrake = torqueBrakeTotal / brakeDrivenWheelsCount;

        waypointLayer = LayerMask.NameToLayer("Waypoint");
        carMask = (1 << LayerMask.NameToLayer("Car"));

        lapTimePrevious = 0;
        lapTimeCurrent = 0;
        lapTimeTotal = 0;
        lapTimeBest = 0;

        rb.constraints = RigidbodyConstraints.FreezeAll;
    }
    //void Start() - NO LONGER NEEDED
    //{
    //    SetStartPosition(startWaypoint);
    //}

    public void SetStartPosition(Collider startPosition, int idIn, int totalLapsIn)
    {
        currentWaypoint = startPosition;
        transform.position = new Vector3(
            currentWaypoint.transform.position.x,
            currentWaypoint.transform.position.y + 0.575f,
            currentWaypoint.transform.position.z);
        transform.rotation = currentWaypoint.transform.rotation;

        //Set up lap stuff
        nextLapWaypoint = currentWaypoint.GetComponent<Waypoint>().firstLapWaypoint;
        nextLapWaypointInfo = nextLapWaypoint.GetComponent<Waypoint>();
        lapWaypoint = nextLapWaypointInfo.lapWaypointValue - 1;
        if (currentWaypoint.GetComponent<Waypoint>().startBeforeLine)
        {
            lapZero = true;
        }
        
        id = idIn;
        totalLaps = totalLapsIn;

        UpdateWaypoint(currentWaypoint);
    }

    void UpdateWaypoint(Collider hitWaypoint)
    {
        currentWaypoint = hitWaypoint;
        resetPosition = currentWaypoint.transform.position;
        resetPosition.y += 3;
        resetRotation = currentWaypoint.transform.rotation;
        this.nextWaypoints = currentWaypoint.GetComponent<Waypoint>().nextWaypoints;
        //If AI-controlled, sends info update to CarControlAI
        if (!isPlayer)
        {
            GetComponent<CarControlAI>().UpdateWaypoint(this.currentWaypoint);
        }
    }

    public void SetMotorIn(float value)
    {
        motorIn = value;
    }
    public void SetSteerIn(float value)
    {
        steerIn = value;
    }

    public void ResetPosition()
    {
        if (raceStarted)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.position = resetPosition;
            transform.rotation = resetRotation;

            for (int i = 0; i < wheelInfos.Length; i++)
            {
                wheelInfos[i].wheelCollider.rotationSpeed = 0f;
            }

            respawnTime = 0;
            rb.excludeLayers = carMask;
            respawnImmunity = true;
            transform.Find("car/body").GetComponent<Renderer>().enabled = false;
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == waypointLayer)
        {
            for (int i = 0; i < nextWaypoints.Length; i++)
            {
                if (collision == nextWaypoints[i])
                {
                    UpdateWaypoint(collision);

                    //Reaching the next lapWaypoint
                    if (collision == nextLapWaypoint)
                    {
                        if (nextLapWaypointInfo.lapEnd)
                        {
                            if (lapZero)
                            {
                                lapZero = false;
                            }
                            else
                            {
                                //lapTimeBest updated here now
                                if (lapTimeBest == 0 || lapTimeBest > lapTimeCurrent)
                                {
                                    lapTimeBest = lapTimeCurrent;
                                }
                                if (lap == totalLaps && !finished)
                                {
                                    lapManager.RegisterFinish(id, lapTimePrevious + lapTimeCurrent, lapTimeBest); //CHANGED
                                    finished = true;
                                    
                                    if (isPlayer)
                                    {
                                        lapManager.DisplayPlayerResults(id); //CHANGED
                                        GetComponent<PlayerHudManager>().Disable();
                                    }
                                }
                                else
                                {
                                    lap += 1;
                                    lapTimePrevious += lapTimeCurrent;
                                    lapTimeCurrent = 0;

                                    //NEW
                                    if (isPlayer && !finished)
                                    {
                                        lapManager.LapUpdateSound();
                                    }
                                    
                                    if (lap == totalLaps && totalLaps != 1 && isPlayer)
                                    {
                                        StartCoroutine(lapManager.LastLapAlert()); //CHANGED
                                    }
                                }
                            }
                        }


                        lapWaypoint = nextLapWaypointInfo.lapWaypointValue;
                        nextLapWaypoint = nextLapWaypointInfo.nextLapWaypoint;
                        nextLapWaypointInfo = nextLapWaypoint.GetComponent<Waypoint>();
                        //updates waypointDist instantly instead of waiting for FixedUpdate
                        nextLapWaypointDist = Vector3.Distance(transform.position,
                            nextLapWaypoint.ClosestPoint(transform.position));
                    }

                    break;
                }
            }
        }

        if (collision.gameObject.tag == "CarTrigger")
        {
            carCollisions += 1;
        }
    }

    void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.tag == "CarTrigger")
        {
            carCollisions -= 1;
        }
    }
    //void OnTriggerStay(Collider collision)
    //{
    //    if (collision.gameObject.layer == waypointLayer && isPlayer)
    //    {
    //        collision.GetComponent<Waypoint>().UpdateRoute(this.transform.position, rb.linearVelocity);
    //    }
    //}

    
    public void EnableRaceStarted()
    {
        rb.constraints = RigidbodyConstraints.None;
        raceStarted = true;
    }


    //Traction control multiplier moves to 0 when slippage is too high, and 1 when otherwise, should hover around desired multiplier
    public static float AdjustTractionControl(
        WheelCollider wheel, 
        float startValue, 
        float adjustSpeed,
        bool braking,
        float delta
    ){
        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            float slip = braking ? hit.forwardSlip * -1 : hit.forwardSlip;
            float limit = wheel.forwardFriction.extremumSlip;

            if (slip > limit)
            {
                return Mathf.MoveTowards(startValue, 0, delta * adjustSpeed);
            }
        }

        return Mathf.MoveTowards(startValue, 1, delta * adjustSpeed);
    }

    void FixedUpdate()
    {
        //lapTime
        if (raceStarted)
        {
            lapTimeCurrent += Time.fixedDeltaTime;
            lapTimeTotal = lapTimeCurrent + lapTimePrevious;
        }

        //Disables the car's respawn immunity if enough time's passed
        //and it's not inside any other cars 
        if (respawnImmunity)
        {
            respawnTime += Time.fixedDeltaTime;
            if ((respawnTime >= respawnTimeTotal) && (carCollisions == 0))
            {
                rb.excludeLayers = 0;
                respawnImmunity = false;
                transform.Find("car/body").GetComponent<Renderer>().enabled = true; //CHANGED
            }
        }

        //Finds distance to the next lapWaypoint
        nextLapWaypointDist = Vector3.Distance(transform.position,
            nextLapWaypoint.ClosestPoint(transform.position));



        //Get average wheel linear velocity, and isAirborne
        averageWheelLinearVelocity = 0;
        int wheelDrivenCount = 0;
        areAllWheelsAirborne = true;
        areDrivenWheelsAirborne = true;
        for (int i = 0; i < wheelInfos.Length; i++)
        {
            if (IsWheelDriven(wheelInfos[i].wheelEnd, motorWheelDrive))
            {
                float wheelLinearVelocity = (wheelInfos[i].wheelCollider.radius * wheelInfos[i].wheelCollider.rotationSpeed * Mathf.Deg2Rad);

                averageWheelLinearVelocity += wheelLinearVelocity;
                wheelDrivenCount++;

                if (wheelInfos[i].wheelCollider.isGrounded)
                {
                    areDrivenWheelsAirborne = false;
                }
            }
            if (wheelInfos[i].wheelCollider.isGrounded)
            {
                areAllWheelsAirborne = false;
            }
        }
        if (wheelDrivenCount != 0)
        {
            averageWheelLinearVelocity /= wheelDrivenCount;
        }
        if (Mathf.Abs(averageWheelLinearVelocity) < 0.01f)
        {
            averageWheelLinearVelocity = 0f;
        }

        //Finds forward speed
        currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
        if (Mathf.Abs(currentSpeed) < 0.01f)
        {
            currentSpeed = 0f;
        }

        currentSpeedLogic = (areDrivenWheelsAirborne)
            ? averageWheelLinearVelocity
            : currentSpeed;

        //Finds value from 1 to 0 depending on how close currentSpeed is to maxSpeed
        if (currentSpeedLogic >= 0)
        {
            currentSpeedFraction = currentSpeedLogic / maxSpeed;
        }
        else
        {
            currentSpeedFraction = (currentSpeedLogic / maxSpeedReverse) * -1;
        }
        currentSpeedFraction = Mathf.Clamp(currentSpeedFraction, 0f, 1f);
        currentSpeedFraction = 1 - Mathf.Pow(currentSpeedFraction, 1.5f);

        //steerRangeFraction = (1-x)^2, x = currentSpeed / maxSpeed (NOT currentSpeedLogic)
        if (steerRangeFalloffAtSpeed)
        {
            steerRangeFraction = Mathf.Pow((-1 * Mathf.Clamp(currentSpeed / maxSpeed, 0f, 1f)) + 1, 2f);
            steerRangeFraction = Mathf.Lerp(steerRangeMinMult, 1, steerRangeFraction);
        }
        else
        {
            steerRangeFraction = 1;
        }

        for (int i=0;i<wheelInfos.Length;i++)
        {
            

            //steering
            if (wheelInfos[i].steerable)
            {
                wheelInfos[i].wheelCollider.steerAngle = steerIn * steerRange * steerRangeFraction * (
                    wheelInfos[i].wheelEnd == WheelEnd.Front ? 1
                  : wheelInfos[i].wheelEnd == WheelEnd.Rear  ? -1
                                                             : 0
                );
            }
            else
            {
                wheelInfos[i].wheelCollider.steerAngle = 0;
            }

            if (raceStarted)
            {
                //torque
                bool isBraking = (Mathf.Sign(motorIn) != Mathf.Sign(currentSpeedLogic) && currentSpeedLogic != 0f && motorIn != 0f);
                bool isDrivenMotor = IsWheelDriven(wheelInfos[i].wheelEnd, motorWheelDrive);
                bool isDrivenBrake = IsWheelDriven(wheelInfos[i].wheelEnd, brakeWheelDrive);

                wheelInfos[i].wheelCollider.motorTorque = 0f;
                wheelInfos[i].wheelCollider.brakeTorque = 0f;
                if (isBraking)
                {
                    if (isDrivenBrake)
                    {
                        wheelInfos[i].wheelCollider.brakeTorque = Mathf.Abs(motorIn * torqueBrake);
                    }
                }
                else
                {
                    if (isDrivenMotor)
                    {
                        wheelInfos[i].wheelCollider.motorTorque = motorIn * torqueMotor * currentSpeedFraction;
                    }
                }


                //traction control - it's commented out because it's not very good

                //float tractionAdjustSpeed = 2;

                //if (isBraking && isDrivenBrake)
                //{
                //    wheelInfos[i].brakeTractionMult = AdjustTractionControl(
                //        wheelInfos[i].wheelCollider,
                //        wheelInfos[i].brakeTractionMult,
                //        tractionAdjustSpeed,
                //        true,
                //        Time.fixedDeltaTime
                //    );

                //    wheelInfos[i].wheelCollider.brakeTorque *= wheelInfos[i].brakeTractionMult;
                //}
                //else
                //{
                //    wheelInfos[i].brakeTractionMult = Mathf.MoveTowards(wheelInfos[i].brakeTractionMult, 1, Time.fixedDeltaTime * tractionAdjustSpeed);
                //}

                //if (!isBraking && isDrivenMotor)
                //{
                //    wheelInfos[i].motorTractionMult = AdjustTractionControl(
                //        wheelInfos[i].wheelCollider,
                //        wheelInfos[i].motorTractionMult,
                //        tractionAdjustSpeed,
                //        false,
                //        Time.fixedDeltaTime
                //    );

                //    wheelInfos[i].wheelCollider.motorTorque *= wheelInfos[i].motorTractionMult;
                //}
                //else
                //{
                //    wheelInfos[i].motorTractionMult = Mathf.MoveTowards(wheelInfos[i].motorTractionMult, 1, Time.fixedDeltaTime * tractionAdjustSpeed);
                //}

                //
            }

            //meshes
        }

        firstFrame = false;
    }

    void Update()
    {
        lapPub = lap;
        lapWaypointPub = lapWaypoint;
        nextLapWaypointDistPub = nextLapWaypointDist;

        //WHEEL MESHES
        for (int i=0;i<wheelInfos.Length;i++)
        {
            float steerAnglePrior = wheelInfos[i].wheelCollider.steerAngle;

            wheelInfos[i].wheelCollider.GetWorldPose(out wheelPos, out wheelRot);

            wheelInfos[i].wheelModel.position = wheelPos;
            wheelInfos[i].wheelModel.rotation = wheelRot;

            wheelInfos[i].brakeModel.position = wheelPos;
            wheelInfos[i].brakeModel.rotation = wheelInfos[i].wheelCollider.transform.rotation;
            wheelInfos[i].brakeModel.Rotate(0, wheelInfos[i].wheelModel.localScale.x * (steerAnglePrior + 180), 0);
        }

        //ENGINE AUDIO
        if (areDrivenWheelsAirborne)
        {
            if (currentSpeedLogic >= 0)
            {
                revs = currentSpeedLogic / gearSpeeds[gear];
            }
            else
            {
                revs = (-currentSpeedLogic / maxSpeedReverse) * 2;
            }
        }
        else if (currentSpeedLogic >= 0)
        { 
            ShiftGear();
        }
        else
        {
            revs = (-currentSpeedLogic / maxSpeedReverse) * 2;
        }
        revs = Mathf.Clamp(revs, 0f, 2f);

        revsGrad = Mathf.MoveTowards(revsGrad, revs, 10 * Time.deltaTime);

        audioSource.pitch = 0.25f + (revsGrad * 0.75f);
    }

    void ShiftGear()
    {
        revs = currentSpeedLogic / gearSpeeds[gear];
        if (gear != 0 && revs < (0.6f))
        {
            gear -= 1;
            ShiftGear();
        }
        else if (gear != (gearSpeeds.Length - 1) && revs > (5f / 3f))
        {
            gear += 1;
            ShiftGear();
        }
    }
}

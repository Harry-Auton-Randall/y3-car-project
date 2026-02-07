using UnityEngine;

public class CarMovement : MonoBehaviour
{
    //Car stats
    public float torqueMotor = 1000.0f;
    public float torqueBrake = 1000.0f;
    public float steerRange = 30.0f;

    public float maxSpeed = 90.0f;
    public float maxSpeedReverse = 15.0f;

    //Current variables
    public float motorIn, steerIn;
    public float currentSpeed;

    public float currentSpeedFraction; 

    //References to components/children
    Rigidbody rb;
    public WheelCollider[] wheelColliders;
    public Transform[] wheelModels;

    //For the wheels
    Vector3 wheelPos;
    Quaternion wheelRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        wheelColliders = new WheelCollider[4];
        wheelColliders[0] = transform.Find("WheelFrontLeftCollider").GetComponent<WheelCollider>();
        wheelColliders[1] = transform.Find("WheelFrontRightCollider").GetComponent<WheelCollider>();
        wheelColliders[2] = transform.Find("WheelBackLeftCollider").GetComponent<WheelCollider>();
        wheelColliders[3] = transform.Find("WheelBackRightCollider").GetComponent<WheelCollider>();

        wheelModels = new Transform[4];
        wheelModels[0] = transform.Find("WheelFrontLeft");
        wheelModels[1] = transform.Find("WheelFrontRight");
        wheelModels[2] = transform.Find("WheelBackLeft");
        wheelModels[3] = transform.Find("WheelBackRight");
    }

    public void SetMotorIn(float value)
    {
        motorIn = value;
    }
    public void SetSteerIn(float value)
    {
        steerIn = value;
    }

    void FixedUpdate()
    {
        //Finds forward speed
        currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
        if (Mathf.Abs(currentSpeed) < 0.01f)
        {
            currentSpeed = 0f;
        }
        //Finds value from 1 to 0 depending on how close currentSpeed is to maxSpeed
        if (currentSpeed >= 0)
        {
            currentSpeedFraction = currentSpeed / maxSpeed;
        }
        else
        {
            currentSpeedFraction = (currentSpeed / maxSpeedReverse) * -1;
        }
        currentSpeedFraction = 1 - Mathf.Pow(Mathf.Clamp(currentSpeedFraction, 0f, 1f), 1.5f);


        //Steers front wheels
        wheelColliders[0].steerAngle = steerIn * steerRange;
        wheelColliders[1].steerAngle = steerIn * steerRange;

        //checks if desired direction is opposite to current direction, and that neither current speed or motorIn are 0
        //If true, cause braking instead of accelerating
        if (Mathf.Sign(motorIn) != Mathf.Sign(currentSpeed) && currentSpeed != 0f && motorIn != 0f)
        {
            for (int i=0;i<wheelColliders.Length;i++)
            {
                wheelColliders[i].motorTorque = 0f;
                wheelColliders[i].brakeTorque = Mathf.Abs(motorIn * torqueBrake);
            }
        }
        else
        {
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                wheelColliders[i].motorTorque = motorIn * torqueMotor * currentSpeedFraction;
                wheelColliders[i].brakeTorque = 0f;
            }
        }

        //make the wheel meshes match the wheel colliders
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            wheelColliders[i].GetWorldPose(out wheelPos, out wheelRot);
            wheelModels[i].transform.position = wheelPos;
            wheelModels[i].transform.rotation = wheelRot;
            wheelModels[i].transform.Rotate(0, 0, 90);
        }
    }
}

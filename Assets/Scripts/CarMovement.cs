using UnityEngine;

public class CarMovement : MonoBehaviour
{
    //Car stats
    public float torqueAccel = 1000.0f;
    public float torqueBrake = 1000.0f;

    //public float maxSpeed = 80.0f;
    //public float maxReverse = 16.0f;

    public float steerRange = 30.0f;
    public float steerFalloff = 0.2f;

    //Current variables
    public float accelIn, steerIn;
    public float currentSpeed;
    //float currentSteer;
    Vector3 wheelPos;
    Quaternion wheelRot;

    //References to components/children
    Rigidbody rb;
    public WheelCollider[] wheelColliders;
    public Transform[] wheelModels;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        wheelColliders = GetComponentsInChildren<WheelCollider>();

        wheelModels = new Transform[4];
        wheelModels[0] = transform.Find("WheelFrontLeft");
        wheelModels[1] = transform.Find("WheelFrontRight");
        wheelModels[2] = transform.Find("WheelBackLeft");
        wheelModels[3] = transform.Find("WheelBackRight");
    }

    public void SetAccelIn(float value)
    {
        accelIn = value;
    }
    public void SetSteerIn(float value)
    {
        steerIn = value;
    }

    void FixedUpdate()
    {
        currentSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);

        wheelColliders[0].steerAngle = steerIn * steerRange;
        wheelColliders[1].steerAngle = steerIn * steerRange;

        //checks if desired direction is opposite to current direction, and that neither current speed or accelIn are 0
        //If true, cause braking instead of accelerating
        if (Mathf.Sign(accelIn) != Mathf.Sign(currentSpeed))
        {
            for (int i=0;i<wheelColliders.Length;i++) //applies braking to all 4 wheels
            {
                wheelColliders[i].motorTorque = 0f;
                wheelColliders[i].brakeTorque = Mathf.Abs(accelIn * torqueBrake);
            }
        }
        else
        {
            wheelColliders[2].motorTorque = accelIn * torqueAccel;
            wheelColliders[3].motorTorque = accelIn * torqueAccel;
            for (int i = 0; i < wheelColliders.Length; i++)
            {
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

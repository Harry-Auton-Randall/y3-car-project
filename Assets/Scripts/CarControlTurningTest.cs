using UnityEngine;
//using UnityEngine.InputSystem;

public class CarControlTurningTest : MonoBehaviour
{
    public enum testType { turningRadius, braking };
    public testType test;

    CarMovement carMovement;
    //CameraMovement cameraMovement;

    //InputActionMap carActions;

    //InputAction motorAction, steerAction, resetPosAction;
    //InputAction camRotAction;
    //Vector2 camAngle;

    //TURNING RADIUS
    public float targetSpeed = 10f;
    public float targetAngle = 90f;
    public float targetAngleLocal;
    public Vector3 startPos, endPos;
    public int mode = 0;

    public float motorIn;
    public float steerIn = 0;
    public float maxSteering;

    public float resultRadius;

    Vector3 startingPos;
    Quaternion startingRot;
    public Collider startWaypoint;

    //BRAKING
    //targetSpeed from turning radius
    //mode from turning radius
    public float brakingTime;
    public float brakingSpeed;

    void Awake()
    {
        brakingTime = 0;
        carMovement = GetComponent<CarMovement>();

        startingPos = transform.position;
        startingRot = transform.rotation;
        carMovement.SetStartPosition(startWaypoint, -1, 1);
        transform.position = startingPos;
        transform.rotation = startingRot;
        
        //cameraMovement = GetComponent<CameraMovement>();

        //carActions = InputSystem.actions.FindActionMap("Car");

        //motorAction = carActions.FindAction("Motor");
        //steerAction = carActions.FindAction("Steer");
        //resetPosAction = carActions.FindAction("ResetPosition");
        //camRotAction = carActions.FindAction("CameraRotate");
    }
    void Start()
    {
        carMovement.EnableRaceStarted();
    }

    //void OnEnable()
    //{
    //    carActions.Enable();
    //    resetPosAction.performed += OnResetPos;
    //}
    //void OnDisable()
    //{
    //    carActions.Disable();
    //    resetPosAction.performed -= OnResetPos;
    //}

    //void OnResetPos(InputAction.CallbackContext context)
    //{
    //    carMovement.ResetPosition();
    //}

    void Update()
    {
        if (test == testType.turningRadius)
        {


            //carMovement.SetMotorIn(motorAction.ReadValue<float>());
            //carMovement.SetSteerIn(steerAction.ReadValue<float>());

            //start of turn
            if (carMovement.currentSpeed >= targetSpeed)
            {
                if (mode == 0)
                {
                    mode = 1;
                    targetAngle = 180f;
                    startPos = transform.position;
                }
            }

            //end of turn
            if (mode == 1)
            {
                endPos = transform.position;
                if (Mathf.Abs(endPos.z - startPos.z) >= Mathf.Abs(endPos.x - startPos.x))
                {
                    if (transform.rotation.eulerAngles.y >= 175f)
                    {
                        mode = 2;
                        resultRadius = Mathf.Abs(endPos.z - startPos.z);
                    }
                }
            }

            //steering
            maxSteering = carMovement.steerRange * carMovement.steerRangeFraction;
            targetAngleLocal = targetAngle - transform.rotation.eulerAngles.y;
            if (targetAngleLocal > maxSteering)
            {
                steerIn = 1;
            }
            else if (targetAngleLocal < maxSteering * -1)
            {
                steerIn = -1;
            }
            else
            {
                steerIn = targetAngleLocal / maxSteering;
            }

            //motor
            if (carMovement.currentSpeed <= targetSpeed)
            {
                motorIn = 1;
            }
            else
            {
                motorIn = -1;
            }

            //functions
            carMovement.SetMotorIn(motorIn);
            carMovement.SetSteerIn(steerIn);


            //camAngle = camRotAction.ReadValue<Vector2>();
            //if (camAngle.x == 0f && camAngle.y == 0f)
            //{
            //    camAngle.y = 1;
            //}
            //cameraMovement.SetCamRotIn(Mathf.Atan2(camAngle.x, camAngle.y) * Mathf.Rad2Deg);
        }

        else if (test == testType.braking)
        {

            if (mode == 0)
            {
                if (carMovement.currentSpeed >= 80)
                {
                    mode = 1;
                }
            }
            else if (mode == 1)
            {
                brakingTime += Time.deltaTime;
                if (carMovement.currentSpeed <= targetSpeed)
                {
                    brakingSpeed = (80f - targetSpeed) / brakingTime;
                    mode = 2;
                }
            }



            if (mode == 0)
            {
                motorIn = 1;
            }
            else if (mode == 1)
            {
                motorIn = -1;
            }
            else
            {
                motorIn = 0;
            }
            steerIn = 0;

            //functions
            carMovement.SetMotorIn(motorIn);
            carMovement.SetSteerIn(steerIn);
        }
    }
}

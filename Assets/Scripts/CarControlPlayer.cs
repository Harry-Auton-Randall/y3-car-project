using UnityEngine;
using UnityEngine.InputSystem;

public class CarControlPlayer : MonoBehaviour
{
    CarMovement carMovement;
    CameraMovement cameraMovement;

    InputActionMap carActions;

    InputAction motorAction, steerAction, resetPosAction;
    InputAction shiftGearUpAction, shiftGearDownAction;
    InputAction camRotAction;
    Vector2 camAngle;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        cameraMovement = GetComponent<CameraMovement>();

        carActions = InputSystem.actions.FindActionMap("Car");

        motorAction = carActions.FindAction("Motor");
        steerAction = carActions.FindAction("Steer");
        resetPosAction = carActions.FindAction("ResetPosition");
        camRotAction = carActions.FindAction("CameraRotate");

        shiftGearUpAction = carActions.FindAction("ShiftGear_Up");
        shiftGearDownAction = carActions.FindAction("ShiftGear_Down");
    }

    void OnEnable()
    {
        carActions.Enable();
        resetPosAction.performed += OnResetPos;
        shiftGearUpAction.performed += OnShiftGearUp;
        shiftGearDownAction.performed += OnShiftGearDown;

    }
    void OnDisable()
    {
        carActions.Disable();
        resetPosAction.performed -= OnResetPos;
        shiftGearUpAction.performed -= OnShiftGearUp;
        shiftGearDownAction.performed -= OnShiftGearDown;
    }

    void OnResetPos(InputAction.CallbackContext context)
    {
        carMovement.ResetPosition();
    }
    void OnShiftGearUp(InputAction.CallbackContext context)
    {
        carMovement.ShiftGearUp();
    }
    void OnShiftGearDown(InputAction.CallbackContext context)
    {
        carMovement.ShiftGearDown();
    }

    void Update()
    {
        carMovement.SetMotorIn(motorAction.ReadValue<float>());
        carMovement.SetSteerIn(steerAction.ReadValue<float>());

        camAngle = camRotAction.ReadValue<Vector2>();
        if (camAngle.x == 0f && camAngle.y == 0f)
        {
            camAngle.y = 1;
        }
        cameraMovement.SetCamRotIn(Mathf.Atan2(camAngle.x, camAngle.y) * Mathf.Rad2Deg);
    }
}

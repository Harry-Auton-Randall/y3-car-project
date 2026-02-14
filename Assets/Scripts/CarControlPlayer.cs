using UnityEngine;
using UnityEngine.InputSystem;

public class CarControlPlayer : MonoBehaviour
{
    CarMovement carMovement;
    CameraMovement cameraMovement;

    InputActionMap carActions;

    InputAction motorAction, steerAction, resetPosAction;
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
    }

    void OnEnable()
    {
        carActions.Enable();
        resetPosAction.performed += OnResetPos;
    }
    void OnDisable()
    {
        carActions.Disable();
        resetPosAction.performed -= OnResetPos;
    }

    void OnResetPos(InputAction.CallbackContext context)
    {
        carMovement.ResetPosition();
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

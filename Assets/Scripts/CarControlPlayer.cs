using UnityEngine;
using UnityEngine.InputSystem;

public class CarControlPlayer : MonoBehaviour
{
    CarMovement carMovement;
    CameraMovement cameraMovement;
    InputAction motorAction, steerAction, camRotAction;
    Vector2 camAngle;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        cameraMovement = GetComponent<CameraMovement>();
        motorAction = InputSystem.actions.FindAction("Motor");
        steerAction = InputSystem.actions.FindAction("Steer");
        camRotAction = InputSystem.actions.FindAction("CameraRotate");
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

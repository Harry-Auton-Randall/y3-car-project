using UnityEngine;
using UnityEngine.InputSystem; // 1

public class CarControlPlayer : MonoBehaviour
{
    // 2
    CarMovement carMovement;
    InputAction motorAction, steerAction;

    void Awake() // 3
    {
        carMovement = GetComponent<CarMovement>();
        motorAction = InputSystem.actions.FindAction("Motor");
        steerAction = InputSystem.actions.FindAction("Steer");
    }

    void Update() // 4
    {
        carMovement.SetMotorIn(motorAction.ReadValue<float>());
        carMovement.SetSteerIn(steerAction.ReadValue<float>());
    }
}

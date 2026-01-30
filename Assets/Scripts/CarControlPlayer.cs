using UnityEngine;
using UnityEngine.InputSystem;

public class CarControlPlayer : MonoBehaviour
{
    CarMovement carMovement;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
    }

    void OnAccel(InputValue value)
    {
        carMovement.SetAccelIn(value.Get<float>());
    }
    void OnSteer(InputValue value)
    {
        carMovement.SetSteerIn(value.Get<float>());
    }
}

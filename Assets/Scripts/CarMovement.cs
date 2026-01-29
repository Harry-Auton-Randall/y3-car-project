using UnityEngine;

public class CarMovement : MonoBehaviour
{
    //Car stats
    public float torqueAccel = 1000.0f;
    public float torqueBrake = 1000.0f;

    public float maxSpeed = 80.0f;
    public float maxReverse = 16.0f;

    public float steerRange = 30.0f;
    public float steerFalloff = 0.2f;

    //Current variables
    float accelIn, steerIn;
    float currentSpeed, currentSteer;


    //References to components/children
    
    Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

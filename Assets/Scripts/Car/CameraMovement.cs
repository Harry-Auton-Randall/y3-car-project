using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    //Vector3 moveDir;
    //float rbRotY;

    //float camBase0RotX, camBase0RotY;
    Quaternion camBaseActualGlobalRot, camBaseTargetGlobalRot;
    
    float camRotYIn;
    float camRotSpeed = 18.0f;
    // float camRotDist;

    //float camRotXGrad; //NEW
    //float camRotXSpeed = 5; //NEW

    Rigidbody rb;
    Transform camBaseActual, camBaseTarget, camBase1;
    CarMovement cm;
    bool isTumbling;

    bool isZRotationLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camBaseActual = transform.Find("CameraBaseActual");
        camBaseTarget = transform.Find("CameraBaseTarget");
        camBase1 = transform.Find("CameraBaseTarget/CameraBase1");
        cm = GetComponent<CarMovement>();
    }

    public void SetCamRotIn(float value)
    {
        camRotYIn = value;
    }

    float Pyth(float a, float b)
    {
        return Mathf.Sqrt((a * a) + (b * b));
    }

    void OnCollisionEnter(Collision collision)
    {
        isTumbling = true;
    }

    void Update()
    {
        if (!cm.areWheelsTouchingValidSurface)
        {
            Vector3 moveDir = rb.linearVelocity;

            float xzMag = Pyth(moveDir.x, moveDir.z);

            if (moveDir.magnitude < 0.5f)
            {
                moveDir = transform.forward;
            }
            else if (xzMag < 0.5f)
            {
                moveDir.x = transform.forward.x * 0.1f;
                moveDir.z = transform.forward.z * 0.1f;
            }
            xzMag = Pyth(moveDir.x, moveDir.z);

            moveDir.y = Mathf.Clamp(
                moveDir.y, 
                -0.5f * Mathf.Abs(xzMag), 
                0.5f * Mathf.Abs(xzMag)
            );
            if (isTumbling){
                camBaseTargetGlobalRot = Quaternion.RotateTowards(camBaseTargetGlobalRot, Quaternion.LookRotation(moveDir), 200 * Time.deltaTime);
                camBaseTarget.rotation = camBaseTargetGlobalRot;
                camBaseTarget.Rotate(Vector3.up * camRotYIn);
            }else{
                camBaseTarget.rotation = Quaternion.LookRotation(moveDir);
                camBaseTarget.Rotate(Vector3.up * (camRotYIn - camBaseTarget.localEulerAngles.y));
                camBaseTargetGlobalRot = camBaseTarget.rotation;
            }
            
            
            
        }
        else
        {
            isTumbling = false;

            Ray floorRay = new Ray(transform.position, transform.up * -1);
            if (Physics.Raycast(floorRay, out RaycastHit rayHit, 9999, cm.validRoadSurfacesMask))
            {
                camBaseTarget.rotation = Quaternion.LookRotation(rayHit.normal);
                camBaseTarget.Rotate(Vector3.right * 90);
                camBaseTarget.Rotate(Vector3.up * (camRotYIn - camBaseTarget.localEulerAngles.y));
            }
        }

        float camRotDist = 1.0f - Mathf.Exp(camRotSpeed * -1 * Time.deltaTime);
        camBaseActualGlobalRot = Quaternion.Lerp(camBaseActualGlobalRot, camBaseTarget.rotation, camRotDist);
        camBaseActual.rotation = camBaseActualGlobalRot;
        if (isZRotationLocked){
            camBaseActual.rotation = Quaternion.Euler(camBaseActual.eulerAngles.x, camBaseActual.eulerAngles.y, 0);
        }
    }

    //void FixedUpdate()
    //{
    //    //Gets movement direction of Rigidbody
    //    moveDir = (rb.linearVelocity);
    //    if (moveDir.magnitude < 0.1f)
    //    {
    //        moveDir = Vector3.zero;
    //    }

    //    //Makes camBase0 point in movement direction
    //    rbRotY = rb.transform.eulerAngles.y;
    //    if (moveDir != Vector3.zero)
    //    {
    //        camBase0.rotation = Quaternion.LookRotation(moveDir);
    //        if (moveDir.x == 0f && moveDir.z == 0f)
    //        {
    //            camBase0.rotation = Quaternion.Euler(camBase0.eulerAngles.x, rbRotY, 0);
    //        }
    //    }
    //    else
    //    {
    //        camBase0.rotation = Quaternion.identity;
    //    }

    //    //NEW - reduces camBase0RotX for slow movements
    //    camRotXGrad = moveDir.magnitude / camRotXSpeed;
    //    camRotXGrad = Mathf.Clamp(camRotXGrad, 0f, 1f);

    //    //Clamps camBase0's x rotation between 30 and -30, and accounts for camRotXGrad
    //    camBase0RotX = camBase0.eulerAngles.x;
    //    if (camBase0RotX > 180)
    //    {
    //        camBase0RotX = Mathf.Clamp(camBase0RotX, 360f - (30 * camRotXGrad), 360);
    //    }
    //    else
    //    {
    //        camBase0RotX = Mathf.Clamp(camBase0RotX, 0, 30 * camRotXGrad);
    //    }

    //    camBase0.rotation = Quaternion.Euler(camBase0RotX, camBase0.eulerAngles.y, camBase0.eulerAngles.z);

    //    //Rotates camBase1 to point in direction of car + input
    //    camBase0RotY = camBase0.eulerAngles.y;
    //    camBase1.localRotation = Quaternion.Euler(0, (camBase0RotY * -1) + camRotIn + rbRotY, 0);

    //    //Rotates camBaseRot gradually towards camBase1
    //    camRotDist = 1.0f - Mathf.Exp(camRotSpeed * -1 * Time.fixedDeltaTime);
    //    camBaseRot = Quaternion.Lerp(camBaseRot, camBase1.rotation, camRotDist);
    //}
    //void Update()
    //{
    //    //Sets camBase's rotation to camBaseRot on Update instead of fixedUpdate, also resets z rotation
    //    camBase.rotation = camBaseRot;
    //    camBase.rotation = Quaternion.Euler(camBase.eulerAngles.x, camBase.eulerAngles.y, 0);
    //}
}

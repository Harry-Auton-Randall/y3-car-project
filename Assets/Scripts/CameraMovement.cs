using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Vector3 moveDir;
    float rbRotY;

    float camBase0RotX, camBase0RotY;
    Quaternion camBaseRot;
    
    public float camRotIn = 0f;
    public float camRotSpeed = 18.0f;
    float camRotDist;

    float camRotXGrad; //NEW
    float camRotXSpeed = 5; //NEW

    Rigidbody rb;
    Transform camBase, camBase0, camBase1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camBase = transform.Find("CameraBase");
        camBase0 = transform.Find("CameraBase0");
        camBase1 = transform.Find("CameraBase0/CameraBase1");
    }

    public void SetCamRotIn(float value)
    {
        camRotIn = value;
    }

    void FixedUpdate()
    {
        //Gets movement direction of Rigidbody
        moveDir = (rb.linearVelocity);
        if (moveDir.magnitude < 0.1f)
        {
            moveDir = Vector3.zero;
        }

        //Makes camBase0 point in movement direction
        rbRotY = rb.transform.eulerAngles.y;
        if (moveDir != Vector3.zero)
        {
            camBase0.rotation = Quaternion.LookRotation(moveDir);
            if (moveDir.x == 0f && moveDir.z == 0f)
            {
                camBase0.rotation = Quaternion.Euler(camBase0.eulerAngles.x, rbRotY, 0);
            }
        }
        else
        {
            camBase0.rotation = Quaternion.identity;
        }

        //NEW - reduces camBase0RotX for slow movements
        camRotXGrad = moveDir.magnitude / camRotXSpeed;
        camRotXGrad = Mathf.Clamp(camRotXGrad, 0f, 1f);

        //Clamps camBase0's x rotation between 30 and -30, and accounts for camRotXGrad
        camBase0RotX = camBase0.eulerAngles.x;
        if (camBase0RotX > 180)
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 360f - (30 * camRotXGrad), 360);
        }
        else
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 0, 30 * camRotXGrad);
        }

        camBase0.rotation = Quaternion.Euler(camBase0RotX, camBase0.eulerAngles.y, camBase0.eulerAngles.z);

        //Rotates camBase1 to point in direction of car + input
        camBase0RotY = camBase0.eulerAngles.y;
        camBase1.localRotation = Quaternion.Euler(0, (camBase0RotY * -1) + camRotIn + rbRotY, 0);

        //Rotates camBaseRot gradually towards camBase1
        camRotDist = 1.0f - Mathf.Exp(camRotSpeed * -1 * Time.fixedDeltaTime);
        camBaseRot = Quaternion.Lerp(camBaseRot, camBase1.rotation, camRotDist);
    }
    void Update()
    {
        //Sets camBase's rotation to camBaseRot on Update instead of fixedUpdate, also resets z rotation
        camBase.rotation = camBaseRot;
        camBase.rotation = Quaternion.Euler(camBase.eulerAngles.x, camBase.eulerAngles.y, 0);
    }
}

using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Vector3 moveDir;
    public float rbRotY;

    public float camBase0RotX, camBase0RotY;
    public Quaternion camBase2Rot;
    
    public float camRotIn = 0f;
    public float camRotSpeed = 0.3f;

    Rigidbody rb;
    Transform camBase, camBase0, camBase1, camBase2;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camBase = transform.Find("CameraBase");
        camBase0 = transform.Find("CameraBase/CameraBase0");
        camBase1 = transform.Find("CameraBase/CameraBase0/CameraBase1");
        camBase2 = transform.Find("CameraBase/CameraBase2");
    }

    void SetCamRotIn(float value)
    {
        camRotIn = value;
    }

    void FixedUpdate()
    {
        //Get movement direction of Rigidbody
        moveDir = (rb.linearVelocity);
        if (Vector3.Distance(Vector3.zero, moveDir) < 0.1f)
        {
            moveDir = Vector3.zero;
        }

        //Align camBase with world axis
        camBase.rotation = Quaternion.identity;

        //Make camBase0 point in movement direction
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

        //Clamps camBase0's x rotation between 30 and -30
        camBase0RotX = camBase0.eulerAngles.x;
        if (camBase0RotX > 180)
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 330, 360);
        }
        else
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 0, 30);
        }
        camBase0.rotation = Quaternion.Euler(camBase0RotX, camBase0.eulerAngles.y, camBase0.eulerAngles.z);

        //Rotate camBase1 to point in direction of car + input
        camBase0RotY = camBase0.eulerAngles.y;
        camBase1.localRotation = Quaternion.Euler(0, (camBase0RotY * -1) + camRotIn + rbRotY, 0);

        //Rotate camBase2Rot gradually towards camBase1
        camBase2Rot = Quaternion.Lerp(camBase2Rot, camBase1.rotation, camRotSpeed);
    }
    void Update()
    {
        //Set camBase2's rotation to camBase2Rot on update instead of fixedUpdate, also resets z rotation
        camBase2.rotation = camBase2Rot;
        camBase2.rotation = Quaternion.Euler(camBase2.eulerAngles.x, camBase2.eulerAngles.y, 0);
    }
}

using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Vector3 moveDir;
    public float camBase0RotX, camBase0RotY;
    public float rbRotY;
    
    public float camRotIn = 0f;

    Rigidbody rb;
    Transform camBase, camBase0, camBase1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camBase = transform.Find("CameraBase");
        camBase0 = transform.Find("CameraBase0");
        camBase1 = transform.Find("CameraBase0/CameraBase1");
    }

    void SetCamRotIn(float value)
    {
        camRotIn = value;
    }

    void Update()
    {
        //Get movement direction of Rigidbody
        moveDir = (rb.linearVelocity);
        if (Vector3.Distance(Vector3.zero, moveDir) < 0.1f)
        {
            moveDir = Vector3.zero;
        }

        //Make camBase0 point in movement direction
        if (moveDir != Vector3.zero)
        {
            camBase0.rotation = Quaternion.LookRotation(moveDir);
        }
        else
        {
            camBase0.rotation = Quaternion.identity;
        }

        camBase0RotX = camBase0.eulerAngles.x;
        camBase0RotY = camBase0.eulerAngles.y;
        rbRotY = rb.transform.eulerAngles.y;

        //Clamps camBase0's x rotation between 30 and -30
        if (camBase0RotX > 180)
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 330, 360);
        }
        else
        {
            camBase0RotX = Mathf.Clamp(camBase0RotX, 0, 30);
        }
        camBase0.rotation = Quaternion.Euler(camBase0RotX, camBase0.eulerAngles.y, camBase0.eulerAngles.z);

        //camBase1 rotation - rotates around local y axis to face car's direction, then resets global z rotation
        camBase1.localRotation = Quaternion.Euler(0, (camBase0RotY * -1) + camRotIn + rbRotY, 0);
        camBase1.rotation = Quaternion.Euler(camBase1.eulerAngles.x, camBase1.eulerAngles.y, 0);

        //TEMPORARY - camBase instantly snaps to camBase1, need to make it rotate smoothly.
        camBase.rotation = camBase1.rotation;
    }
}

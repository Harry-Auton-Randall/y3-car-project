using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Vector3 moveDir;
    public float camBaseRotY;
    public float rbRotY;
    public float rotateIn;

    Rigidbody rb;
    Transform camBase0, camBase1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camBase0 = transform.Find("CameraBase0");
        camBase1 = transform.Find("CameraBase0/CameraBase1");
    }

    void Update()
    {
        //Gets velocity of rigidbody, sets to 0 if too low like currentSpeed
        moveDir = (rb.linearVelocity);
        if (Vector3.Distance(Vector3.zero, moveDir) < 0.1f)
        {
            moveDir = Vector3.zero;
        }

        //updates camBase0's rotation
        if (moveDir != Vector3.zero)
        {
            camBase0.rotation = Quaternion.LookRotation(moveDir);
        }

        rbRotY = rb.transform.eulerAngles.y; //rigidbody's y rotation
        camBaseRotY = camBase0.eulerAngles.y; //camBase0's y rotation

        camBase1.localRotation = Quaternion.Euler(0, (camBaseRotY * -1) + rotateIn + rbRotY, 0); //rotates camBase1 around local axis, so it looks up/down when car is moving up/down
        camBase1.rotation = Quaternion.Euler(camBase1.eulerAngles.x, camBase1.eulerAngles.y, 0); // resets z-rotation
    }
}

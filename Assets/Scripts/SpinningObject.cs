using UnityEngine;

public class SpinningObject : MonoBehaviour
{
    public float spinSpeed;

    void Update()
    {
        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y + (spinSpeed * Time.deltaTime), 0);
    }
}

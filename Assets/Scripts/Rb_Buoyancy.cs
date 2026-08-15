using UnityEngine;
using System.Collections.Generic;

public class Rb_Buoyancy : MonoBehaviour
{
    Rigidbody rb;
    LayerMask waterLayer;

    List<Collider> inColliders = new List<Collider>();
    float waterColliderTotalDragMult = 0;
    int waterColliderNo = 0;
    bool inWater = false;

    float dragNormal, dragRotNormal;
    public float dragWater = 1;
    public float dragRotWater = 1;
    float dragWaterMult = 1;

    float buoyancy;
    public float buoyancyChangeRate = 0;
    public float buoyancyEmpty = 0;
    public float buoyancyFull = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        waterLayer = LayerMask.NameToLayer("Water");

        dragNormal = rb.linearDamping;
        dragRotNormal = rb.angularDamping;
        buoyancy = buoyancyEmpty;
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == waterLayer && !inColliders.Contains(collision))
        {
            inColliders.Add(collision);
            waterColliderNo++;
            if (collision.TryGetComponent<WaterInfo>(out WaterInfo waterInfo))
            {
                waterColliderTotalDragMult += waterInfo.dragMult;
            }
            else
            {
                waterColliderTotalDragMult++;
            }
        }
    }
    void OnTriggerExit(Collider collision)
    {
        if (inColliders.Contains(collision))
        {
            inColliders.Remove(collision);
            waterColliderNo--;
            if (collision.TryGetComponent<WaterInfo>(out WaterInfo waterInfo))
            {
                waterColliderTotalDragMult -= waterInfo.dragMult;
            }
            else
            {
                waterColliderTotalDragMult--;
            }
        }
    }

    void FixedUpdate()
    {
        inWater = waterColliderNo > 0;

        if (inWater)
        {
            dragWaterMult = waterColliderTotalDragMult / waterColliderNo;

            rb.linearDamping = dragWater * dragWaterMult;
            rb.angularDamping = dragRotWater * dragWaterMult;
            buoyancy = Mathf.MoveTowards(buoyancy, buoyancyFull, (buoyancyChangeRate / dragWaterMult) * Time.fixedDeltaTime); //more viscous liquid enters/leaves the rigidbody slower

            rb.useGravity = false;
            rb.AddForce(Physics.gravity * rb.mass * -buoyancy);
        }
        else
        {
            rb.linearDamping = dragNormal;
            rb.angularDamping = dragRotNormal;
            buoyancy = Mathf.MoveTowards(buoyancy, buoyancyEmpty, (buoyancyChangeRate / dragWaterMult) * Time.fixedDeltaTime);

            rb.useGravity = true;
        }
    }
}

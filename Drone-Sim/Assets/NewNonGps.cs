using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewNonGps : MonoBehaviour
{
    private GameObject Lead;
    public Rigidbody rb;
    private bool start = false;
    private float dx, dy, dz;
    private float dx_intial, dy_initial, dz_initial;
    // Start is called before the first frame update
    void Start()
    {
    }

    private Vector3 velocity = Vector3.zero;
    // Update is called once per frame
    void FixedUpdate()
    {
        Lead = GameObject.FindGameObjectsWithTag("Leader")[0];
        if (!start)
        {
            dx = Lead.transform.position.x - this.transform.position.x;
            dy = Lead.transform.position.y - this.transform.position.y;
            dz = Lead.transform.position.z - this.transform.position.z;
            dx_intial = dx;
            dy_initial = dy;
            dz_initial = dz;
            start = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, Lead.transform.TransformPoint(new Vector3(-dx_intial, -dy_initial, -dz_initial)), ref velocity, 0.3f);
        }

        //transform.position = Vector3.SmoothDamp(transform.position, Lead.transform.TransformPoint(new Vector3(-dx, -dy, -dz)), ref velocity, 0.3f);
        //Debug.Log(transform.position.x + " " + transform.position.y + " " + transform.position.z);
    }
}

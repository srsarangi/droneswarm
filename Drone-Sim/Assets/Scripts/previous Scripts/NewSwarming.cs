using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewSwarming : MonoBehaviour
{
    private GameObject Lead;
    public Rigidbody rb;
    private float dx, dy, dz;

    // Start is called before the first frame update
    void Start()
    {
        Lead = GameObject.FindGameObjectsWithTag("Leader")[0];
        dx = Lead.transform.position.x - this.transform.position.x;
        dy = Lead.transform.position.y - this.transform.position.y;
        dz = Lead.transform.position.z - this.transform.position.z;
    }

    private Vector3 velocity = Vector3.zero;
    // set posiston of droen behind the leader drone
    //public Vector3 behindPosition = new Vector3 (0,0,0);
    public float angle;
    // keep following the leader drone with position offset
    void FixedUpdate(){
        rb.useGravity = false;
        
        transform.position = Vector3.SmoothDamp(transform.position, Lead.transform.TransformPoint(new Vector3(-dx,dy,-dz)), ref velocity, 1.0f);
          //Debug.Log(transform.position.x + " " + transform.position.y + " " + transform.position.z);

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// make droens follow the leader drone
// can also be implemented by making droens as children of leader drone
public class Swarming : MonoBehaviour
{
   public Transform myDrone;
   public Rigidbody rb;

   void Awake(){
     	//myDrone= GameObject.FindGameObjectWithTag("Leader").transform;
    
   }

   private Vector3 velocity = Vector3.zero;
   // set posiston of droen behind the leader drone
   public Vector3 behindPosition = new Vector3 (0,0,0);
   public float angle;
   // keep following the leader droen with position offset
   void FixedUpdate(){

     rb.useGravity = false;
     transform.position = Vector3.SmoothDamp(transform.position, myDrone.transform.TransformPoint(behindPosition), ref velocity, 1.0f);
   	// transform.rotation = Quaternion.Euler(new Vector3 (angle, myDrone.GetComponent<DroneMovementScript>().correntYRotation, 0));
   }
}

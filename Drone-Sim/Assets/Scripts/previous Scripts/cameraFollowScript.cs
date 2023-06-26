// Tutorial
// https://www.youtube.com/watch?v=XRe8Zt42Z1Y

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraFollowScript : MonoBehaviour
{
	private Transform ourDrone;
    void Awake()
    {
        // find the drone and store its location in ourDrone private variable 
        ourDrone = GameObject.FindGameObjectWithTag("RedDrone").transform;
    }

   private Vector3 velocityCameraFollow;  // cordinates of camera 
   public Vector3 behindPosition = new Vector3(0,2,-4);  // define howfar behind the drone camera will follow
    public float angle;
   void FixedUpdate(){
        // we want ot follow the drone so we will fetch the current position of the drone (using ourDrone.transform)and the transform position of camera TransformPoint whth behind position 
        transform.position = Vector3.SmoothDamp(transform.position, ourDrone.transform.TransformPoint(behindPosition) +Vector3.up * Input.GetAxis("Vertical"), ref velocityCameraFollow,0.1f);
    
     // we want to rotete the camera in the direction of the droen so we fetch the current Y rotaion of the drone, we are fetching it from drone movement script as it is defined there

    //transform.rotation = Quaternion.Euler(new Vector3 (angle,ourDrone.GetComponent<DroneMovementScript>(). currentYRotation,0));

   }

}

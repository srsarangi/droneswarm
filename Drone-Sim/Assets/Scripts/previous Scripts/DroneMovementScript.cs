

// for manual movement of drone 
//Tutorial Followed https://www.youtube.com/watch?v=3R_V4gqTs_I
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DroneMovementScript : MonoBehaviour
{
    Rigidbody ourDrone;
    void Awake(){

        ourDrone = GetComponent<Rigidbody>();
         droneSound = gameObject.transform.Find("drone_sound").GetComponent<AudioSource>();
    }
    public int numberOfRays = 12;
public float angle = 60;
public float targetVelocity = 10.0f;
public float rayRange = 14;
    


    // Update is called once per frame
    void FixedUpdate()   {
        MovementUpDown();           // for  vertical movement of drone
        MovementForward();          // for forward movement of drone    	
        Swerwing();                 // for sideways movement of drone
        Rotation();                 // For rotation of drone about its axis 
        DroneSound();               // to implement drone sound
        ClampingSpeedValues();      // to clamp the max speed value 

        ourDrone.AddRelativeForce(Vector3.up * upForce);
        ourDrone.rotation = Quaternion.Euler(
        new Vector3(tiltAmountForward, currentYRotation, tiltAmountSideways)
            );


    }
    // for  vertical movement of drone
    public float upForce;
    void MovementUpDown (){
        if ((Mathf.Abs(Input.GetAxis("Vertical"))>0.2f||Mathf.Abs(Input.GetAxis("Horizontal"))>0.2f )){
            // I us to make drone move up words and K will make drone go down
            if (Input.GetKey(KeyCode.I)||Input.GetKey(KeyCode.K)){
                ourDrone.velocity = ourDrone.velocity;
            }
            
        
        if (!Input.GetKey(KeyCode.I) && !Input.GetKey(KeyCode.K) && !Input.GetKey(KeyCode.J) && !Input.GetKey(KeyCode.L)){
            ourDrone.velocity = new Vector3(ourDrone.velocity.x,Mathf.Lerp(ourDrone.velocity.y,0,Time.deltaTime *5),ourDrone.velocity.z);
            upForce= 281;
        }
        if (!Input.GetKey(KeyCode.I) && !Input.GetKey(KeyCode.K) && (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.L))){
                ourDrone.velocity = new Vector3(ourDrone.velocity.x,Mathf.Lerp(ourDrone.velocity.y,0,Time.deltaTime *5),ourDrone.velocity.z);
                upForce= 110;
            }
        if (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.L)){
            upForce= 410;
        }
    }
    if ((Mathf.Abs(Input.GetAxis("Vertical"))<0.2f||Mathf.Abs(Input.GetAxis("Horizontal"))>0.2f )){
            upForce = 135;
    }
    	if (Input.GetKey(KeyCode.I)){
    		upForce = 450;
            if (Mathf.Abs(Input.GetAxis("Horizontal"))>0.2f){
                upForce= 500;
            }
    	}
    	else if (Input.GetKey(KeyCode.K)){
    		upForce = -200;
    	}
    	else if (!Input.GetKey(KeyCode.I) && !Input.GetKey(KeyCode.K) && (Mathf.Abs(Input.GetAxis("Vertical"))<0.2f && Mathf.Abs(Input.GetAxis("Horizontal")) <0.2f )){
    		upForce = 98.1f;
    		// Debug.Log("anit gravity not working");

    	}
    }
    // for forward movement of drone  
    public float movementForwardSpeed = 500.0f;
    private float tiltAmountForward = 0;
    private float tiltVilocityForward;
    void MovementForward(){
    	if (Input.GetAxis("Vertical")!= 0){
    		ourDrone.AddRelativeForce(Vector3.forward *Input.GetAxis("Vertical") * movementForwardSpeed);
    		//tiltAmountForward = Mathf.SmoothDamp(tiltAmountForward, 10 * Input.GetAxis("Vertical"), ref tiltVilocityForward, 0.1f);



    	}
    }
        // for sideways movement of drone
        private float sideMovementAmount =300.0f;
    	private float tiltAmountSideways ;
    	private float tiltAmountVelocity;
    	private void Swerwing(){

    		if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.2f){
    			ourDrone.AddRelativeForce(Vector3.right * Input.GetAxis("Horizontal") * sideMovementAmount  );
    			//tiltAmountSideways= Mathf.SmoothDamp(tiltAmountSideways, -10* Input.GetAxis("Horizontal"), ref tiltAmountVelocity, 0.1f);

    		}
    		else {
    			//tiltAmountSideways= Mathf.SmoothDamp(tiltAmountSideways, 0, ref tiltAmountVelocity, 0.1f);
    		}
    	} 
    private Vector3 velocityToSmoothDampToZero;
    void ClampingSpeedValues(){
    	if (Mathf.Abs(Input.GetAxis("Vertical"))>0.2f && Mathf.Abs(Input.GetAxis("Horizontal"))>0.2f ){
    		ourDrone.velocity = Vector3.ClampMagnitude(ourDrone.velocity,Mathf.Lerp(ourDrone.velocity.magnitude,10.0f,Time.deltaTime * 5f));

    	}
    	if (Mathf.Abs(Input.GetAxis("Vertical"))>0.2f && Mathf.Abs(Input.GetAxis("Horizontal"))<0.2f ){
    		ourDrone.velocity = Vector3.ClampMagnitude(ourDrone.velocity,Mathf.Lerp(ourDrone.velocity.magnitude,10.0f,Time.deltaTime * 5f));
    	}
    	if (Mathf.Abs(Input.GetAxis("Vertical"))<0.2f && Mathf.Abs(Input.GetAxis("Horizontal"))>0.2f ){
    		ourDrone.velocity = Vector3.ClampMagnitude(ourDrone.velocity,Mathf.Lerp(ourDrone.velocity.magnitude,5.0f,Time.deltaTime * 5f));
    		
    	}
    	if (Mathf.Abs(Input.GetAxis("Vertical"))<0.2f && Mathf.Abs(Input.GetAxis("Horizontal"))<0.2f ){
    		ourDrone.velocity = Vector3.SmoothDamp(ourDrone.velocity, Vector3.zero, ref velocityToSmoothDampToZero, 0.95f);
       	}
    }


        // For rotation of drone about its axis 
        private float wantedYRotation;
    	[HideInInspector]public float currentYRotation;
    	private float rorateAmountByKeys= 2.5f;
    	private float rotationYVelocity;
    	void Rotation(){

    		if (Input.GetKey(KeyCode.J)){
    			wantedYRotation -= rorateAmountByKeys;
    		}
    		if (Input.GetKey(KeyCode.L)){
    			wantedYRotation += rorateAmountByKeys;
    		}
    		currentYRotation= Mathf.SmoothDamp(currentYRotation, wantedYRotation, ref rotationYVelocity, 0.25f);
    	}
    // to make drone sound like a real drone we added sound to drone
        private AudioSource droneSound;
        void DroneSound(){
            droneSound.pitch = 1+ (ourDrone.velocity.magnitude /100);
        }




    }


   
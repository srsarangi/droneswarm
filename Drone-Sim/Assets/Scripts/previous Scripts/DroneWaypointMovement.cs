

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneWaypointMovement : MonoBehaviour
{
	public Transform path;
	private List<Transform> nodes;
	private int currentNode1 = 0;
	public float maxSteerAngle= 40f;
	// Adjust the speed for the application.
    public float speed = 1.0f;
    public float forwardSpeed = 20f;
    public float avoidingSpeed = 20f;

    // The target (cylinder) position.
    private Transform target;
    // Start is called before the first frame update
    //header is used to arrange code in inspector
    [Header("Sensors")]
    public float sensorLength =14f;
    
    public Vector3 frontSensorPosition = new Vector3(0f,0f,0f);
    public float frontSideSensorPosition = 1f;
    public float frontSensorAngle = 25f;
    public float sideSensorPosition =0.2f;

    public Vector3 bottomSensorPosition = new Vector3(0f, -1.0f, 0f);

    private bool avoiding = false;

    void Start()
    {
        Transform [] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();
        for (int i = 0 ; i< pathTransforms.Length; i++)
        {
        	if (pathTransforms[i] != path.transform)
        	{
        		nodes.Add(pathTransforms[i]);
        	}
        }
    }
private int k =0;
    // Update is called once per frame
    void FixedUpdate()
    {
    	
        //ApplySteer();
        MoveToNextNode(); // to move drone forward force applied
        MoveInCircuit();  // to form a circuit 
        Sensors();
    }
    // dont use now used in cars 
    void ApplySteer()
    {

       Vector3 reelativeVector = transform.InverseTransformPoint(nodes[currentNode1].position) ;
       float newSteer = (reelativeVector.x / reelativeVector.magnitude) * maxSteerAngle ;
       //Debug.Log(newSteer);
    }

    // moving drone from note to node 
    void MoveToNextNode()
 	{
 		 float step =  speed * Time.deltaTime;
    
        // Rotate the drone every frame so it keeps looking at the target
   
        Vector3 relativePos = nodes[currentNode1].position - transform.position;
        

   
        if ((relativePos.magnitude <= 2f) && (currentNode1 != nodes.Count))
        {
            //transform.rotation = Quaternion.LookRotation(-(transform.position - nodes[currentNode1].position));
            currentNode1 ++;
            Debug.Log(currentNode1);
           //Debug.Log(nodes.Count);

        }
        // when last node encountered then move to first node to form closed path
       if ((relativePos.magnitude <= 2f) && (currentNode1 == nodes.Count))
        {
            currentNode1 = 0;
            Debug.Log(currentNode1);
        }
        transform.LookAt(nodes[currentNode1]);

        
        //transform.position = Vector3.MoveTowards(transform.position, nodes[currentNode1].position, step);
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
        // Determine which direction to rotate towards
        //Vector3 targetDirection = target.position - transform.position;

        //// The step size is equal to speed times frame time.
        //float singleStep = speed * Time.deltaTime;

        //// Rotate the forward vector towards the target direction by one step
        //Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);

        //// Draw a ray pointing at our target in
        //Debug.DrawRay(transform.position, newDirection, Color.red);

        //// Calculate a rotation a step closer to the target and applies rotation to this object
        //transform.rotation = Quaternion.LookRotation(newDirection);

    }
    // to form circuit 
    void MoveInCircuit()
 	{
 		if (Vector3.Distance(transform.position, nodes[currentNode1].position)<0.05f)
 		{
 			if (currentNode1 == nodes.Count -1)
 			{
 				currentNode1 = 0;
 			}
 			else
 			{
 				currentNode1 ++;
 			}
 		}


 	}
    // function to create sensors
 	void Sensors()
 	{
 		RaycastHit hit;
 		Vector3 sensorStartPosition = transform.position;
 		 sensorStartPosition += transform.forward * frontSensorPosition.z;
 		sensorStartPosition += transform.up * frontSensorPosition.y;
 		float avoidMultiplier = 0;
 		avoiding = false;
 
 		// front sensor
 		if (Physics.Raycast(sensorStartPosition,transform.forward,out hit , sensorLength))
 		{
            // to check current position 
 			 // Vector3 currentPosition = transform.position;
 			// Debug.Log(transform.position);
 			if (!hit.collider.CompareTag("Terrain"))
 			{
 				Debug.DrawLine(sensorStartPosition, hit.point,Color.red);
 				avoiding = true;
                
            }
        }


        // To check current position
        // Debug.Log(transform.position);
        // right sensor
        sensorStartPosition += transform.right * frontSideSensorPosition;
 		if (Physics.Raycast(sensorStartPosition,transform.forward,out hit , sensorLength))
 		{
 			if (!hit.collider.CompareTag("Terrain"))
 			{
 				Debug.DrawLine(sensorStartPosition,hit.point);
 				avoiding = true;
 				avoidMultiplier -= 1f;
 			} 
 		}
 		// right angle sensor 
 		if (Physics.Raycast(sensorStartPosition,Quaternion.AngleAxis(frontSensorAngle,transform.up)* transform.forward,out hit , sensorLength))
 		{
 			if (!hit.collider.CompareTag("Terrain"))
 			{
 				Debug.DrawLine(sensorStartPosition,hit.point);
 				avoiding = true;
 				avoidMultiplier -= 0.5f;
 			} 
 		}

 		// left sensor
 		sensorStartPosition -= transform.right * frontSideSensorPosition *2;
 		if (Physics.Raycast(sensorStartPosition,transform.forward,out hit , sensorLength))
 		{
 			if (!hit.collider.CompareTag("Terrain"))
 			{
 				Debug.DrawLine(sensorStartPosition,hit.point);
                avoiding = true;
                avoidMultiplier += 1f;
 			} 
 		}
         
 		// left angle sensor 
 		if (Physics.Raycast(sensorStartPosition,Quaternion.AngleAxis(-frontSensorAngle,transform.up)* transform.forward,out hit , sensorLength))
 		{
 			if (!hit.collider.CompareTag("Terrain"))
 			{
                Debug.DrawLine(sensorStartPosition, hit.point);
                avoiding = true;
                avoidMultiplier += 0.5f;
            }

 		}
       
 		if (avoiding)
        {
            //leftSteer = maxSteerAngle * avoidMultiplier;
            forwardSpeed = 0.001f;
            avoidingSpeed =3.0f;
            transform.Translate(Vector3.right * avoidingSpeed * Time.deltaTime);
            // transform.Translate((Vector3.up ) * avoidingSpeed * Time.deltaTime);

        }
        else
        {
            forwardSpeed = 20f;

        }

       
    }

private void OnCollisionEnter(Collision collision)
    {
	//Display collision on console
        Debug.Log("Collision");
    }


}

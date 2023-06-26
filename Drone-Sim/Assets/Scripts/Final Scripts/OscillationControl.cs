using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OscillationControl : MonoBehaviour
{
    GameObject[] allDrones;
    GameObject[] leaderDrones;
    GameObject[] followerDrones;
    GameObject leader;
    public float multiplier = 0.5f;
    public Vector3 migrationPoint = Vector3.zero;
    public float r1 = 1f;
    public float r2 = 1.5f;
    public float r3 = 1f;
    public float r4 = 1f;
    public float r5 = 1f;
    public float r6 = 1f;
    public float avoid_speed = 3.0f;

    //Removing oscillations
    private int counter = 0;
    private float thresholdDistance = 1f;
    public bool is_stuck = false;
    private Vector3 leaderPrevPos;
    List<Vector3> obstaclePts = new List<Vector3>();
    List<Vector3> prevObstaclePts = new List<Vector3>();
    //StreamWriter writetext;

    // Start is called before the first frame update
    void Start()
    {
        ////    Getting Tag of All the drones     //////
        leaderDrones = GameObject.FindGameObjectsWithTag("Leader");
        followerDrones = GameObject.FindGameObjectsWithTag("Drone");
        allDrones = new GameObject[leaderDrones.Length + followerDrones.Length];
        leaderDrones.CopyTo(allDrones, 0);
        followerDrones.CopyTo(allDrones, leaderDrones.Length);
        leader = leaderDrones[0];
        leaderPrevPos = leader.transform.position;

        for(int i=0; i<11; i++)
        {
            obstaclePts.Add(new Vector3(float.MinValue, float.MinValue, float.MinValue));
        }
        prevObstaclePts = obstaclePts;
        //writetext = new StreamWriter("TIMEGRAPH" + this.gameObject.name + ".txt");
        //writetext.WriteLine("r1-r2-r3-r4-r5-r6");
    }

    void FixedUpdate()
    {
        //string line = "";

        //Rule 1 : Seperation
        Vector3 seperation = Vector3.zero;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                Vector3 direction = this.transform.position - g.transform.position;
                direction.Normalize();
                float distance = Vector3.Distance(g.transform.position, this.transform.position);
                direction = direction / distance;
                seperation += direction;
            }
        }



        //Rule 2 : Alignment
        Vector3 alignment = Vector3.zero;
        int neighbours = 0;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                Vector3 vel = g.GetComponent<Rigidbody>().velocity;
                neighbours += 1;
                alignment += vel;
            }
        }
        alignment /= neighbours;


        //Rule 3 : Cohesion
        Vector3 cohesion = Vector3.zero;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                Vector3 vel = g.transform.position;
                cohesion += vel;
            }
        }
        cohesion /= neighbours;

        
        //Rule 4 : Migration
        Vector3 migration = leader.transform.position - transform.position;

       

        //CONFINED ENVIRONMENT
        Vector3 confined = Vector3.zero;
        if (leader.transform.position.z - transform.position.z < 5)
        {
            confined.z -= 1;
        }
        
        if (leader.transform.position.z - 50 >transform.position.z)
        {
            confined.z += 1;
        }
        
        if (leader.transform.position.y + 40 < transform.position.y)
        {
            confined.y -= 1;
        }
        
        if (leader.transform.position.y - 40 > transform.position.y)
        {
            confined.y += 1;
        }
        
        if (leader.transform.position.x + 40 < transform.position.x)
        {
            confined.x -= 1;
        }
            
        if (leader.transform.position.x - 40 > transform.position.x)
        {
            confined.x += 1;
        }

        float[] detection = Sensors(); //Geting data from sensors


        float val = detection[0];
        float obsext_at_left = detection[1];
        float obsext_at_right = detection[2];
        float minHitDistance = detection[3];
        float front = val % 10;
        val = val / 10;
        float right = val % 10;
        val = val / 10;
        float left = val % 10;
        val = val / 10;
        float bottom = val % 10;
        val = val / 10;
        float back = val % 10;
        bool obstAtLeft = false;
        bool obstAtRight = false;
        bool obstAtFront = false;

        

        Vector3 obstacleAvoidance = Vector3.zero;

        //Front sensor
        if (front.Equals((float)1))
        {
            obstAtFront = true;
            if (obsext_at_left.Equals((float)1) && obsext_at_right.Equals((float)1))  //if the obstacle extends on both sides go right
            {
                
                obstacleAvoidance.x += 1;

            }
            else if (obsext_at_left.Equals((float)1))  //if the obstacle extend on left side go right
            {
                
                obstacleAvoidance.x += 1;
            }
            else if (obsext_at_right.Equals((float)1)) //if the obstacle extend on right side go right
            {
                
                obstacleAvoidance.x -= 1;
            }
            else   //if the obstacle doesn't extend on both the sides go right
            {
                
                obstacleAvoidance.x += 1;
            }
        }

        //Back sensor
        if (back.Equals((float)1))
        {
            if (obsext_at_left.Equals((float)1) && obsext_at_right.Equals((float)1))  //if the obstacle extends on both sides go right
            {
                
                obstacleAvoidance.x += 1;

            }
            else if (obsext_at_left.Equals((float)1))   //if the obstacle extend on left side go right
            {
                obstacleAvoidance.x += 1;
            }
            else if (obsext_at_right.Equals((float)1)) //if the obstacle extend on right side go right
            {
                obstacleAvoidance.x -= 1;
            }
            else   //if the obstacle doesn't extend on both the sides go right
            {
                obstacleAvoidance.x += 1;
            }
        }

        // Right Sensor
        if (right.Equals((float)1))
        {
            obstAtRight = true;
            obstacleAvoidance.x -= 2;
        }
        if (left.Equals((float)1))
        {
            obstAtLeft = true;
            obstacleAvoidance.x += 2;
        }
        if (obsext_at_left.Equals((float)1) || obsext_at_right.Equals((float)1))
        {
            obstAtFront = true;
        }


        //Final velocity
        Vector3 droneVel = Vector3.zero;

        //Obstacle avoidance and oscilation check
        if (obstacleAvoidance == Vector3.zero && !obsext_at_left.Equals((float)1) && !obsext_at_right.Equals((float)1))
        {
            counter = 0;
            is_stuck = false;

            if (minHitDistance != 0)
            {
                droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration + r5 * (obstacleAvoidance) + r6 * confined;
            }
            else
            {
                droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration + r5 * obstacleAvoidance + r6 * confined;
            }

        }
        else //Obstacle is there then avoid
        {
            counter += 1;
            if (counter == 10)
            {
                is_stuck = false;
                counter = 0;
                if (obstAtFront)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float dist_ = Vector3.Distance(prevObstaclePts[i], obstaclePts[i]);
                        if (dist_ < thresholdDistance)
                        {
                            is_stuck = true;
                            break;
                        }
                    }
                }
                else if (obstAtRight)
                {
                    for (int i = 8; i < 11; i++)
                    {
                        float dist_ = Vector3.Distance(prevObstaclePts[i], obstaclePts[i]);
                        if (dist_ < thresholdDistance)
                        {
                            is_stuck = true;
                            break;
                        }
                    }
                }
                else if (obstAtLeft)
                {
                    for (int i = 5; i < 8; i++)
                    {
                        float dist_ = Vector3.Distance(prevObstaclePts[i], obstaclePts[i]);
                        if (dist_ < thresholdDistance)
                        {
                            is_stuck = true;
                            break;
                        }
                    }
                }
            }

            if (minHitDistance != 0)
            {

                droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration + r5 * ((obstacleAvoidance * sensorLength) / minHitDistance) + r6 * confined;

            }
            else
            {

                droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration + r5 * obstacleAvoidance + r6 * confined;
            }
        }
        
        if(leader.transform.position != leaderPrevPos)
        {
            is_stuck = false;
            counter = 0;
            leaderPrevPos = leader.transform.position;
        }

        prevObstaclePts = obstaclePts;
        if (!is_stuck)
        {
            this.GetComponent<Rigidbody>().velocity = multiplier * droneVel;
        }
        else
        {
            this.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
        

    }


    //Sensor Data collection

    [Header("Sensors")]
    public float sensorLength = 3f;
    public float frontSensorPosition = 1f;
    public float frontSideSensorPosition = 1f;
    public float frontSensorAngle = 30;
    public float sideSensorPosition = 1f;

    public Vector3 bottomSensorPosition = new Vector3(0f, -1.0f, 0f);

    //To traverse the obstaclePts list
    int index = 0;

    float[] Sensors()
    {
        float[] result = new float[7];
        RaycastHit hit;
        Vector3 sensorStartPosition;
        int res = 0;
        int obsext_at_left = 0;
        int obsext_at_right = 0;
        int sensor_val;
        float hitDistances = int.MaxValue;
        Vector3 hitPoint = Vector3.zero;

        index = 0;

        ///FRONT SENSORS
        ///These have index from 0 to 4 in the obstaclePts list
        sensorStartPosition = transform.position;
        //Front mid sensor
        
        sensorStartPosition += Vector3.forward * frontSensorPosition;
        if (Physics.Raycast(sensorStartPosition, Vector3.forward, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
                
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //Front right sensor
        sensorStartPosition += Vector3.right * (sideSensorPosition + 0.3f);
        if (Physics.Raycast(sensorStartPosition, Vector3.forward, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 1;
               Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //Front right angle sensor
        if (Physics.Raycast(sensorStartPosition, Quaternion.AngleAxis(frontSensorAngle, Vector3.up) * Vector3.forward, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                obsext_at_right = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //Front left sensor
        
        sensorStartPosition -= Vector3.right * (2 * (sideSensorPosition + 0.3f));
        if (Physics.Raycast(sensorStartPosition, Vector3.forward, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //Front left angle sensor
        if (Physics.Raycast(sensorStartPosition, Quaternion.AngleAxis(-frontSensorAngle, Vector3.up) * Vector3.forward, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                obsext_at_left = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        sensor_val = res;
        res = 0;

        index += 1;
        ///LEFT SENSORS
        ///These have index from 5 to 7 in the obstaclePts list
        sensorStartPosition = transform.position;
        //Left mid sensor
        
        sensorStartPosition -= Vector3.right * frontSensorPosition;
        if (Physics.Raycast(sensorStartPosition, Vector3.left, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 100;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        // left front sensor
        sensorStartPosition += Vector3.forward * (sideSensorPosition + 0.3f);
        if (Physics.Raycast(sensorStartPosition, Vector3.left, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 100;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //left rear sensor
        
        sensorStartPosition -= Vector3.forward * (2 * (sideSensorPosition + 0.3f));
        if (Physics.Raycast(sensorStartPosition, Vector3.left, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 100;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        sensor_val += res;
        res = 0;
        index += 1;
        ///RIGHT SENSORS
        ///These have index from 8 to 10 in the obstaclePts list
        sensorStartPosition = transform.position;
        //right mid sensor
        
        sensorStartPosition += Vector3.right * frontSensorPosition;
        if (Physics.Raycast(sensorStartPosition, Vector3.right, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        // right Front sensor
        sensorStartPosition += Vector3.forward * (sideSensorPosition + 0.3f);
        if (Physics.Raycast(sensorStartPosition, Vector3.right, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        index += 1;
        //right rear sensor
        
        sensorStartPosition -= Vector3.forward * (2 * (sideSensorPosition + 0.3f));
        if (Physics.Raycast(sensorStartPosition, Vector3.right, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                obstaclePts[index] = hit.point;
                if (hitDistances > hit.distance)
                {
                    hitDistances = hit.distance;
                    hitPoint = hit.point;
                }
            }
            else
            {
                obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }
        else
        {
            obstaclePts[index] = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        sensor_val += res;
        res = 0;

        ///BACK Sensor
        //int obsext_at_back = 0;
        sensorStartPosition = transform.position;
        //back mid sensor
        
        sensorStartPosition += Vector3.back * frontSensorPosition;
        if (Physics.Raycast(sensorStartPosition, Vector3.back, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10000;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                hitDistances = hitDistances > hit.distance ? hit.distance : hitDistances;
            }
        }

        //back right sensor
        //sensorStartPosition.x += (sideSensorPosition + 0.3f);
        sensorStartPosition += Vector3.right * (sideSensorPosition + 0.3f);
        if (Physics.Raycast(sensorStartPosition, Vector3.back, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10000;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                hitDistances = hitDistances > hit.distance ? hit.distance : hitDistances;
            }
        }

        //back right angle sensor
        if (Physics.Raycast(sensorStartPosition, Quaternion.AngleAxis(-frontSensorAngle, Vector3.up) * Vector3.back, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                obsext_at_right = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                hitDistances = hitDistances > hit.distance ? hit.distance : hitDistances;
            }
        }

        //back left sensor
        sensorStartPosition -= Vector3.right * (2 * (sideSensorPosition + 0.3f));
        if (Physics.Raycast(sensorStartPosition, Vector3.back, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                res = 10000;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                hitDistances = hitDistances > hit.distance ? hit.distance : hitDistances;
            }
        }

        //back left angle sensor
        if (Physics.Raycast(sensorStartPosition, Quaternion.AngleAxis(frontSensorAngle, Vector3.up) * Vector3.back, out hit, sensorLength))
        {
            if (!(hit.collider.CompareTag("Drone") || hit.collider.CompareTag("Leader")))
            {
                obsext_at_left = 1;
                Debug.DrawLine(sensorStartPosition, hit.point, Color.red);
                hitDistances = hitDistances > hit.distance ? hit.distance : hitDistances;
            }
        }

        sensor_val += res;

        result[0] = sensor_val;
        result[1] = obsext_at_left;
        result[2] = obsext_at_right;
        result[3] = hitDistances;
        result[4] = hitPoint.x;
        result[5] = hitPoint.y;
        result[6] = hitPoint.z;

        return result;

    }
}

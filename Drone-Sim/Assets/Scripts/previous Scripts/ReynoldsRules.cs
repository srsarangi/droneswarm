using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class ReynoldsRules : MonoBehaviour
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

    // Start is called before the first frame update
    void Start()
    {
        leaderDrones = GameObject.FindGameObjectsWithTag("Leader");
        followerDrones = GameObject.FindGameObjectsWithTag("Drone");
        allDrones = new GameObject[leaderDrones.Length + followerDrones.Length];
        //Debug.Log(followerDrones.Length);
        leaderDrones.CopyTo(allDrones, 0);
        followerDrones.CopyTo(allDrones, leaderDrones.Length);

        leader = leaderDrones[0];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        //Rule 1 : Seperation
        Vector3 seperation = Vector3.zero;
        foreach(GameObject g in allDrones)
        {   
            if(g != gameObject){
                Vector3 direction = this.transform.position - g.transform.position;
                direction.Normalize();
                float distance = Vector3.Distance(g.transform.position, this.transform.position);
                direction = direction/distance;
                seperation += direction;
            }
        }
        //Debug.Log(seperation.x + " "+ seperation.y + " "+seperation.z);

        //Rule 2 : Alignment
        Vector3 alignment = Vector3.zero;
        int neighbours = 0;
        foreach(GameObject g in allDrones)
        {
            if(g != gameObject){
                Vector3 vel = g.GetComponent<Rigidbody>().velocity;
                neighbours += 1;
                alignment += vel;
            }
        }
        alignment /= neighbours;

        //Rule 3 : Cohesion
        Vector3 cohesion = Vector3.zero;
        foreach(GameObject g in allDrones)
        {
            if(g != gameObject){
                Vector3 vel = g.transform.position;
                cohesion += vel;
            }
        }
        cohesion /= neighbours;

        //Rule 4 : Migration
        Vector3 migration = Vector3.zero;
        migration = leader.transform.position - this.transform.position;

        /*for (int i = 0; i <= 1000; i++)
        {
            Debug.Log("Drone : " + gameObject.GetInstanceID() + "  " + "Num : " + i);
        }*/

        //Debug.Log("Drone id is : " + gameObject.GetInstanceID());
        Vector3 droneVel = Vector3.zero;
        droneVel = r1*seperation + r2*alignment + r3*cohesion + r4*migration;
        this.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

    }
}

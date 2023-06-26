using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reynoldmtp2 : MonoBehaviour
{
    GameObject[] allDrones;
    GameObject[] leaderDrones;
    GameObject[] followerDrones;
    GameObject leader;
    public float multiplier = 0.5f;
    private Vector3 migrationPoint = Vector3.zero;
    public float r1 = 1f;
    public float r2 = 1.5f;
    public float r3 = 1f;
    public float r4 = 1f;
    public float maxneighbourdistance = 5f;

    //New Swarming se aaya hai
    private float dx, dy, dz;
    public Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        leaderDrones = GameObject.FindGameObjectsWithTag("Leader");
        followerDrones = GameObject.FindGameObjectsWithTag("Drone");
        allDrones = new GameObject[leaderDrones.Length + followerDrones.Length];
        leaderDrones.CopyTo(allDrones, 0);
        followerDrones.CopyTo(allDrones, leaderDrones.Length);

        leader = leaderDrones[0];

        //New Swarming se aaya hai
        rb = this.GetComponent<Rigidbody>();
        dx = leader.transform.position.x - this.transform.position.x;
        dy = leader.transform.position.y - this.transform.position.y;
        dz = leader.transform.position.z - this.transform.position.z;
    }

    void FixedUpdate()
    {
        //rb.useGravity = false;

        //Rule 1 : Seperation
        Vector3 seperation = Vector3.zero;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                float neighbourdistance = Vector3.Distance(g.transform.position, this.transform.position);
                if (neighbourdistance < maxneighbourdistance)
                {
                    Vector3 direction = this.transform.position - g.transform.position;
                    direction.Normalize();
                    direction /= Vector3.Distance(g.transform.position, this.transform.position);
                    seperation += direction;
                }
            }
        }

        //Rule 2 : Alignment
        Vector3 alignment = Vector3.zero;
        int neighbours = 0;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                float neighbourdistance = Vector3.Distance(g.transform.position, this.transform.position);
                if (neighbourdistance < maxneighbourdistance)
                {
                    Vector3 vel = g.GetComponent<Rigidbody>().velocity;
                    neighbours += 1;
                    alignment += vel;
                }
            }
        }
        if (neighbours != 0)
        {
            alignment /= neighbours;
        }

        //Rule 3 : Cohesion
        Vector3 cohesion = Vector3.zero;
        foreach (GameObject g in allDrones)
        {
            if (g != gameObject)
            {
                float neighbourdistance = Vector3.Distance(g.transform.position, this.transform.position);
                if (neighbourdistance < maxneighbourdistance)
                {
                    Vector3 vel = g.transform.position;
                    cohesion += vel;
                }
            }
        }
        if (neighbours != 0)
        {
            cohesion /= neighbours;
        }

        //Rule 4 : Migration
        Vector3 migration = Vector3.zero;
        migration = leader.transform.position - this.transform.position;

        Vector3 droneVel = Vector3.zero;
        droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration;
        Debug.Log(droneVel);
        this.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

    }
}

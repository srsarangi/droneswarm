using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : MonoBehaviour
{
    GameObject[] allDrones;
    //GameObject[] obstacles;
    public float SpaceBetweenNeighbour = 8.0f;
    public float SpaceBetweenObstacle = 1.5f;
    public float multiplier = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
      allDrones = GameObject.FindGameObjectsWithTag("Drone");
      //obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
    }

    // Update is called once per frame
    void Update()
    {
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

        Vector3 leaderVel = Vector3.zero;
        leaderVel = seperation;
        this.GetComponent<Rigidbody>().velocity = multiplier * leaderVel;

        // foreach(GameObject g in allDrones)
        // {
        //     if(g != gameObject){
        //         float distance = Vector3.Distance(g.transform.position, this.transform.position);
        //         if(distance <= SpaceBetweenNeighbour)
        //         {
        //           Vector3 direction = transform.position - g.transform.position;
        //           transform.Translate(direction * multiplier);
        //         }
        //     }
        // }
    }
}

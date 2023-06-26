using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderWaypoints : MonoBehaviour  //// Script to to move leader automatically on predefined path
{
    private GameObject[] allgame;
    private List<GameObject> waypoints = new List<GameObject>();
    private int index = 0;
    private float ref_dist = -1;
    public bool check = true;
    public bool switchenv = true;
    public bool go = false;
    private bool nogps = false;
   
    // Start is called before the first frame update
    void Start()
    {
        GameObject g;
        //// Getting all waypoints ////
        for (int i = 1; i <= 62; i++)
        {
            g = GameObject.Find("point (" + i + ")");
            waypoints.Add(g);
        }
        g = GameObject.Find("Sphere1");
        waypoints.Add(g);
        for(int i = 1; i <= 417; i++)
        {
            g = GameObject.Find("Sphere1 ("+ i + ")");
            waypoints.Add(g);
        }

    }


    // Update is called once per frame
    void Update()
    {
        if (go && switchenv && !nogps)
        {
            Vector3 pos = waypoints[index].transform.position;
            Vector3 velocity = Vector3.zero;
            float distance = Vector3.Distance(pos, transform.position);
            ref_dist = ref_dist == -1 ? distance : ref_dist;
            
            transform.position = Vector3.MoveTowards(transform.position, pos, 5f* Time.deltaTime);
            if (distance < 0.1)
            {
                if (index < waypoints.Count - 1)
                {
                    ref_dist = -1;
                    index++;
                   
                    if(index == 201)
                    {

                        nogps = true;
                        Debug.Log(index);
                        this.GetComponent<ChangeEnv>().change = true;
                    }
                    
                }
            }
        }
        else if (go && switchenv && nogps)
        {
            Vector3 pos = waypoints[index].transform.position;
            Vector3 velocity = Vector3.zero;
            float distance = Vector3.Distance(pos, transform.position);
            ref_dist = ref_dist == -1 ? distance : ref_dist;

            transform.position = Vector3.MoveTowards(transform.position, pos, 5f * Time.deltaTime);
            if (distance < 0.1)
            {
                if (index < waypoints.Count - 1)
                {
                    ref_dist = -1;
                    index++;

                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeEnv : MonoBehaviour  ///// Changing The environment from GPS to non GPS ////// 
{
    public bool change = false;
    private bool check = false;
    private int count = 0;
    private GameObject[] players; 
    // Start is called before the first frame update
    void Start()
    {
        players = GameObject.FindGameObjectsWithTag("Drone");
    }

    // Update is called once per frame
    void Update()
    {
        if (check)
        {
            if(count == 0)
            {
                foreach (var drone in players)
                {
                    drone.GetComponent<Rey2CamObs>().enabled = true;
                }
                this.GetComponent<LeaderWaypoints>().switchenv = true;
                check = false;
            }
            
            else if(count == 20)
            {
                foreach (var drone in players)
                {
                    drone.GetComponent<Rey2CamObs>().enabled = true;
                }
                count-- ;
            }
            else
            {
                count--;
            }
            
        }
        if (change)
        {
            check = true;
            count = 80;
            this.GetComponent<LeaderWaypoints>().switchenv = false;
            float y = 0;
            float z = -9999;
            float x = 0;
            int noofdrone = 0;
            foreach( var drone in players)
            {
                drone.GetComponent<OscillationControl>().enabled = false;
                drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
                drone.GetComponent<NewSwarmingTrial>().enabled = false;
                y += drone.transform.position.y;
                x += drone.transform.position.x;
                if(drone.transform.position.z > z)
                {
                    z = drone.transform.position.z;
                }
                
                noofdrone++;

            }
            y /= noofdrone;
            x /= noofdrone;
            Debug.Log("z is " + z);
            Debug.Log("x is " + x);
            Vector3 pos = new Vector3(x, y, z+10);

            change = false;

        }
    }
}

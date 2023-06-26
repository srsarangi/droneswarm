using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateFollower : MonoBehaviour
{
    public int followerCount =0 ;
    public GameObject leaderDrone;
    public GameObject followerDrone;
    private int x = 0;
    private int y = 0;
    private int z = 0;
    public int dist = 10;

    // Start is called before the first frame update
    void Start()
    {
        for(int i =1; i <= followerCount; i+=5)
        {
            Vector3 v = new Vector3(-2*dist, 0, z-dist);
            Instantiate(followerDrone, leaderDrone.transform.position + v, new Quaternion(0, 0, 0, 0));
            /*Debug.Log(leaderDrone.transform.position);
            Debug.Log(leaderDrone.transform.position + v);*/
            z -= 5;
        }
        z = 0;
        for(int i =1; i <= followerCount; i+=5)
        {
            Vector3 v = new Vector3(-1*dist, 0, z-dist);
            Instantiate(followerDrone, leaderDrone.transform.position + v, new Quaternion(0, 0, 0, 0));
            z -= 5;
        }
        z = 0;
        for (int i = 2; i <= followerCount; i += 5)
        {
            Vector3 v = new Vector3(dist, 0, z - dist);
            Instantiate(followerDrone, leaderDrone.transform.position + v, new Quaternion(0, 0, 0, 0));
            z -= 5;
        }
        z = 0;
        for (int i = 2; i <= followerCount; i += 5)
        {
            Vector3 v = new Vector3(2*dist, 0, z - dist);
            Instantiate(followerDrone, leaderDrone.transform.position + v, new Quaternion(0, 0, 0, 0));
            z -= 5;
        }
        z = 0;
        for (int i = 3; i <= followerCount; i += 5)
        {
            Vector3 v = new Vector3(0, 0, z - dist);
            Instantiate(followerDrone, leaderDrone.transform.position + v, new Quaternion(0, 0, 0, 0));
            z -= 5;
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}

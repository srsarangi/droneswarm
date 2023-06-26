using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderAutoMove : MonoBehaviour
{

    public List<GameObject> waypoints;
    private int index = 0;
    private float ref_dist = -1;
    public bool check =true;
    public bool go = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (go)
        {
            Vector3 pos = waypoints[index].transform.position;
            Vector3 velocity = Vector3.zero;
            float distance = Vector3.Distance(pos, transform.position);
            ref_dist = ref_dist == -1 ? distance : ref_dist;
            Debug.Log(index + " index and dista  " + distance + " smmothtime " + distance / ref_dist);
            transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, check ? 0.1f : (distance / ref_dist));
            if (distance < 0.5)
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

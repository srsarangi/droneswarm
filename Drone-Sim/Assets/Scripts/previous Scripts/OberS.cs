using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OberS : MonoBehaviour
{
    GameObject[] followerDrones;
    GameObject leader;
    public float r;
    public float ep;
    public float h;
    public float a;
    public float b;
    private float c;
    public float c1_alpha;
    public float c1_gamma;
    private float c2_gamma;
    private float c2_alpha;
    //public float multiplier;
    private Vector3 prevvel = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        leader = GameObject.FindGameObjectsWithTag("Leader")[0];
        followerDrones = GameObject.FindGameObjectsWithTag("Drone");
        //Debug.Log(leader.transform.position);
        c = Mathf.Abs(a - b) / Mathf.Sqrt(b * b - 4 * a * c);
        c2_alpha = 2 * Mathf.Sqrt(c1_alpha);
        c2_gamma = 0.2f * Mathf.Sqrt(c1_gamma);
        prevvel.x = 1f;
        prevvel.z = 1.2f;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 ui =prevvel;
        foreach (GameObject g in followerDrones)
        {
            if (g != gameObject)
            {
                float d = Vector3.Distance(g.transform.position, this.transform.position);
                if (d <= r)
                {
                    ui += ((c1_alpha *get_gradient(g.transform.position, d)) + (c2_alpha * get_consensus(d, g.GetComponent<Rigidbody>().velocity) /*+ get_mig()*/));
                }
            }
            
        }
        //apply ui to drone
        this.GetComponent<Rigidbody>().velocity = ui;
        prevvel = ui;
        //transform.position = ui;
        Debug.Log(ui.x + " x " + ui.y + " y " + ui.z);
    }

    Vector3 get_gradient(Vector3 neigh,float d)
    {
        Vector3 res;
        res = fi_alpha(get_norm(d),d) * get_nij(neigh);
        return res;
    }

    float get_norm(float z)
    {
        float val;
        val = (Mathf.Sqrt(1+(ep * z *z))-1) / ep;
        return val;
    }

    float sigma1(float z)
    {
        float val;
        val = z / (1 + (z * z));
        return val;
    }

    float rouh(float z)
    {
        float val;
        if (z >= 0 && z < h)
        {
            return 1;
        }
        if(z>=h && z <= 1)
        {
            val = (1 + (Mathf.Cos(Mathf.PI * (z - h) / (1 - h))))/2;
            return val;
        }
        return 0;
    }
    float fi(float z)
    {
        float val;
        val = ((a + b) * sigma1(z + c) + (a - b)) / 2;
        return val;

    }
    float fi_alpha( float z,float d)
    {
        float val;
        val = rouh(z / get_norm(r)) * fi(z - get_norm(d));
        return val;
    }

    Vector3 get_nij(Vector3 neigh)
    {
        Vector3 res;
        float d = (neigh - this.transform.position).magnitude;
        res = neigh - ((this.transform.position) / Mathf.Sqrt(1 + ep * d *d ));
        return res;
    }


    Vector3 get_consensus(float d,Vector3 neigh)
    {
        Vector3 res;
        res = rouh(get_norm(d) / get_norm(r)) * (neigh - this.GetComponent<Rigidbody>().velocity);
        return res;
    }

    Vector3 get_mig()
    {
        Vector3 res;
        res = - c1_gamma * (this.transform.position - leader.transform.position) - c2_gamma * (this.GetComponent<Rigidbody>().velocity - leader.GetComponent<Rigidbody>().velocity);
        return res;
    }
}

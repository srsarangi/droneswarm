using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class RulesOfReynolds : MonoBehaviour
{
    GameObject[] allDrones;
    GameObject[] leaderDrones;
    GameObject[] followerDrones;
    GameObject leader;
    public float c_g = 1.0f;
    public float R_n = 10.0f;
    public float c_ca = 10.0f;
    public float R_ca = 4.0f;
    public float w_ca = 2.0f;
    public float c_fl = 2.0f;
    public float c_pa = 300.0f;
    public float w_pa = 2.0f;
    public float R_d = 30.0f;
    public float R_pa = 10.0f;
    public float multiplier = 0.5f;
    private Vector3 f_ig, f_ica, f_ifl, f_ipa;
    private Vector3 u, z_mc;

    // Start is called before the first frame update
    void Start()
    {
        leaderDrones = GameObject.FindGameObjectsWithTag("Leader");
        followerDrones = GameObject.FindGameObjectsWithTag("Drone");
        //predatorDrones = GameObject.FindGameObjectsWithTag("Predator");
        allDrones = new GameObject[leaderDrones.Length + followerDrones.Length];
        leaderDrones.CopyTo(allDrones, 0);
        followerDrones.CopyTo(allDrones, leaderDrones.Length);
        leader = leaderDrones[0];
    }

    // Update is called once per frame
    void Update()
    {
        //Grouping force
        f_ig = Vector3.zero;
        foreach(GameObject g in allDrones)
        {
            if(g != gameObject){
                float distance = Vector3.Distance(g.transform.position, this.transform.position);
                if(distance <= R_n){
                    Vector3 direction = g.transform.position - this.transform.position;
                    f_ig += (float) c_g * direction;
                }
            }
            Debug.Log(f_ig.x + " " + f_ig.y + " " + f_ig.z + " " );
        }

        //Collision avoidance force
        f_ica = Vector3.zero;
        foreach(GameObject g in allDrones)
        {
            if(g != gameObject){
                float distance = Vector3.Distance(g.transform.position, this.transform.position);
                if(distance <= R_n){
                    Vector3 direction = this.transform.position - g.transform.position;
                    direction.Normalize();
                    f_ica += (float) c_ca * (float) (1/(1+Math.Exp(w_ca*distance - R_ca))) * direction;
                }
            }
        }

        //Flocking force
        f_ifl = Vector3.zero;
        z_mc = Vector3.zero;
        foreach(GameObject g in allDrones)
        {
            z_mc += g.transform.position;
        }
        //z_mc = z_mc/(allDrones.Length);
        z_mc = leader.transform.position;
        f_ifl = c_fl * (z_mc - this.transform.position);

        //Predator avoidance force
        // f_ipa = 0;
        // foreach(GameObject p in predatorDrones)
        // {
        //     if(g != gameObject){
        //         float distance = Vector3.Distance(p.transform.position, this.transform.position);
        //         if(distance <= R_d){
        //             Vector3 direction = this.transform.position - p.transform.position;
        //             direction.Normalize();
        //             f_ipa += c_pa * ((1/(1+Math.Exp(w_pa*distance - R_pa)))+0.3) * direction;
        //         }
        //     }
        // }
        //
        u = f_ig + f_ica + f_ifl;

        Vector3 droneVel = Vector3.zero;
        droneVel = u;
        this.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

    }
}

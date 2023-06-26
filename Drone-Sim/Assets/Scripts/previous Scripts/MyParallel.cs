using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;

public class MyParallel : MonoBehaviour
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
        leaderDrones.CopyTo(allDrones, 0);
        followerDrones.CopyTo(allDrones, leaderDrones.Length);
        leader = leaderDrones[0];
        Debug.Log(allDrones.Length);

    }

    // Update is called once per frame
    void Update()
    {
        var memberPositions = new NativeArray<Vector3>(allDrones.Length, Allocator.TempJob);
        var memberVelocities = new NativeArray<Vector3>(allDrones.Length, Allocator.TempJob);
        var resultantVelocities = new NativeArray<Vector3>(allDrones.Length, Allocator.TempJob);

        for (int i = 0; i < allDrones.Length; i++)
        {
            memberPositions[i] = allDrones[i].transform.position;
            memberVelocities[i] = allDrones[i].GetComponent<Rigidbody>().velocity;
            resultantVelocities[i] = Vector3.zero;
        }

        var job = new MoveJob()
        {
            memberPositions = memberPositions,
            memberVelocities = memberVelocities,
            resultantVelocities = resultantVelocities,
            r1 = r1,
            r2 = r2,
            r3 = r3,
            r4 = r4
        };

        JobHandle handle = job.Schedule(resultantVelocities.Length, 256);
        handle.Complete();

        for (int i = 1; i < allDrones.Length; i++)
        {
            //Debug.Log(resultantVelocities[i].ToString("F3"));
            allDrones[i].GetComponent<Rigidbody>().velocity = multiplier * resultantVelocities[i];
        }

        memberPositions.Dispose();
        memberVelocities.Dispose();
        resultantVelocities.Dispose();
    }

    public struct MoveJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> memberPositions;
        [ReadOnly]
        public NativeArray<Vector3> memberVelocities;
        public NativeArray<Vector3> resultantVelocities;
        public float r1;
        public float r2;
        public float r3;
        public float r4;

        public void Execute(int index)
        {
            Vector3 seperation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            int neighbours = 0;
            Vector3 cohesion = Vector3.zero;
            Vector3 migration = Vector3.zero;

            Debug.Log("Here i am : " + index);

            

            for (int i = 0; i < memberPositions.Length; i++)
            {
                //Rule 1 : Seperation
                if (i != index && i != 0)
                {
                    Vector3 direction = memberPositions[index] - memberPositions[i];
                    direction.Normalize();
                    float distance = Vector3.Distance(memberPositions[i], memberPositions[index]);
                    direction = direction / distance;
                    seperation += direction;
                }

                //Rule 2 : Alignment
                if (i != index && i != 0)
                {
                    Vector3 vel = memberVelocities[i];
                    neighbours += 1;
                    alignment += vel;
                }

                //Rule 3 : Cohesion
                if (i != index && i != 0)
                {
                    Vector3 vel = memberPositions[i];
                    cohesion += vel;
                }
            }
            alignment = alignment / neighbours;
            cohesion = cohesion / neighbours;

            //Rule 4 : Migration
            migration = memberPositions[0] - memberPositions[index];

            Vector3 droneVel = Vector3.zero;
            droneVel = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * migration;
            resultantVelocities[index] = droneVel;
            Debug.Log("Here you are : " + index);
        }
    }
}

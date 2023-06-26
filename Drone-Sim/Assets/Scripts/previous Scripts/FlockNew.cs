using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockNew : MonoBehaviour
{
    [Header("Spawn Setup")]
    [SerializeField] private FlockUnitNew flockUnitPrefab;
    [SerializeField] private int flockSize;
    [SerializeField] private Vector3 spawnBounds;

    GameObject leader;
    private float dx, dy, dz;

    [Header("Speed Setup")]
    [Range(0, 10)]
    [SerializeField] private float _minSpeed;
    public float minSpeed { get { return _minSpeed; } }
    [Range(0, 10)]
    [SerializeField] private float _maxSpeed;
    public float maxSpeed { get { return _maxSpeed; } }


    [Header("Detection Distances")]

    [Range(0, 10)]
    [SerializeField] private float _cohesionDistance;
    public float cohesionDistance { get { return _cohesionDistance; } }

    [Range(0, 10)]
    [SerializeField] private float _avoidanceDistance;
    public float avoidanceDistance { get { return _avoidanceDistance; } }

    [Range(0, 10)]
    [SerializeField] private float _aligementDistance;
    public float aligementDistance { get { return _aligementDistance; } }

    [Range(0, 30)]
    [SerializeField] private float _obstacleDistance = 3;
    public float obstacleDistance { get { return _obstacleDistance; } }

    [Range(0, 100)]
    [SerializeField] private float _boundsDistance;
    public float boundsDistance { get { return _boundsDistance; } }


    [Header("Behaviour Weights")]

    [Range(0, 10)]
    [SerializeField] private float _cohesionWeight;
    public float cohesionWeight { get { return _cohesionWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _avoidanceWeight;
    public float avoidanceWeight { get { return _avoidanceWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _aligementWeight;
    public float aligementWeight { get { return _aligementWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _boundsWeight;
    public float boundsWeight { get { return _boundsWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _migrationWeight;
    public float migrationWeight { get { return _migrationWeight; } }

    [Range(0, 100)]
    [SerializeField] private float _obstacleWeight = 3;
    public float obstacleWeight { get { return _obstacleWeight; } }

    public FlockUnitNew[] allUnits { get; set; }

    private void Start()
    {
        GenerateUnits();
        leader = GameObject.FindGameObjectsWithTag("Leader")[0];
        dx = leader.transform.position.x - this.transform.position.x;
        dy = leader.transform.position.y - this.transform.position.y;
        dz = leader.transform.position.z - this.transform.position.z;

    }

    private void Update()
    {
        Vector3 velocity = Vector3.zero;
        transform.position = Vector3.SmoothDamp(transform.position, leader.transform.TransformPoint(new Vector3(-dx, 0, -dz)), ref velocity, 1.0f);
        //Debug.Log("Leader " + leader.transform.position);
        //Debug.Log("flock " + transform.position);
        for (int i = 0; i < allUnits.Length; i++)
        {
            allUnits[i].MoveUnit();
        }
    }

    private void GenerateUnits()
    {
        allUnits = new FlockUnitNew[flockSize];
        for (int i = 0; i < flockSize; i++)
        {
            var randomVector = UnityEngine.Random.insideUnitSphere;
            randomVector = new Vector3(randomVector.x * spawnBounds.x, randomVector.y * spawnBounds.y, randomVector.z * spawnBounds.z);
            var spawnPosition = transform.position + randomVector;
            var rotation = Quaternion.Euler(0, 0, 0);
            allUnits[i] = Instantiate(flockUnitPrefab, spawnPosition, rotation);
            allUnits[i].AssignFlock(this);
            allUnits[i].InitializeSpeed(UnityEngine.Random.Range(minSpeed, maxSpeed));
        }
    }
}

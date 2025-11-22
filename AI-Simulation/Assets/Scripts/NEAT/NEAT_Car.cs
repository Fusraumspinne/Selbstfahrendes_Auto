using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NEAT_Car : MonoBehaviour
{
    [SerializeField] private Rigidbody rig;
    [SerializeField] private WheelColliders colliders;
    [SerializeField] private WheelMeshes meshes;
    [SerializeField] private float gasInput;
    [SerializeField] private float steeringInput;
    [SerializeField] private float brakeInput;

    [SerializeField] private float motorPower;
    [SerializeField] private float brakePower;
    [SerializeField] private float slipAngle;
    [SerializeField] private float speed;

    [SerializeField] private float smoothingFactor;

    [SerializeField] private bool isBraking;

    private float previousSteeringInput = 0f;

    private bool initialized = false;
    private Transform target;

    private NEAT_NeuralNetwork net;

    public void Update()
    {
        speed = rig.velocity.magnitude;

        //CheckInput();
        ApplySpeed();
        ApplySteering();
        ApplyWheels();

        if (initialized)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            float angleToTarget = Vector3.SignedAngle(transform.forward, (target.position - transform.position).normalized, Vector3.up);
            angleToTarget /= 180.0f;

            float[] vision = PerformRaycastVision();

            float[] inputs = new float[11];
            inputs[0] = angleToTarget;
            //inputs[0] = 0;
            //inputs[1] = gasInput;
            for (int i = 0; i < vision.Length; i++)
            {
                if (i + 1 >= 10)
                {
                    break;
                }

                inputs[i + 1] = vision[i];
            }

            float[] output = net.FeedForward(inputs);

            float newSteeringInput = Mathf.Clamp(output[0], -1f, 1f);
            steeringInput = newSteeringInput;
            previousSteeringInput = steeringInput;

            //gasInput = Mathf.Clamp(output[1], -1f, 1f);

            float distanceFactor = Mathf.Exp(-distance / 53f);

            float orientationFitness = (1f - Mathf.Abs(inputs[0])) / 2f;

            float minVision = vision.Min();     
            float dangerPenalty = 0f;
            if (minVision < 0.1f)
            {
                dangerPenalty = (0.1f - minVision) * 10f;  
            }

            float totalFitness = 0f;
            totalFitness += distanceFactor * 0.5f;
            totalFitness += orientationFitness * 1f;
            //totalFitness -= dangerPenalty * 1;   

            net.AddFitness(totalFitness * Time.fixedDeltaTime);
        }
    }

    private float[] PerformRaycastVision()
    {
        float[] vision = new float[9];
        float[] angles = { -75, -50, -25, -10, 0, 10, 25, 50, 75 };

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angles[i], 0) * transform.forward;
            vision[i] = CastRay(direction);
        }

        return vision;
    }

    private float CastRay(Vector3 direction)
    {
        RaycastHit hit;
        float maxDistance = 30f;
        int layerMask = ~LayerMask.GetMask("Car");

        if (Physics.Raycast(transform.position, direction, out hit, maxDistance, layerMask))
        {
            Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            return hit.distance / maxDistance;
        }

        Debug.DrawRay(transform.position, direction * maxDistance, Color.green);
        return 1f;
    }

    public void Init(NEAT_NeuralNetwork net, Transform target)
    {
        this.target = target;
        this.net = net;
        initialized = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    void ApplySpeed()
    {
        colliders.RRWheel.motorTorque = motorPower * gasInput;
        colliders.RLWheel.motorTorque = motorPower * gasInput;
    }

    void ApplySteering()
    {
        steeringInput = smoothingFactor * previousSteeringInput + (1 - smoothingFactor) * steeringInput;
        previousSteeringInput = steeringInput;

        float steeringAngle = steeringInput * 30;
        colliders.FRWheel.steerAngle = steeringAngle;
        colliders.FLWheel.steerAngle = steeringAngle;
    }

    void ApplyWheels()
    {
        UpdateWheels(colliders.FRWheel, meshes.FRWheel);
        UpdateWheels(colliders.FLWheel, meshes.FLWheel);
        UpdateWheels(colliders.RRWheel, meshes.RRWheel);
        UpdateWheels(colliders.RLWheel, meshes.RLWheel);
    }

    void UpdateWheels(WheelCollider coll, MeshRenderer mesh)
    {
        Quaternion quat;
        Vector3 position;

        coll.GetWorldPose(out position, out quat);
        mesh.transform.position = position;
        mesh.transform.rotation = quat;
    }

    public float GetFitness()
    {
        return net.GetFitness();
    }
}
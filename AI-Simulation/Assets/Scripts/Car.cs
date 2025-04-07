using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
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

    private NeuralNetwork net;

    private void Update()
    {
        speed = rig.velocity.magnitude;

        //CheckInput();
        ApplySpeed();
        ApplySteering();
        ApplyWheels();
    }

    void FixedUpdate()
    {
        if (initialized)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            float angleToTarget = Vector3.SignedAngle(transform.forward, (target.position - transform.position).normalized, Vector3.up);
            angleToTarget /= 180.0f;

            float[] vision = PerformRaycastVision();

            float[] inputs = new float[10];
            inputs[0] = angleToTarget;
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

            float forwardSpeed = Vector3.Dot(rig.velocity, transform.forward);
            //net.AddFitness(forwardSpeed * 0.02f); 
            float distanceFactor = Mathf.Pow(0.5f, distance / 50f);
            net.AddFitness(distanceFactor);

            net.AddFitness((1f - Mathf.Abs(inputs[0])) / 2);
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

    public void Init(NeuralNetwork net, Transform target)
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

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FRWheel;
    public WheelCollider FLWheel;
    public WheelCollider RRWheel;
    public WheelCollider RLWheel;
}

[System.Serializable]
public class WheelMeshes
{
    public MeshRenderer FRWheel;
    public MeshRenderer FLWheel;
    public MeshRenderer RRWheel;
    public MeshRenderer RLWheel;
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Globalization;
using System.Linq;

public class ExpertSteering : MonoBehaviour
{
    public SensorManager sensorManager;
    public float forwardSpeed = 5f;
    public float steerFactor = 2f;
    public float actionThreshold = 0.2f;
    public ScenarioSpawner spawner;
    public LayerMask obstacleMask;
    public float flushInterval;

    public Rigidbody rb;
    private bool crashed = false;
    private List<string> buffer = new List<string>();
    private float flushTimer = 0f;

    void Start()
    {
        ResetCar();
    }

    void FixedUpdate()
    {
        if (crashed) return;

        float[] sensors = sensorManager.ReadSensors();
        Vector3 steering = Vector3.zero;

        for (int i = 0; i < sensors.Length; i++)
        {
            float dist = Mathf.Clamp01(sensors[i]);
            float strength = 1f - dist;
            Vector3 dir = sensorManager.sensors[i].forward;
            steering += (-dir) * strength;
        }

        float steeringX = steering.x * steerFactor;
        float clamped = Mathf.Clamp(steeringX, -1f, 1f);

        transform.Rotate(0f, clamped * 60f * Time.fixedDeltaTime, 0f);
        transform.Translate(Vector3.forward * forwardSpeed * Time.fixedDeltaTime);

        int action = 1;
        if (clamped < -actionThreshold) action = 0;
        else if (clamped > actionThreshold) action = 2;

        string line =
            string.Join(",",
                sensors.Select(s => s.ToString(CultureInfo.InvariantCulture))
            )
            + "," +
            action.ToString(CultureInfo.InvariantCulture);

        buffer.Add(line);

        flushTimer += Time.fixedDeltaTime;
        if (flushTimer >= flushInterval)
        {
            FinishRun();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            crashed = true;
            buffer.Clear();
            ResetCar();
        }
    }

    public void FinishRun()
    {
        if (!crashed)
        {
            string path = Path.Combine(Application.persistentDataPath, "training.csv");
            File.AppendAllLines(path, buffer);
        }

        ResetCar();
    }

    void ResetCar()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        crashed = false;
        buffer.Clear();
        flushTimer = 0f;

        spawner.SpawnNewPattern();
    }
}
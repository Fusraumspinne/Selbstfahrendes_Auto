using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Globalization;
using System.Linq;

public class ExpertSteering : MonoBehaviour
{
    public ScenarioSpawner spawner;
    public SensorManager sensorManager;

    public string csvPath;

    public float forwardSpeed;
    public float steerFactor;
    public float actionThreshold;

    public Rigidbody rb;
    private List<string> buffer = new List<string>();

    void FixedUpdate()
    {
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
    }

    public void FinishAndSave(bool save)
    {
        if (buffer.Count > 0 && save)
        {
            File.AppendAllLines(csvPath, buffer);
        }

        buffer.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            spawner.FinishRun(false);
        }
    }
}
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ScenarioSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public GameObject carPrefab;

    private GameObject car;
    private ExpertSteering steering;

    public int minObstacleCount;
    public int maxObstacleCount;
    public float minObstacleSize;
    public float maxObstacleSize;
    public Vector2 areaSize;
    public float clearCenterSize;
    public Vector3 center;

    public float flushInterval;
    private float flushTimer;

    public string csvPath;

    private List<GameObject> obstacles = new List<GameObject>();

    void Start()
    {
        car = Instantiate(carPrefab, transform.position, Quaternion.identity);
        car.GetComponent<ExpertSteering>().spawner = this;
        steering = car.GetComponent<ExpertSteering>();
        steering.csvPath = csvPath;

        SpawnNewPattern();
    }

    void Update()
    {
        flushTimer += Time.deltaTime;
        if (flushTimer >= flushInterval)
        {
            FinishRun(true);
        }
    }

    public void FinishRun(bool save)
    {
        flushTimer = 0f;

        car.transform.position = transform.position;
        car.transform.rotation = Quaternion.identity;

        car.GetComponent<ExpertSteering>().rb.linearVelocity = Vector3.zero;
        car.GetComponent<ExpertSteering>().rb.angularVelocity = Vector3.zero;

        car.GetComponent<ExpertSteering>().FinishAndSave(save);

        SpawnNewPattern();
    }

    public void SpawnNewPattern()
    {
        int obstacleCount = UnityEngine.Random.Range(minObstacleCount, maxObstacleCount);

        foreach (var o in obstacles)
        {
            Destroy(o);
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 pos;
            int attempts = 0;

            do
            {
                pos = transform.position + new Vector3(UnityEngine.Random.Range(-areaSize.x / 2f, areaSize.x / 2f), 0, UnityEngine.Random.Range(-areaSize.y / 2f, areaSize.y / 2f));
                attempts++;
                if (attempts > 100) break;
            } while (Vector3.Distance(pos, transform.position) < clearCenterSize);

           GameObject o = (Instantiate(obstaclePrefab, pos, Quaternion.identity));
           obstacles.Add(o);
           float randomSize = UnityEngine.Random.Range(minObstacleSize, maxObstacleSize);
           o.transform.localScale = new Vector3(randomSize, 1f, randomSize);
        }
    }
}
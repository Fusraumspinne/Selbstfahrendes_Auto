using UnityEngine;
using System;

public class TrainingManager : MonoBehaviour
{
    public ScenarioSpawner spawnerPrefab;

    public int simulations;
    public float spacing;
    private string sessionFolder;

    void Start()
    {
        sessionFolder = System.IO.Path.Combine(Application.persistentDataPath, "TrainingData");

        System.IO.Directory.CreateDirectory(sessionFolder);

        int cols = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, simulations)));
        int rows = Mathf.CeilToInt((float)simulations / cols);

        float totalWidth = (cols - 1) * spacing;
        float totalHeight = (rows - 1) * spacing;

        for (int i = 0; i < simulations; i++)
        {
            int col = i % cols;
            int row = i / cols;

            Vector3 offset = new Vector3(col * spacing - totalWidth / 2f, 0, row * spacing - totalHeight / 2f);

            ScenarioSpawner s = Instantiate(spawnerPrefab, offset, Quaternion.identity);

            string csvPath = System.IO.Path.Combine(sessionFolder, $"sim_{i}.csv");

            s.csvPath = csvPath;
        }
    }
}
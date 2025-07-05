// NEAT_Manager.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SavedConnection
{
    public int inNode;
    public int outNode;
    public float weight;
    public bool enabled;
}

[System.Serializable]
public class SavedNEATNetwork
{
    public int generation;
    public float fitness;
    public int inputCount;
    public int outputCount;
    public List<NodeGene> nodes;
    public List<SavedConnection> conns;
}

public class NEAT_Manager : MonoBehaviour
{
    public bool testBestAgent;

    public GameObject agentPrefab;
    public Transform target;
    public int populationSize;
    public float generationTime;
    public int inputCount;  
    public int outputCount;

    private List<NEAT_NeuralNetwork> nets;
    private List<NEAT_Car> agents;
    private int generation = 0;

    public GameObject spawnWallZone;
    public float spawnWallTimer;

    void Start()
    {
        if (testBestAgent)
        {
            TestBestAgent();
            return;
        }

        InitPopulation();
        SpawnAgents();
        InvokeRepeating(nameof(NextGeneration), generationTime, generationTime);
    }

    private void InitPopulation()
    {
        spawnWallZone.SetActive(false);
        StartCoroutine(SpawnWallZone());

        string path = Path.Combine(Application.persistentDataPath, "best_neat_agent.json");

        nets = new List<NEAT_NeuralNetwork>();

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var saved = JsonUtility.FromJson<SavedNEATNetwork>(json);

            generation = saved.generation;

            var baseNet = new NEAT_NeuralNetwork(saved.inputCount, saved.outputCount);
            baseNet.nodes.Clear();
            baseNet.conns.Clear();

            foreach (var n in saved.nodes)
                baseNet.nodes.Add(new NodeGene(n.id, n.type));

            foreach (var c in saved.conns)
            {
                var cg = new ConnectionGene(c.inNode, c.outNode, c.weight) { enabled = c.enabled };
                baseNet.conns.Add(cg);
            }

            for (int i = 0; i < populationSize / 2; i++)
                nets.Add(new NEAT_NeuralNetwork(baseNet)); // kopieren

            for (int i = populationSize / 2; i < populationSize; i++)
            {
                var mutated = new NEAT_NeuralNetwork(baseNet);
                mutated.Mutate();
                nets.Add(mutated);
            }

            Debug.Log("Loaded saved agent from previous session. Continuing at generation " + generation);
        }
        else
        {
            for (int i = 0; i < populationSize; i++)
            {
                var net = new NEAT_NeuralNetwork(inputCount, outputCount);
                net.Mutate();
                nets.Add(net);
            }

            generation = 0;
            Debug.Log("No save found. Starting new training session.");
        }
    }

    private void TestBestAgent()
    {
        string path = Path.Combine(Application.persistentDataPath, "best_neat_agent.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning("Kein gespeicherter Agent gefunden.");
            return;
        }

        string json = File.ReadAllText(path);
        var saved = JsonUtility.FromJson<SavedNEATNetwork>(json);

        var net = new NEAT_NeuralNetwork(saved.inputCount, saved.outputCount);
        net.nodes.Clear();
        net.conns.Clear();

        foreach (var n in saved.nodes)
            net.nodes.Add(new NodeGene(n.id, n.type));

        foreach (var c in saved.conns)
        {
            var cg = new ConnectionGene(c.inNode, c.outNode, c.weight) { enabled = c.enabled };
            net.conns.Add(cg);
        }

        var go = Instantiate(agentPrefab, Vector3.zero + Vector3.up * 2, Quaternion.identity);
        var car = go.GetComponent<NEAT_Car>();
        car.Init(net, target);

        Debug.Log("Test: Agent mit gespeichertem Netzwerk gestartet.");
    }

    private void SpawnAgents()
    {
        if (agents != null)
            foreach (var a in agents)
                if (a) Destroy(a.gameObject);

        agents = new List<NEAT_Car>();
        foreach (var net in nets)
        {
            var go = Instantiate(agentPrefab, RandomSpawnPos(), Quaternion.identity);
            var car = go.GetComponent<NEAT_Car>();
            car.Init(net, target);
            agents.Add(car);
        }
    }

    private Vector3 RandomSpawnPos()
        => new Vector3(Random.Range(-5f, 5f), 2f, Random.Range(-5f, 5f));

    private void NextGeneration()
    {
        spawnWallZone.SetActive(false);
        StartCoroutine(SpawnWallZone());

        nets.Sort((a, b) => b.GetFitness().CompareTo(a.GetFitness()));

        SaveBestNetwork(nets[0]);

        int survive = populationSize / 2;
        var newPop = new List<NEAT_NeuralNetwork>();

        // Elitism
        for (int i = 0; i < survive; i++)
            newPop.Add(new NEAT_NeuralNetwork(nets[i]));

        for (int i = survive; i < populationSize; i++)
        {
            var child = new NEAT_NeuralNetwork(nets[i - survive]);
            child.Mutate();
            newPop.Add(child);
        }

        nets = newPop;
        generation++;
        SpawnAgents();
    }

    private void SaveBestNetwork(NEAT_NeuralNetwork bestNet)
    {
        var save = new SavedNEATNetwork
        {
            generation = generation,
            fitness = bestNet.GetFitness(),
            inputCount = inputCount,
            outputCount = outputCount,
            nodes = bestNet.nodes,
            conns = new List<SavedConnection>()
        };

        foreach (var c in bestNet.conns)
        {
            save.conns.Add(new SavedConnection
            {
                inNode = c.inNode,
                outNode = c.outNode,
                weight = c.weight,
                enabled = c.enabled
            });
        }

        string json = JsonUtility.ToJson(save, true);
        string path = Path.Combine(Application.persistentDataPath, "best_neat_agent.json");
        File.WriteAllText(path, json);
    }

    private IEnumerator SpawnWallZone()
    {
        yield return new WaitForSeconds(spawnWallTimer);
        spawnWallZone.SetActive(true);
    }
}

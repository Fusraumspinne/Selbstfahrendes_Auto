using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ManagerNEAT implementiert eine stark vereinfachte NEAT‐Population ohne Spezies‐Mechanismus.
/// Zusätzlich speichert es am Ende jeder Generation das beste Genome als JSON.
/// </summary>
public class ManagerNEAT : MonoBehaviour
{
    [Header("=== Populationseinstellungen ===")]
    [Tooltip("Anzahl Netzwerke in jeder Generation")]
    [SerializeField] private int populationSize = 50;

    [Tooltip("Anzahl Input‐Knoten (z. B. Sensoren + Winkel etc.)")]
    [SerializeField] private int numInputs = 11;

    [Tooltip("Anzahl Output‐Knoten (z. B. Lenkung, Gas)")]
    [SerializeField] private int numOutputs = 2;

    [Tooltip("Minimaler Anteil (%) der besten Genome, die überleben (0–1)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float survivalRate = 0.5f;

    [Header("=== UI & Prefabs ===")]
    [SerializeField] private Text generationText;      // Anzeige der aktuellen Generation
    [SerializeField] private GameObject carPrefab;     // Prefab für den Car-Agent
    [SerializeField] private GameObject target;        // Zielpunkt im Spielraum

    [Header("=== Mutationsparameter ===")]
    [Tooltip("Wahrscheinlichkeit, pro Genom eine Gewichtsmutation auszuführen (0–1)")]
    [SerializeField] private float weightMutateChance = 0.9f;

    [Tooltip("Wahrscheinlichkeit, pro Genom einen Add‐Connection‐Mutationsversuch (0–1)")]
    [SerializeField] private float addConnMutateChance = 0.3f;

    [Tooltip("Wahrscheinlichkeit, pro Genom einen Add‐Node‐Mutationsversuch (0–1)")]
    [SerializeField] private float addNodeMutateChance = 0.2f;

    // Die aktuelle Population von Genome‐Objekten
    public List<Genome> population;    // public, damit CarNEAT darauf zugreifen kann

    private int generation = 0;

    // Aktive Car‐Agenten in der aktuellen Generation
    private List<CarNEAT> activeAgents = new List<CarNEAT>();

    private void Start()
    {
        // 1) Reset Innovation‐Zähler
        Innovation.Reset();

        // 2) Populationsinitialisierung
        InitializePopulation();

        // 3) Starte die erste Generation
        StartGeneration();
    }

    private void InitializePopulation()
    {
        population = new List<Genome>();
        for (int i = 0; i < populationSize; i++)
        {
            Genome g = new Genome(numInputs, numOutputs);
            population.Add(g);
        }
    }

    private void StartGeneration()
    {
        // UI‐Update
        generation++;
        if (generationText != null)
            generationText.text = $"Generation: {generation}";

        // Entferne alte Agenten (falls vorhanden)
        foreach (var agent in activeAgents)
            if (agent != null) Destroy(agent.gameObject);
        activeAgents.Clear();

        // Für jedes Genome erzeugen wir einen CarNEAT‐Agenten
        for (int i = 0; i < population.Count; i++)
        {
            Genome g = population[i];
            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0.5f, UnityEngine.Random.Range(-5f, 5f));
            GameObject carGO = Instantiate(carPrefab, spawnPos, Quaternion.identity);
            CarNEAT brain = carGO.GetComponent<CarNEAT>();
            brain.Init(new NeuralNetworkNEAT(g), target.transform, OnAgentDeath, i);
            activeAgents.Add(brain);
        }
    }

    /// <summary>
    /// Callback, wenn ein Agent (Crash o.Ä.) ausscheidet.
    /// Sobald alle Agenten tot sind, wird eine neue Generation erstellt.
    /// </summary>
    public void OnAgentDeath(CarNEAT agent, int genomeIndex)
    {
        // Wenn alle Agenten tot sind, weiter zur Selektion
        if (activeAgents.All(a => a.IsDead))
        {
            // 1) Speichere das aktuell beste Genome
            SaveBestGenome();

            // 2) Erzeuge die nächste Generation
            NextPopulation();
        }
    }

    /// <summary>
    /// Geht von der sortierten Population aus (absteigend nach fitness),
    /// wählt die Top-X% als Überlebende und füllt den Rest per 
    /// Crossover+Mutation wieder auf.
    /// </summary>
    private void NextPopulation()
    {
        // Sortieren nach Fitness (desc)
        population = population.OrderByDescending(g => g.fitness).ToList();

        // 2) Überlebende kopieren
        int survivors = Mathf.CeilToInt(populationSize * survivalRate);
        List<Genome> newPop = new List<Genome>();
        for (int i = 0; i < survivors; i++)
        {
            newPop.Add(population[i].Clone());
        }

        // 3) Kinder erzeugen via Crossover, bis Population voll
        while (newPop.Count < populationSize)
        {
            // Wähle zwei Eltern zufällig aus den Überlebenden
            Genome parent1 = newPop[UnityEngine.Random.Range(0, survivors)];
            Genome parent2 = newPop[UnityEngine.Random.Range(0, survivors)];

            // Achte darauf, dass parent1.fitness >= parent2.fitness
            if (parent2.fitness > parent1.fitness)
            {
                var tmp = parent1; parent1 = parent2; parent2 = tmp;
            }

            // Crossover
            Genome child = parent1.Crossover(parent2);

            // 4) Mutationen am Kind
            if (UnityEngine.Random.value < weightMutateChance)
                child.MutateWeights();

            if (UnityEngine.Random.value < addConnMutateChance)
                child.MutateAddConnection();

            if (UnityEngine.Random.value < addNodeMutateChance)
                child.MutateAddNode();

            newPop.Add(child);
        }

        population = newPop;
        StartGeneration();
    }

    /// <summary>
    /// Ermittelt das beste Genome (höchste fitness) und speichert es als JSON.
    /// </summary>
    private void SaveBestGenome()
    {
        if (population == null || population.Count == 0) return;

        // 1) Finde Genome mit max. Fitness
        Genome best = population.OrderByDescending(g => g.fitness).First();

        // 2) Baue SerializableGenome auf
        SerializableGenome s = new SerializableGenome();
        s.nodes = new List<SerializableNode>();
        s.connections = new List<SerializableConnection>();
        s.fitness = best.fitness;
        s.generation = generation;

        // Knoten serialisieren
        foreach (var n in best.nodes)
        {
            SerializableNode sn = new SerializableNode
            {
                id = n.id,
                type = n.type
            };
            s.nodes.Add(sn);
        }

        // Verbindungen serialisieren
        foreach (var c in best.connections)
        {
            SerializableConnection sc = new SerializableConnection
            {
                inNode = c.inNode,
                outNode = c.outNode,
                weight = c.weight,
                enabled = c.enabled,
                innovationID = c.innovationID
            };
            s.connections.Add(sc);
        }

        // 3) In JSON umwandeln (schön formatiert)
        string json = JsonUtility.ToJson(s, true);

        // 4) In Datei schreiben
        string fileName = "best_genome.json";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, json);

        Debug.Log($"[ManagerNEAT] Bestes Genome (Gen {generation}, Fitness {best.fitness:F3}) gespeichert in:\n{path}");
    }
}

[Serializable]
public class SerializableNode
{
    public int id;
    public NodeType type;
}

[Serializable]
public class SerializableConnection
{
    public int inNode;
    public int outNode;
    public float weight;
    public bool enabled;
    public int innovationID;
}

[Serializable]
public class SerializableGenome
{
    public List<SerializableNode> nodes;
    public List<SerializableConnection> connections;
    public float fitness;
    public int generation;
}
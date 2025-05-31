using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Ein Genome repräsentiert den „Genotyp“ eines Netzwerks:
/// - Eine Liste von Nodes (Knoten)
/// - Eine Liste von Connections (gerichtete Kanten)
/// Außerdem enthält es Methoden zum Mutieren und Kreuzbefruchten (Crossover).
/// </summary>
public class Genome
{
    public List<NodeGene> nodes = new List<NodeGene>();
    public List<ConnectionGene> connections = new List<ConnectionGene>();
    public float fitness = 0f; // Wird nach Evaluation gesetzt

    private System.Random rng = new System.Random();

    /// <summary>
    /// Erzeugt ein neues, minimal vernetztes Genom mit numInputs Input-Knoten 
    /// und numOutputs Output-Knoten. Alle Input-Knoten werden direkt mit allen 
    /// Output-Knoten verbunden (initial fully connected feedforward).
    /// </summary>
    public Genome(int numInputs, int numOutputs)
    {
        // 1) Erzeuge Input‐Knoten (IDs 0..numInputs-1)
        for (int i = 0; i < numInputs; i++)
            nodes.Add(new NodeGene(i, NodeType.Input));

        // 2) Erzeuge Output‐Knoten (IDs numInputs..numInputs+numOutputs-1)
        for (int j = 0; j < numOutputs; j++)
            nodes.Add(new NodeGene(numInputs + j, NodeType.Output));

        // 3) Initiale Verbindungen (fully connect Input → Output)
        foreach (NodeGene inN in nodes.Where(n => n.type == NodeType.Input))
        {
            foreach (NodeGene outN in nodes.Where(n => n.type == NodeType.Output))
            {
                int innov = Innovation.Next();
                float w = UnityEngine.Random.Range(-1f, 1f);
                connections.Add(new ConnectionGene(inN.id, outN.id, w, true, innov));
            }
        }
    }

    /// <summary>
    /// Klont dieses Genom (deep copy).
    /// </summary>
    public Genome Clone()
    {
        Genome clone = new Genome();
        clone.nodes = nodes.Select(n => n.Clone()).ToList();
        clone.connections = connections.Select(c => c.Clone()).ToList();
        clone.fitness = this.fitness;
        return clone;
    }

    // Privater Konstruktor für Clone
    private Genome() { }

    /// <summary>
    /// Mutation: Zufällige Anpassung von Gewichten (kleine Änderungen).
    /// </summary>
    public void MutateWeights(float perturbChance = 0.8f, float stepSize = 0.1f, float replaceChance = 0.1f)
    {
        foreach (var conn in connections)
        {
            if (UnityEngine.Random.value < perturbChance)
            {
                // Mit stepSize zufällig addieren/subtrahieren
                conn.weight += UnityEngine.Random.Range(-stepSize, stepSize);
            }
            else if (UnityEngine.Random.value < replaceChance)
            {
                // Gewicht komplett neu zufällig setzen
                conn.weight = UnityEngine.Random.Range(-1f, 1f);
            }
        }
    }

    /// <summary>
    /// Mutation: Fügt eine neue Verbindung (wenn möglich) zwischen zwei bislang 
    /// nicht verbundenen Knoten hinzu. Verhindert Duplikate.
    /// </summary>
    public void MutateAddConnection(int maxTries = 100)
    {
        for (int tries = 0; tries < maxTries; tries++)
        {
            // Wähle zwei zufällige Knoten
            NodeGene a = nodes[rng.Next(nodes.Count)];
            NodeGene b = nodes[rng.Next(nodes.Count)];

            // Verboten: Verbindung von Knoten zu sich selbst
            if (a.id == b.id) continue;

            // Erlaube nur Feedforward (Input→Hidden, Input→Output, Hidden→Hidden, Hidden→Output)
            if (a.type == NodeType.Output && b.type == NodeType.Input) continue;

            // Prüfe, ob Verbindung bereits existiert
            bool exists = connections.Any(c => c.inNode == a.id && c.outNode == b.id);
            if (exists) continue;

            // Neue Verbindung erstellen
            int innov = Innovation.Next();
            float w = UnityEngine.Random.Range(-1f, 1f);
            connections.Add(new ConnectionGene(a.id, b.id, w, true, innov));
            return;
        }
        // Falls nach maxTries keine neue Verbindung gefunden, macht Methode nichts.
    }

    /// <summary>
    /// Mutation: Fügt einen neuen Knoten ein (split a connection). 
    /// Wähle zufällig eine bestehende, aktive Verbindung, deaktiviere sie und 
    /// setze an ihre Stelle: inNode→neuNode (Gewicht=1), neuNode→outNode (Gewicht=altesGewicht).
    /// </summary>
    public void MutateAddNode()
    {
        // Filter aktiver Verbindungen (enabled == true)
        var enabledConns = connections.Where(c => c.enabled).ToList();
        if (enabledConns.Count == 0) return;

        // Wähle zufällig eine Verbindung
        ConnectionGene conn = enabledConns[rng.Next(enabledConns.Count)];
        conn.enabled = false; // Deaktiviere sie

        // Erzeuge neuen Knoten (ID = nächstfreier, Typ Hidden)
        int newNodeId = nodes.Max(n => n.id) + 1;
        NodeGene newNode = new NodeGene(newNodeId, NodeType.Hidden);
        nodes.Add(newNode);

        // Zwei neue Verbindungen:
        // 1) inNode → newNode (Gewicht = 1.0f)
        int innov1 = Innovation.Next();
        connections.Add(new ConnectionGene(conn.inNode, newNodeId, 1f, true, innov1));

        // 2) newNode → outNode (Gewicht = ursprüngliches Gewicht)
        int innov2 = Innovation.Next();
        connections.Add(new ConnectionGene(newNodeId, conn.outNode, conn.weight, true, innov2));
    }

    /// <summary>
    /// Führt ein einfaches Crossover zwischen diesem (parent1) und parent2 durch.
    /// Voraussetzungen:
    /// - parent1.fitness >= parent2.fitness
    /// - Eltern teilen sich denselben Node-Set (IDs)
    /// </summary>
    public Genome Crossover(Genome parent2)
    {
        Genome child = new Genome();
        child.nodes = this.nodes.Select(n => n.Clone()).ToList();

        // Kombiniere Connections anhand der Innovationsnummern
        var dict2 = parent2.connections.ToDictionary(c => c.innovationID, c => c);
        foreach (var c1 in this.connections)
        {
            if (dict2.ContainsKey(c1.innovationID))
            {
                // Matching gene: wähle zufällig von einem der beiden Eltern
                ConnectionGene c2 = dict2[c1.innovationID];
                ConnectionGene chosen = (UnityEngine.Random.value < 0.5f) ? c1 : c2;
                child.connections.Add(chosen.Clone());
            }
            else
            {
                // Disjunkt oder Überlegen: nehmen von fitterem Elternteil (this)
                child.connections.Add(c1.Clone());
            }
        }

        return child;
    }
}

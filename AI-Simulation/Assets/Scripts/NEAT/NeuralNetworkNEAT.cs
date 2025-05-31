using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Baut aus einem Genome eine ausführbare Netzwerk-Instanz, 
/// die ein Feedforward (ohne Rekurrenz) ausführen kann.
/// </summary>
public class NeuralNetworkNEAT
{
    private Genome genome;
    // Zwischenspeicher für Knotenwerte
    private Dictionary<int, float> nodeValues = new Dictionary<int, float>();

    public NeuralNetworkNEAT(Genome g)
    {
        genome = g;
    }

    /// <summary>
    /// Führt einen Forward-Pass durch:
    /// - inputs.Length muss gleich Anzahl der Input-Knoten sein
    /// - Gibt ein Array zurück mit den Output-Werten in der Reihenfolge der Output-Knoten-IDs.
    /// </summary>
    public float[] FeedForward(float[] inputs)
    {
        nodeValues.Clear();

        // 1) Alle Input-Knoten mit Werten füllen
        var inputNodes = genome.nodes.Where(n => n.type == NodeType.Input).OrderBy(n => n.id).ToList();
        if (inputs.Length != inputNodes.Count)
            Debug.LogError("FeedForward: Falsche Anzahl Input-Werte!");

        for (int i = 0; i < inputNodes.Count; i++)
            nodeValues[inputNodes[i].id] = inputs[i];

        // 2) Berechne Werte für Hidden- und Output-Knoten in Topo-Sort-Reihenfolge
        //    Wir gehen davon aus, dass keine Zyklen existieren (rein feedforward).
        //    Deshalb wiederholen wir so lange, bis alle nicht-input-Knoten berechnet sind.
        var remainingNodes = genome.nodes.Where(n => n.type != NodeType.Input).OrderBy(n => n.id).Select(n => n.id).ToList();

        // Wiederhole, bis keine Restknoten mehr übrig
        while (remainingNodes.Count > 0)
        {
            bool progress = false;

            foreach (int nodeId in remainingNodes.ToList()) // Kopie der Liste, weil wir Elemente entfernen
            {
                // Sammle alle Verbindungen, die in diesen Knoten münden und enabled sind
                var incoming = genome.connections
                    .Where(c => c.outNode == nodeId && c.enabled)
                    .ToList();

                // Prüfe, ob alle Quellknoten bereits berechnet sind
                bool allSourcesKnown = incoming.All(c => nodeValues.ContainsKey(c.inNode));

                if (!allSourcesKnown)
                    continue;

                // Berechne gewichtete Summe
                float sum = 0f;
                foreach (var conn in incoming)
                    sum += conn.weight * nodeValues[conn.inNode];

                // Aktivierungsfunktion (Tanh)
                float activated = Mathf.Tan(sum);
                nodeValues[nodeId] = activated;

                // Knoten aus remaining entfernen
                remainingNodes.Remove(nodeId);
                progress = true;
            }

            if (!progress)
            {
                Debug.LogError("FeedForward: Topologische Sortierung gescheitert (Zyklus oder fehlende Werte).");
                break;
            }
        }

        // 3) Sammle Output‐Knoten-Werte in korrekter Reihenfolge zurück
        var outputNodes = genome.nodes.Where(n => n.type == NodeType.Output).OrderBy(n => n.id).ToList();
        float[] outputs = new float[outputNodes.Count];
        for (int i = 0; i < outputNodes.Count; i++)
        {
            outputs[i] = nodeValues.ContainsKey(outputNodes[i].id) ? nodeValues[outputNodes[i].id] : 0f;
        }

        return outputs;
    }
}

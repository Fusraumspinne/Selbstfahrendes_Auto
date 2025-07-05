using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeGene
{
    public enum NodeType { Input, Hidden, Output }
    public int id;
    public NodeType type;
    public float value;

    public NodeGene(int id, NodeType type)
    {
        this.id = id;
        this.type = type;
        this.value = 0f;
    }
}

[Serializable]
public class ConnectionGene
{
    public int inNode;
    public int outNode;
    public float weight;
    public bool enabled;

    public ConnectionGene(int inNode, int outNode, float weight)
    {
        this.inNode = inNode;
        this.outNode = outNode;
        this.weight = weight;
        this.enabled = true;
    }
}

public class NEAT_NeuralNetwork : IComparable<NEAT_NeuralNetwork>
{
    public List<NodeGene> nodes = new List<NodeGene>();
    public List<ConnectionGene> conns = new List<ConnectionGene>();
    private float fitness;

    private const float addNodeProb = 0.15f;  
    private const float addConnProb = 0.20f; 
    private const float weightMutateProb = 0.9f;  
    private const float weightPerturbProb = 0.8f;  
    private const float weightStepMax = 0.7f;  

    private System.Random rnd = new System.Random();

    public NEAT_NeuralNetwork(int inputCount, int outputCount)
    {
        int idCounter = 0;
        for (int i = 0; i < inputCount; i++)
            nodes.Add(new NodeGene(idCounter++, NodeGene.NodeType.Input));
        for (int i = 0; i < outputCount; i++)
            nodes.Add(new NodeGene(idCounter++, NodeGene.NodeType.Output));
        foreach (var nIn in nodes)
        {
            if (nIn.type != NodeGene.NodeType.Input) continue;
            foreach (var nOut in nodes)
            {
                if (nOut.type != NodeGene.NodeType.Output) continue;
                conns.Add(new ConnectionGene(nIn.id, nOut.id, UnityEngine.Random.Range(-1f, 1f)));
            }
        }
    }

    public NEAT_NeuralNetwork(NEAT_NeuralNetwork other)
    {
        rnd = other.rnd;
        foreach (var n in other.nodes)
            nodes.Add(new NodeGene(n.id, n.type));
        foreach (var c in other.conns)
        {
            var nc = new ConnectionGene(c.inNode, c.outNode, c.weight) { enabled = c.enabled };
            conns.Add(nc);
        }
    }

    public float[] FeedForward(float[] inputs)
    {
        foreach (var n in nodes) n.value = 0f;
        int idx = 0;
        foreach (var n in nodes)
            if (n.type == NodeGene.NodeType.Input)
                n.value = inputs[idx++];

        foreach (var n in nodes)
        {
            if (n.type == NodeGene.NodeType.Input) continue;
            float sum = 0f;
            foreach (var c in conns)
            {
                if (!c.enabled || c.outNode != n.id) continue;
                var src = nodes.Find(x => x.id == c.inNode);
                sum += src.value * c.weight;
            }
            n.value = (float)Math.Tanh(sum);
        }

        var outputs = new List<float>();
        foreach (var n in nodes)
            if (n.type == NodeGene.NodeType.Output)
                outputs.Add(n.value);
        return outputs.ToArray();
    }

    public void Mutate()
    {
        foreach (var c in conns)
        {
            if (rnd.NextDouble() < weightMutateProb)
            {
                if (rnd.NextDouble() < weightPerturbProb)
                    c.weight += UnityEngine.Random.Range(-weightStepMax, weightStepMax);
                else
                    c.weight = UnityEngine.Random.Range(-1f, 1f);
            }
        }
        if (rnd.NextDouble() < addConnProb)
            AddRandomConnection();
        if (rnd.NextDouble() < addNodeProb)
            AddRandomNode();
    }

    private void AddRandomConnection()
    {
        for (int i = 0; i < 100; i++)
        {
            var a = nodes[rnd.Next(nodes.Count)];
            var b = nodes[rnd.Next(nodes.Count)];

            if (a.id == b.id || b.type == NodeGene.NodeType.Input)
                continue;

            bool exists = conns.Exists(c => c.inNode == a.id && c.outNode == b.id);
            if (exists) continue;

            conns.Add(new ConnectionGene(a.id, b.id, UnityEngine.Random.Range(-1f, 1f)));
            return;
        }
    }


    private void AddRandomNode()
    {
        var candidates = conns.FindAll(c => c.enabled);
        if (candidates.Count == 0) return;
        var cOld = candidates[rnd.Next(candidates.Count)];
        cOld.enabled = false;
        int newId = nodes[^1].id + 1;
        var newNode = new NodeGene(newId, NodeGene.NodeType.Hidden);
        nodes.Add(newNode);
        conns.Add(new ConnectionGene(cOld.inNode, newId, 1f));
        conns.Add(new ConnectionGene(newId, cOld.outNode, cOld.weight));
    }

    public void AddFitness(float f) => fitness += f;
    public void SetFitness(float f) => fitness = f;
    public float GetFitness() => fitness;

    public int CompareTo(NEAT_NeuralNetwork other)
        => fitness.CompareTo(other.fitness);
}
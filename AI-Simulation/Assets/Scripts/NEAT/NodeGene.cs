using System.Collections.Generic;

/// <summary>
/// Typ eines Knotens: Input, Hidden oder Output.
/// Hidden-Knoten entstehen per Mutation.
/// </summary>
public enum NodeType
{
    Input,
    Hidden,
    Output
}

/// <summary>
/// Ein NodeGene repräsentiert ein Neuron in der NEAT‐Topologie.
/// </summary>
public class NodeGene
{
    public int id;           // Eindeutige Knoten-ID
    public NodeType type;    // Input, Hidden oder Output

    /// <summary>
    /// Konstruktor: Erzeugt einen neuen NodeGene mit gegebener ID und Typ.
    /// </summary>
    public NodeGene(int id, NodeType type)
    {
        this.id = id;
        this.type = type;
    }

    /// <summary>
    /// Klont diesen NodeGene (für Crossover o.Ä.).
    /// </summary>
    public NodeGene Clone()
    {
        return new NodeGene(this.id, this.type);
    }
}

/// <summary>
/// Eine gerichtete Verbindung (Kante) zwischen zwei Knoten, mit Gewicht, 
/// einem Enabled-Flag und einer eindeutigen Innovationsnummer.
/// </summary>
public class ConnectionGene
{
    public int innovationID; // Eindeutige Innovationsnummer
    public int inNode;       // ID des Quellknotens
    public int outNode;      // ID des Zielknotens
    public float weight;     // Gewicht der Verbindung
    public bool enabled;     // Schaltet die Verbindung ein/aus

    /// <summary>
    /// Konstruktor für eine neue Verbindung.
    /// </summary>
    public ConnectionGene(int inNode, int outNode, float weight, bool enabled, int innovationID)
    {
        this.inNode = inNode;
        this.outNode = outNode;
        this.weight = weight;
        this.enabled = enabled;
        this.innovationID = innovationID;
    }

    /// <summary>
    /// Klont diese ConnectionGene (für Crossover).
    /// </summary>
    public ConnectionGene Clone()
    {
        return new ConnectionGene(this.inNode, this.outNode, this.weight, this.enabled, this.innovationID);
    }
}

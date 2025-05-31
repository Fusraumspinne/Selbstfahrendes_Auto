using System;

/// <summary>
/// Stellt fortlaufende Innovationsnummern zur Verfügung.
/// Wird benutzt, um jedem neuen ConnectionGene eine weltweit eindeutige ID zuzuweisen.
/// </summary>
public static class Innovation
{
    private static int currentInnovation = 0;

    /// <summary>
    /// Gibt die nächstgrößere Innovationsnummer zurück.
    /// </summary>
    public static int Next()
    {
        return currentInnovation++;
    }

    /// <summary>
    /// Setzt den Zähler zurück (z.B. beim Neustart einer Population).
    /// </summary>
    public static void Reset()
    {
        currentInnovation = 0;
    }
}
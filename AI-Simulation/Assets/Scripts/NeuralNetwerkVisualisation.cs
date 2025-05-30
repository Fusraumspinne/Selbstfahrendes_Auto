using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class NeuralNetwerkVisualisation : MonoBehaviour
{
    [SerializeField] private CanvasExporter canvasExporter;

    [Header("=== Im Inspector zuweisen ===")]
    [Tooltip("Canvas im Screen Space – Overlay")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Prefab für ein einzelnes Neuron (UI-Image mit TMP_Text als Child).")]
    [SerializeField] private GameObject neuronPrefab;

    [Tooltip("Prefab für eine Verbindung (UI-Image als Linie mit TMP_Text als Child).")]
    [SerializeField] private GameObject connectionPrefab;

    [Tooltip("Name der JSON-Datei im persistentDataPath (z.B. \"agent.json\").")]
    [SerializeField] private string fileName = "agent.json";

    // Dictionary, um später schnell von (layer,index) → RectTransform des Neurons zu kommen
    private Dictionary<(int layer, int index), RectTransform> neuronMap = new();

    // --- Klassen, um die gespeicherte "agent.json"-Struktur zu parsen ---
    [System.Serializable]
    private class WeightsArray
    {
        public float[] weights;
    }

    [System.Serializable]
    private class LayerWeightsArray
    {
        public WeightsArray[] weightsArrays;
    }

    [System.Serializable]
    private class SavedNetwork
    {
        public float fitness;
        public int generation;
        public LayerWeightsArray[] layerArrays;
    }

    private void Start()
    {
        // 1) JSON-Datei einlesen
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[NeuralNetworkVisualisation] Datei nicht gefunden: {filePath}");
            return;
        }

        string jsonText = File.ReadAllText(filePath);
        SavedNetwork savedNet = null;
        try
        {
            savedNet = JsonUtility.FromJson<SavedNetwork>(jsonText);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NeuralNetworkVisualisation] JSON konnte nicht geparst werden:\n{e}");
            return;
        }

        if (savedNet == null || savedNet.layerArrays == null || savedNet.layerArrays.Length == 0)
        {
            Debug.LogError("[NeuralNetworkVisualisation] Gespeicherte Netzwerkdaten sind ungültig oder leer.");
            return;
        }

        // 2) Aus layerArrays die Layer-Größen ermitteln
        int inputSize = savedNet.layerArrays[0].weightsArrays[0].weights.Length;
        int[] layers = new int[savedNet.layerArrays.Length + 1];
        layers[0] = inputSize;
        for (int i = 0; i < savedNet.layerArrays.Length; i++)
        {
            layers[i + 1] = savedNet.layerArrays[i].weightsArrays.Length;
        }

        Debug.Log($"[NeuralNetworkVisualisation] Gefundene Layer-Größen: [{string.Join(",", layers)}]");

        // 3) Canvas & Prefabs prüfen
        if (canvas == null)
        {
            Debug.LogError("[NeuralNetworkVisualisation] Canvas ist nicht zugewiesen.");
            return;
        }
        if (neuronPrefab == null)
        {
            Debug.LogError("[NeuralNetworkVisualisation] neuronPrefab ist nicht zugewiesen.");
            return;
        }
        if (connectionPrefab == null)
        {
            Debug.LogError("[NeuralNetworkVisualisation] connectionPrefab ist nicht zugewiesen.");
            return;
        }

        // 4) Tatsächliche Canvas-Größe auslesen
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRT.rect.width;
        float canvasHeight = canvasRT.rect.height;

        // 5) Maße festlegen
        float layerSpacing = 150f;  // horizontaler Abstand zwischen Layern in px
        float neuronSize = 5f;     // Breite & Höhe eines Neurons in px
        float lineThickness = 0.5f;   // Dicke der Verbindungs-Linien in px

        // X-Start so, dass alle Layer mittig im Canvas verteilt sind
        float startX = (canvasWidth - layerSpacing * (layers.Length - 1)) / 2f;

        // 6) Neuronen spawnen
        for (int l = 0; l < layers.Length; l++)
        {
            int neuronCount = layers[l];
            if (neuronCount <= 0)
            {
                Debug.LogWarning($"[NeuralNetworkVisualisation] Layer {l} hat 0 Neuronen.");
                continue;
            }
            // Gesamthöhe = neuronCount * neuronSize + (neuronCount - 1) * neuronSize (Abstand = 1×neuronSize)
            float totalHeight = neuronCount * neuronSize + (neuronCount - 1) * neuronSize;
            // Oberste Y-Position so wählen, dass alles zentriert ist
            float startY = (canvasHeight + totalHeight) / 2f - neuronSize;

            for (int n = 0; n < neuronCount; n++)
            {
                Vector2 anchoredPos = new Vector2(
                    startX + l * layerSpacing,
                    startY - n * (neuronSize * 2)
                );

                // Neuron-Objekt instanziieren
                GameObject neuronGO = Instantiate(neuronPrefab, canvas.transform);
                if (neuronGO == null)
                {
                    Debug.LogError($"[NeuralNetworkVisualisation] Konnte neuronPrefab nicht instanziieren (Layer {l}, Neuron {n}).");
                    continue;
                }

                RectTransform rt = neuronGO.GetComponent<RectTransform>();
                if (rt == null)
                {
                    Debug.LogError($"[NeuralNetworkVisualisation] neuronPrefab hat kein RectTransform (Layer {l}, Neuron {n}).");
                    continue;
                }

                // Anchor auf Mitte setzen, sodass anchoredPosition relativ zur Canvas-Mitte ist
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

                // Größe & Position setzen
                rt.sizeDelta = new Vector2(neuronSize, neuronSize);
                Vector2 centeredPos = anchoredPos - new Vector2(canvasWidth / 2f, canvasHeight / 2f);
                rt.anchoredPosition = centeredPos;

                neuronMap[(l, n)] = rt;
            }
        }

        // 7) Verbindungen spawnen
        for (int i = 0; i < savedNet.layerArrays.Length; i++)
        {
            LayerWeightsArray lw = savedNet.layerArrays[i];
            int nextLayerIndex = i + 1;
            int prevLayerIndex = i;

            if (lw == null || lw.weightsArrays == null)
            {
                Debug.LogWarning($"[NeuralNetworkVisualisation] layerArrays[{i}] ist null oder hat keine weightsArrays.");
                continue;
            }

            for (int neuronIdx = 0; neuronIdx < lw.weightsArrays.Length; neuronIdx++)
            {
                float[] weightsOfThisNeuron = lw.weightsArrays[neuronIdx].weights;
                if (weightsOfThisNeuron == null) continue;

                for (int k = 0; k < weightsOfThisNeuron.Length; k++)
                {
                    float w = weightsOfThisNeuron[k];

                    if (!neuronMap.TryGetValue((prevLayerIndex, k), out RectTransform fromRT))
                    {
                        Debug.LogWarning($"[NeuralNetworkVisualisation] Neuron (from) nicht gefunden: Layer {prevLayerIndex}, Index {k}.");
                        continue;
                    }
                    if (!neuronMap.TryGetValue((nextLayerIndex, neuronIdx), out RectTransform toRT))
                    {
                        Debug.LogWarning($"[NeuralNetworkVisualisation] Neuron (to) nicht gefunden: Layer {nextLayerIndex}, Index {neuronIdx}.");
                        continue;
                    }

                    Vector2 start = fromRT.anchoredPosition;
                    Vector2 end = toRT.anchoredPosition;
                    Vector2 direction = (end - start).normalized;
                    float distance = Vector2.Distance(start, end);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    GameObject lineGO = Instantiate(connectionPrefab, canvas.transform);
                    if (lineGO == null)
                    {
                        Debug.LogError($"[NeuralNetworkVisualisation] Konnte connectionPrefab nicht instanziieren (Layer {prevLayerIndex}-{k} → {nextLayerIndex}-{neuronIdx}).");
                        continue;
                    }

                    RectTransform lineRT = lineGO.GetComponent<RectTransform>();
                    if (lineRT == null)
                    {
                        Debug.LogError($"[NeuralNetworkVisualisation] connectionPrefab hat kein RectTransform (Layer {prevLayerIndex}-{k} → {nextLayerIndex}-{neuronIdx}).");
                        continue;
                    }

                    // Anchor auf Mitte setzen, damit anchoredPosition relativ ist
                    lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);

                    // Linie ausrichten und skalieren
                    lineRT.pivot = new Vector2(0f, 0.5f);
                    // Dynamische Dicke basierend auf Gewicht
                    float weightAbs = Mathf.Abs(w);

                    float minThickness = 0.01f;
                    float dynamicThickness = Mathf.Lerp(minThickness, lineThickness, Mathf.Clamp01(weightAbs));

                    // Verstärkung für Werte über 1
                    if (weightAbs > 1f)
                    {
                        dynamicThickness = lineThickness * weightAbs;
                    }

                    lineRT.sizeDelta = new Vector2(distance, dynamicThickness);
                    lineRT.anchoredPosition = start; // keine weitere Zentrierung nötig
                    lineRT.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        canvasExporter.ExportCanvasToPNG();
    }
}
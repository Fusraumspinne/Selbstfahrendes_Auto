using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CanvasExporter : MonoBehaviour
{
    [Header("=== Einstellungen ===")]
    [Tooltip("Ziel-Auflösung (Breite) in Pixeln")]
    [SerializeField] private int exportWidth = 1000;
    [Tooltip("Ziel-Auflösung (Höhe) in Pixeln")]
    [SerializeField] private int exportHeight = 1000;

    [Tooltip("Pfad und Dateiname im persistentDataPath, z. B.: \"UIExport.png\"")]
    [SerializeField] private string outputFileName = "UIExport.png";

    [Tooltip("Die Kamera, die dein Canvas rendert (Screen Space – Camera).")]
    [SerializeField] private Camera uiCamera;

    [Tooltip("Layer, in dem dein Canvas und alle UI-Elemente liegen (z.B. \"UI\").")]
    [SerializeField] private string uiLayerName = "UI";

    private RenderTexture rt;

    private void Awake()
    {
        if (uiCamera == null)
        {
            Debug.LogError("CanvasExporter: Keine UI-Kamera zugewiesen!");
            return;
        }

        // Sorge dafür, dass die Kamera nur den UI-Layer rendert:
        int uiLayer = LayerMask.NameToLayer(uiLayerName);
        if (uiLayer < 0)
        {
            Debug.LogError($"CanvasExporter: Layer \"{uiLayerName}\" existiert nicht. Bitte erstelle ihn und weise Canvas/UI-Elemente darauf zu.");
        }
        else
        {
            uiCamera.cullingMask = 1 << uiLayer;
        }
    }

    /// <summary>
    /// Kann per Button oder anderem Skript aufgerufen werden, um das Canvas in hoher Auflösung zu exportieren.
    /// </summary>
    public void ExportCanvasToPNG()
    {
        if (uiCamera == null)
        {
            Debug.LogError("CanvasExporter: UI-Kamera nicht gefunden.");
            return;
        }

        // 1) RenderTexture in Zielauflösung erzeugen
        rt = new RenderTexture(exportWidth, exportHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1; // AA abschalten (bessere Schärfe)
        rt.Create();

        // 2) Kamera auf die RenderTexture richten
        RenderTexture previousRT = uiCamera.targetTexture;
        CameraClearFlags previousClearFlags = uiCamera.clearFlags;
        Color previousBackground = uiCamera.backgroundColor;
        Rect previousRect = uiCamera.rect;

        uiCamera.targetTexture = rt;
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = Color.clear; // Transparenter Hintergrund
        // Für hohe Auflösung muss das Viewport-Rect auf volle RenderTexture gestellt werden:
        uiCamera.rect = new Rect(0, 0, 1, 1);

        // 3) Einmal rendern lassen
        uiCamera.Render();

        // 4) RenderTexture in Texture2D kopieren
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(exportWidth, exportHeight, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, exportWidth, exportHeight), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // 5) PNG codieren und speichern
        byte[] pngData = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, outputFileName);
        File.WriteAllBytes(path, pngData);
        Debug.Log($"Canvas als PNG exportiert: {path}");

        // 6) Aufräumen: Kamera einstweilige Einstellung zurücksetzen
        uiCamera.targetTexture = previousRT;
        uiCamera.clearFlags = previousClearFlags;
        uiCamera.backgroundColor = previousBackground;
        uiCamera.rect = previousRect;

        Destroy(rt);
        Destroy(tex);
    }
}
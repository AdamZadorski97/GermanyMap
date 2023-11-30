using UnityEngine;
using DigitalRuby.FastLineRenderer;

public class SimpleLineDrawer : MonoBehaviour
{
    public FastLineRenderer lineRenderer;

    void Start()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("Line Renderer not assigned!");
            return;
        }

        // Create a new FastLineRendererProperties object to define the line properties
        FastLineRendererProperties lineProperties = new FastLineRendererProperties
        {
            Start = Vector3.zero, // Starting point of the line
            End = Vector3.one,    // Ending point of the line
            Color = Color.white,  // Line color
            Radius = 1.0f         // Line thickness
        };

        // Add the line to the renderer
        lineRenderer.AppendLine(lineProperties);

        // Apply the changes to the line renderer
        lineRenderer.Apply();
    }
}
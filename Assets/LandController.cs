using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandController : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public TextMesh textMesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public Material highlightMaterial; // Assign a material with the green color in the Inspector
    public Material originalMaterial;
    public MeshRenderer meshRenderer;

 

    private void OnMouseEnter()
    {
        if (highlightMaterial != null)
        {
            meshRenderer.material = highlightMaterial;
        }
    }

    private void OnMouseExit()
    {
        meshRenderer.material = originalMaterial;
    }

}

using UnityEngine;

public class MapMouseController : MonoBehaviour
{
    private LandController currentlyHighlighted = null;

    void Update()
    {
        HighlightAreaUnderMouse();
    }

    private void HighlightAreaUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        LandController closestLandController = null;
        float smallestArea = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            LandController landController = hit.collider.GetComponent<LandController>();
            if (landController != null)
            {
                float areaSize = landController.GetAreaSize();
                if (areaSize < smallestArea)
                {
                    smallestArea = areaSize;
                    closestLandController = landController;
                }
            }
        }

        if (closestLandController != currentlyHighlighted)
        {
            if (currentlyHighlighted != null)
            {
                currentlyHighlighted.DehighlightOnMouseExit();
            }

            currentlyHighlighted = closestLandController;

            if (currentlyHighlighted != null)
            {
                currentlyHighlighted.HighlightOnMouseEnter();
            }
        }
    }
}
/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example of how to limit the position and zoom the map.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/LockPositionAndZoomExample")]
    public class LockPositionAndZoomExample : MonoBehaviour
    {
        /// <summary>
        /// Reference to the map. If not specified, the current instance will be used.
        /// </summary>
        /// 
        public int maxZoom;
        public int minZoom;
        public OnlineMaps map;
        
        private void Start()
        {
            // If the map is not specified, get the current instance.
            if (map == null) map = OnlineMaps.instance;

            // Lock map zoom range
          //  map.zoomRange = new OnlineMapsRange(minZoom, maxZoom);
           // map.zoom = minZoom;
            // Lock map coordinates range
            map.positionRange = new OnlineMapsPositionRange(47, 5, 55, 15);

            // Initializes the position and zoom
           // map.zoom = 10;
           // map.position = map.positionRange.center;
        }
    }
}
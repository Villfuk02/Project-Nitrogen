
using UnityEngine;
using Utils;

namespace World
{
    public class TileSelection : MonoBehaviour
    {
        [Header("Referneces")]
        [SerializeField] Camera mainCamera;
        [Header("Runtime values")]
        [SerializeField] Tile hoveredTile;
        LayerMask tileSelectionMask;
        private void Awake()
        {
            tileSelectionMask = LayerMask.GetMask(LayerNames.TILE_SELECTION);
        }
        void Update()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Tile? newHoveredTile = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100, tileSelectionMask))
                newHoveredTile = hit.transform.GetComponentInParent<Tile>();

            if (newHoveredTile != hoveredTile)
            {
                if (hoveredTile != null) hoveredTile.Unhover();
                hoveredTile = newHoveredTile;
                if (newHoveredTile != null) newHoveredTile.Hover();
            }
        }
    }
}


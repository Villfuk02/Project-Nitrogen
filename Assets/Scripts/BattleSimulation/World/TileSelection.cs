using UnityEngine;
using Utils;

namespace BattleSimulation.World
{
    public class TileSelection : MonoBehaviour
    {
        LayerMask tileSelectionMask_;
        [Header("References")]
        [SerializeField] Camera mainCamera;
        [Header("Runtime values")]
        [SerializeField] Tile hoveredTile;

        void Awake()
        {
            tileSelectionMask_ = LayerMask.GetMask(LayerNames.TILE_SELECTION);
        }
        void Update()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Tile? newHoveredTile = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100, tileSelectionMask_))
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


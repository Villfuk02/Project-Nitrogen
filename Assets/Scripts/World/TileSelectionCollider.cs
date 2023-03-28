using UnityEngine;

namespace Assets.Scripts.World
{
    public class TileSelectionCollider : MonoBehaviour
    {
        [SerializeField]
        private Tile tile;

        private void OnMouseEnter()
        {
            tile.Hover();
        }

        private void OnMouseExit()
        {
            tile.Unhover();
        }
    }
}
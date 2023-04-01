using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.World
{
    public class Tile : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform slantedParts;
        [SerializeField] Animator selectionAnimator;
        [SerializeField] GameObject blockerCollider;
        [Header("Properties")]
        public Vector2Int pos;
        public enum Obstacle { None, Path, Short, Tall, Crystals, Minerals }
        public Obstacle obstacle;
        public bool slant;
        [Header("Runtime variables")]
        public bool occupied; // change to building ref
        [SerializeField] bool hovered;

        public void Init(WorldUtils.Slant slantDir)
        {
            if (slant)
                slantedParts.Rotate(WorldUtils.WORLD_CARDINAL_DIRS[((int)slantDir) % 4] * WorldUtils.SLANT_ANGLE);
            blockerCollider.SetActive(obstacle == Obstacle.Tall);
        }
        public void Hover()
        {
            hovered = true;
            selectionAnimator.SetTrigger("Hover");
        }
        public void Unhover()
        {
            hovered = false;
            selectionAnimator.SetTrigger("Unhover");
        }
    }
}
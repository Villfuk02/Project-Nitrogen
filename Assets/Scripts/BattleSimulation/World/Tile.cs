using BattleSimulation.Buildings;
using UnityEngine;
using Utils;

namespace BattleSimulation.World
{
    public class Tile : MonoBehaviour
    {
        static readonly int HoverTrigger = Animator.StringToHash("Hover");
        static readonly int UnhoverTrigger = Animator.StringToHash("Unhover");
        [Header("References")]
        [SerializeField] Transform slantedParts;
        [SerializeField] Animator selectionAnimator;
        [SerializeField] GameObject blockerCollider;
        public Transform decorationHolder;
        [SerializeField] SpriteRenderer[] highlights;
        [Header("Properties")]
        public Vector2Int pos;
        public enum Obstacle { None, Path, Small, Large, Fuel, Minerals }
        public Obstacle obstacle;
        public WorldUtils.Slant slant;
        [Header("Runtime variables")]
        public Building? building;
        [SerializeField] bool hovered;
        [SerializeField] bool selected;

        void Start()
        {
            if (slant != WorldUtils.Slant.None)
                slantedParts.Rotate(WorldUtils.WORLD_CARDINAL_DIRS[(int)slant % 4] * WorldUtils.SLANT_ANGLE);
            blockerCollider.SetActive(obstacle == Obstacle.Large);
        }
        public void Hover()
        {
            hovered = true;
            selectionAnimator.SetTrigger(HoverTrigger);
        }
        public void Unhover()
        {
            hovered = false;
            selectionAnimator.SetTrigger(UnhoverTrigger);
        }

        public void SetHighlightColor(Color color)
        {
            foreach (var sr in highlights)
            {
                sr.color = color;
            }
        }
    }
}
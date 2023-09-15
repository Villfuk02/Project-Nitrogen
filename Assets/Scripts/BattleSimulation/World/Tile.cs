using BattleSimulation.Buildings;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleSimulation.World
{
    public class Tile : MonoBehaviour, IHighlightable
    {
        [Header("References")]
        [SerializeField] Transform slantedParts;
        [SerializeField] Animator highlightAnim;
        public Transform decorationHolder;
        [SerializeField] SpriteRenderer[] highlights;
        [Header("Properties")]
        public Vector2Int pos;
        public enum Obstacle { None, Path, Small, Large, Fuel, Minerals }
        public Obstacle obstacle;
        public WorldUtils.Slant slant;
        [Header("Runtime variables")]
        public Building? building;

        void Start()
        {
            if (slant != WorldUtils.Slant.None)
                slantedParts.Rotate(WorldUtils.WORLD_CARDINAL_DIRS[(int)slant % 4] * WorldUtils.SLANT_ANGLE);
        }

        public void Highlight(Color color)
        {
            foreach (var sr in highlights)
            {
                sr.color = color;
            }
            highlightAnim.SetTrigger(IHighlightable.HIGHLIGHT_TRIGGER);
        }

        public void Unhighlight()
        {
            highlightAnim.SetTrigger(IHighlightable.UNHIGHLIGHT_TRIGGER);
        }
    }
}
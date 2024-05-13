using BattleSimulation.Buildings;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleSimulation.World
{
    public class Tile : MonoBehaviour, IHighlightable
    {
        public enum Obstacle { None, Path, Small, Large, Fuel, Minerals }

        [Header("References")]
        [SerializeField] Transform slantedParts;
        [SerializeField] Animator highlightAnim;
        public Transform decorationHolder;
        [SerializeField] SpriteRenderer[] highlights;
        [Header("Settings - auto-assigned")]
        public Vector2Int pos;
        public Obstacle obstacle;
        public WorldUtils.Slant slant;

        [Header("Runtime variables")]
        [SerializeField] Building building;
        public Building? Building
        {
            get { return building; }
            set
            {
                building = value;
                if (building == null)
                    return;
                SetupBuilding(building);
            }
        }

        void Start()
        {
            if (slant != WorldUtils.Slant.None)
                slantedParts.Rotate(WorldUtils.WORLD_CARDINAL_DIRS[(int)slant % 4] * WorldUtils.SLANT_ANGLE);
        }

        public void SetupBuilding(Building b)
        {
            b.transform.localPosition = Vector3.zero;
            b.transform.SetParent(slantedParts, false);
            foreach (var t in b.rotateBack)
                t.rotation = b.transform.localRotation;
        }

        public void Highlight(Color color)
        {
            foreach (var sr in highlights)
                sr.color = color;

            highlightAnim.SetTrigger(IHighlightable.HIGHLIGHT_TRIGGER);
        }

        public void Unhighlight()
        {
            highlightAnim.SetTrigger(IHighlightable.UNHIGHLIGHT_TRIGGER);
        }
    }
}
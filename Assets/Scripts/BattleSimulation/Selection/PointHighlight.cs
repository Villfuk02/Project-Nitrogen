using BattleVisuals.Selection.Highlightable;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class PointHighlight : MonoBehaviour, IHighlightable
    {
        [Header("References")]
        [SerializeField] Animator highlightAnim;
        [SerializeField] SpriteRenderer[] highlights;

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
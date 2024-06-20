using UnityEngine;

namespace BattleVisuals.Selection.Highlightable
{
    public interface IHighlightable
    {
        static readonly int HIGHLIGHT_TRIGGER = Animator.StringToHash("Highlight");
        static readonly int UNHIGHLIGHT_TRIGGER = Animator.StringToHash("Unhighlight");

        public void Highlight(Color color);

        public void Unhighlight();
    }

    public enum HighlightType { Clear, Selected, Negative, Affected, Special, Hovered, Unassigned = 0xFF }
}
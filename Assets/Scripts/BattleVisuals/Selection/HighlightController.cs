using BattleSimulation.Selection;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class HighlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] SelectionController selection;
        [SerializeField] RangeVisualization rangeVisualization;
        [Header("Settings")]
        public Color[] highlightColors;
        [Header("Runtime variables")]
        public HighlightProvider highlightProvider;
        [SerializeField] bool doFixedReset;
        [SerializeField] bool doReset;
        readonly Dictionary<IHighlightable, HighlightType> highlighted_ = new();

        void Update()
        {
            var hp = GetHighlightProvider();
            var hpChanged = highlightProvider != hp;
            highlightProvider = hp;
            var hovered = GetHovered();

            if (hp == null)
            {
                ClearVisuals(hovered, hpChanged);
                return;
            }

            if (doReset || hpChanged)
            {
                if (doReset && !doFixedReset)
                    doReset = false;

                rangeVisualization.ResetVisuals();
            }
            UpdateHighlights(hp.GetHighlights(), hovered, selection.placing == null || selection.placing.IsValid());
            rangeVisualization.UpdateVisuals(selection.hoverTilePosition ?? Vector2.zero);
        }

        void ClearVisuals(IHighlightable hovered, bool hpChanged)
        {
            UpdateHighlights(null, hovered);
            doFixedReset = false;
            doReset = false;

            if (hpChanged)
                rangeVisualization.ClearVisuals();
        }

        IHighlightable GetHovered()
        {
            if (selection.hovered == null)
                return null;

            if (selection.hovered.attacker != null)
                return selection.hovered.attacker;
            return selection.hovered.tile;
        }

        HighlightProvider GetHighlightProvider()
        {
            if (selection.selected != null)
                return selection.selected.GetComponent<HighlightProvider>();
            if (selection.placing != null)
                return selection.placing.GetComponent<HighlightProvider>();
            if (selection.hovered != null)
                return selection.hovered.GetComponent<HighlightProvider>();
            return null;
        }

        void FixedUpdate()
        {
            if (doFixedReset)
                doFixedReset = false;
        }

        public void ResetVisuals()
        {
            doFixedReset = true;
            doReset = true;
        }

        void UpdateHighlights(IEnumerable<(HighlightType, IHighlightable)> newHighlights, IHighlightable hovered, bool isHoveredValid = true)
        {
            List<(HighlightType highlight, IHighlightable element)> highlightList = newHighlights?.ToList() ?? new();

            if (hovered != null)
                HighlightHovered(hovered, isHoveredValid, highlightList);
            UpdateChangedHighlights(highlightList);
            RemoveOldHighlights(highlightList);
        }

        void RemoveOldHighlights(IEnumerable<(HighlightType highlight, IHighlightable element)> newHighlights)
        {
            var keep = newHighlights.Select(p => p.element).ToHashSet();
            foreach (var element in highlighted_.Keys.Where(element => !keep.Contains(element)).ToArray())
            {
                highlighted_.Remove(element);
                if (element as Object != null)
                    element.Unhighlight();
            }
        }

        void UpdateChangedHighlights(List<(HighlightType highlight, IHighlightable element)> newHighlights)
        {
            foreach (var (highlight, element) in newHighlights)
            {
                if (highlighted_.TryGetValue(element, out var cachedType) && highlight == cachedType)
                    continue;

                highlighted_[element] = highlight;
                element.Highlight(highlightColors[(int)highlight]);
            }
        }

        static void HighlightHovered(IHighlightable hovered, bool isValid, List<(HighlightType highlight, IHighlightable element)> highlightList)
        {
            var hoverHighlight = isValid ? HighlightType.Hovered : HighlightType.Negative;
            highlightList.RemoveAll(e => e.element == hovered);
            highlightList.Add((hoverHighlight, hovered));
        }
    }
}

using BattleSimulation.Selection;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleVisuals.Selection
{
    public class HighlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] SelectionController selection;
        [SerializeField] RangeVisualization rangeVisualization;
        [SerializeField] PointHighlight pointHighlight;
        [Header("Settings")]
        public Color[] highlightColors;
        [Header("Runtime variables")]
        public HighlightProvider highlightProvider;
        [SerializeField] bool doFixedReset;
        [SerializeField] bool doReset;
        Dictionary<IHighlightable, HighlightType> currentlyHighlighted_ = new();

        void Update()
        {
            UpdateHighlightProvider(out var hpChanged);

            if (doReset || hpChanged)
            {
                if (doReset && !doFixedReset)
                    doReset = false;

                if (highlightProvider == null)
                    rangeVisualization.ClearVisuals();
                else
                    rangeVisualization.ResetVisuals();
            }

            Dictionary<IHighlightable, HighlightType> previouslyHighlighted = currentlyHighlighted_;
            if (highlightProvider != null)
            {
                currentlyHighlighted_ = highlightProvider.GetHighlights().ToDictionary(p => p.Item1, p => p.Item2);
                rangeVisualization.UpdateVisuals(selection.hoverTilePosition ?? Vector2.zero);
            }
            else
            {
                currentlyHighlighted_ = new();
            }

            ApplyHover();
            ApplyNewHighlights(previouslyHighlighted);
            ApplyExpiredHighlights(previouslyHighlighted);
        }

        void ApplyHover()
        {
            HighlightType highlightType = selection.placing == null || selection.placing.IsValid() ? HighlightType.Hovered : HighlightType.Negative;

            var hovered = GetHovered();
            if (hovered == null)
            {
                if (selection.placing != null)
                    currentlyHighlighted_[pointHighlight] = highlightType;
            }
            else
            {
                if (selection.placing != null && !selection.placing.IsCorrectTypeSelected())
                    currentlyHighlighted_[pointHighlight] = highlightType;
                else
                    currentlyHighlighted_[hovered] = highlightType;
            }
        }

        IHighlightable GetHovered()
        {
            if (selection.hovered == null)
                return null;

            if (selection.hovered.attacker != null)
                return selection.hovered.attacker;
            return selection.hovered.tile;
        }

        void UpdateHighlightProvider(out bool changed)
        {
            MonoBehaviour selected = null;
            if (selection.selected != null)
                selected = selection.selected;
            else if (selection.placing != null)
                selected = selection.placing;
            else if (selection.hovered != null)
                selected = selection.hovered;

            HighlightProvider newHighlightProvider = selected?.gameObject.GetComponent<HighlightProvider>();
            changed = newHighlightProvider != highlightProvider;
            highlightProvider = newHighlightProvider;
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

        void ApplyNewHighlights(Dictionary<IHighlightable, HighlightType> previouslyHighlighted)
        {
            foreach (var (element, highlight) in currentlyHighlighted_.ToArray())
            {
                if (highlight == HighlightType.Clear)
                {
                    currentlyHighlighted_.Remove(element);
                    continue;
                }

                if (previouslyHighlighted.TryGetValue(element, out var oldHighlight) && highlight == oldHighlight)
                    continue;

                element.Highlight(highlightColors[(int)highlight]);
            }
        }

        void ApplyExpiredHighlights(Dictionary<IHighlightable, HighlightType> previouslyHighlighted)
        {
            foreach (var (element, _) in previouslyHighlighted)
            {
                if (currentlyHighlighted_.ContainsKey(element))
                    continue;
                if (element as Object != null)
                    element.Unhighlight();
            }
        }
    }
}

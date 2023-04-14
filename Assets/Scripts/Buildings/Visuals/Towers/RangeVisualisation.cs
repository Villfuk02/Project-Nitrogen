using Assets.Scripts.Attackers.Simulation;
using Assets.Scripts.Buildings.Simulation.Towers.Targetting;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.World.WorldData.WorldData;

namespace Assets.Scripts.Buildings.Visuals.Towers
{
    public class RangeVisualisation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject lineRendererPrefab;
        [Header("Settings")]
        [SerializeField] int pathSteps;
        [SerializeField] float maxOutlineStepSize;
        [SerializeField] float cliffThreshold;
        [SerializeField] Color outlineColor;
        [SerializeField] Color invalidColor;
        [SerializeField] Color targettableColor;
        [SerializeField] Color targettableTallColor;
        [SerializeField] Color blockedColor;
        [Header("Runtime values")]
        [SerializeField] Targetting targetting;
        [SerializeField] List<LineRenderer> lineRenderers;
        [SerializeField] bool valid;
        [SerializeField] int lines;
        Color currentColor;
        List<Vector3> pointAccumulator = new();

        private void Update()
        {
            if (targetting == null)
                return;
            if (WORLD_DATA != null && WORLD_DATA.pathStarts != null && WORLD_DATA.tiles != null)
            {
                lines = 0;
                pointAccumulator.Clear();

                DrawOutlines();
                DrawPaths();

                HideUnusedLines();
            }
        }

        void DrawOutlines()
        {
            var outline = targetting.GetRangeOutline();
            for (int l = 0; l < outline.Count; l++)
            {
                var line = outline[l];
                Vector2? lastSourcePos = null;
                float? lastHeight = null;
                while (line.MoveNext())
                {
                    Vector2 current = line.Current;
                    if (!lastSourcePos.HasValue)
                        lastSourcePos = current;
                    float dist = (current - lastSourcePos.Value).magnitude;
                    int subdivisions = Mathf.CeilToInt(dist / maxOutlineStepSize);
                    if (subdivisions == 0) subdivisions = 1;
                    for (int i = 1; i <= subdivisions; i++)
                    {
                        Vector2 sourcePos = Vector2.LerpUnclamped(lastSourcePos.Value, current, i / (float)subdivisions);
                        Vector2 offset = WorldUtils.WorldToTilePos(targetting.transform.position);
                        float? height = WORLD_DATA.tiles.GetHeightAt(sourcePos + offset);
                        if (height.HasValue)
                        {
                            if (lastHeight.HasValue && Mathf.Abs(lastHeight.Value - height.Value) > cliffThreshold)
                                EndLine();
                            Vector3 pos = WorldUtils.TileToWorldPos(sourcePos + offset, height.Value) + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
                            Color c = valid && targetting.IsInBounds(pos) ? outlineColor : invalidColor;
                            AddPoint(pos, c);
                        }
                        else
                        {
                            EndLine();
                        }
                        lastHeight = height;
                    }
                    lastSourcePos = current;
                }
                EndLine();
            }
        }

        void DrawPaths()
        {
            foreach (var tile in WORLD_DATA.tiles)
            {
                foreach (var next in tile.pathNext)
                {
                    DrawPathSegment(tile.pos, next.pos);
                }
            }
            for (int i = 0; i < WORLD_DATA.pathStarts.Length; i++)
            {
                DrawPathSegment(WORLD_DATA.pathStarts[i], WORLD_DATA.firstPathNodes[i]);
            }
        }

        private void DrawPathSegment(Vector2Int from, Vector2Int to)
        {
            for (int i = 0; i < pathSteps; i++)
            {
                float f = i / (float)(pathSteps - 1);
                Vector2 interpolated = Vector2.Lerp(from, to, f);
                float height = WORLD_DATA.tiles.GetHeightAt(interpolated) ?? WORLD_DATA.tiles.GetHeightAt(to) ?? -3;
                Vector3 pos = WorldUtils.TileToWorldPos(interpolated.x, interpolated.y, height);
                if (targetting.IsInBounds(pos))
                {
                    Vector3 smallPos = pos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
                    Vector3 largePos = pos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
                    if (targetting.IsValidTargetPosition(smallPos))
                        AddPoint(smallPos, targettableColor);
                    else if (targetting.IsValidTargetPosition(largePos))
                        AddPoint(smallPos, targettableTallColor);
                    else
                        AddPoint(smallPos, blockedColor);
                }
                else
                {
                    EndLine();
                }
            }
            EndLine();
        }

        private void AddPoint(Vector3 point, Color color)
        {
            if (pointAccumulator.Count == 0)
            {
                pointAccumulator.Add(point);
                currentColor = color;
            }
            else if (currentColor == color)
            {
                pointAccumulator.Add(point);
            }
            else
            {
                EndLine();
                AddPoint(point, color);
            }
        }

        private void EndLine()
        {
            if (pointAccumulator.Count == 0)
                return;
            if (pointAccumulator.Count == 1)
                pointAccumulator.Add(pointAccumulator[0]);

            if (lines == lineRenderers.Count)
            {
                lineRenderers.Add(Instantiate(lineRendererPrefab, transform).GetComponent<LineRenderer>());
            }
            var line = lineRenderers[lines];
            line.enabled = true;
            line.positionCount = pointAccumulator.Count;
            line.SetPositions(pointAccumulator.ToArray());
            line.material.color = currentColor;

            lines++;
            pointAccumulator.Clear();
        }

        private void HideUnusedLines()
        {
            for (int i = lines; i < lineRenderers.Count; i++)
            {
                lineRenderers[i].enabled = false;
            }
        }
    }
}


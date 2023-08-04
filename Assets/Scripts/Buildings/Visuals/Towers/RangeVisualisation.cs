
using Attackers.Simulation;
using Buildings.Simulation.Towers.Targeting;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using World.WorldData;

namespace Buildings.Visuals.Towers
{
    public class RangeVisualisation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] WorldData worldData;
        [SerializeField] World.World world;
        [SerializeField] GameObject lineRendererPrefab;
        [Header("Settings")]
        [SerializeField] int pathSteps;
        [SerializeField] float maxOutlineStepSize;
        [SerializeField] float cliffThreshold;
        [SerializeField] Color outlineColor;
        [SerializeField] Color invalidColor;
        [SerializeField] Color targetableColor;
        [SerializeField] Color targetableTallColor;
        [SerializeField] Color blockedColor;
        [Header("Runtime values")]
        [SerializeField] Targeting targeting;
        [SerializeField] List<LineRenderer> lineRenderers;
        [SerializeField] bool valid;
        [SerializeField] int lines;
        Color currentColor_;
        readonly List<Vector3> pointAccumulator_ = new();

        void Update()
        {
            if (targeting == null)
                return;
            if (!world.ready)
                return;

            lines = 0;
            pointAccumulator_.Clear();

            DrawOutlines();
            DrawPaths();

            HideUnusedLines();
        }

        void DrawOutlines()
        {
            var outline = targeting.GetRangeOutline();
            foreach (var line in outline)
            {
                Vector2? lastSourcePos = null;
                float? lastHeight = null;
                while (line.MoveNext())
                {
                    Vector2 current = line.Current;
                    lastSourcePos ??= current;
                    float dist = (current - lastSourcePos.Value).magnitude;
                    int subdivisions = Mathf.CeilToInt(dist / maxOutlineStepSize);
                    if (subdivisions == 0) subdivisions = 1;
                    for (int i = 1; i <= subdivisions; i++)
                    {
                        Vector2 sourcePos = Vector2.LerpUnclamped(lastSourcePos.Value, current, i / (float)subdivisions);
                        Vector2 offset = WorldUtils.WorldPosToTilePos(targeting.transform.position);
                        float? height = worldData.tiles.GetHeightAt(sourcePos + offset);
                        if (height.HasValue)
                        {
                            if (lastHeight.HasValue && Mathf.Abs(lastHeight.Value - height.Value) > cliffThreshold)
                                EndLine();
                            Vector3 pos = WorldUtils.TilePosToWorldPos(sourcePos + offset, height.Value) + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
                            Color c = valid && targeting.IsInBounds(pos) ? outlineColor : invalidColor;
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
            foreach (var tile in worldData.tiles)
            {
                foreach (var next in tile.pathNext)
                {
                    DrawPathSegment(tile.pos, next.pos);
                }
            }
            for (int i = 0; i < worldData.pathStarts.Length; i++)
            {
                DrawPathSegment(worldData.pathStarts[i], worldData.firstPathTiles[i]);
            }
        }

        void DrawPathSegment(Vector2Int from, Vector2Int to)
        {
            for (int i = 0; i < pathSteps; i++)
            {
                float f = i / (float)(pathSteps - 1);
                Vector2 interpolated = Vector2.Lerp(from, to, f);
                float height = worldData.tiles.GetHeightAt(interpolated) ?? worldData.tiles.GetHeightAt(to)!.Value;
                Vector3 pos = WorldUtils.TilePosToWorldPos(interpolated.x, interpolated.y, height);
                if (targeting.IsInBounds(pos))
                {
                    Vector3 smallPos = pos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
                    Vector3 largePos = pos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
                    if (targeting.IsValidTargetPosition(smallPos))
                        AddPoint(smallPos, targetableColor);
                    else if (targeting.IsValidTargetPosition(largePos))
                        AddPoint(smallPos, targetableTallColor);
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

        void AddPoint(Vector3 point, Color color)
        {
            if (pointAccumulator_.Count == 0)
            {
                pointAccumulator_.Add(point);
                currentColor_ = color;
            }
            else if (currentColor_ == color)
            {
                pointAccumulator_.Add(point);
            }
            else
            {
                EndLine();
                AddPoint(point, color);
            }
        }

        void EndLine()
        {
            if (pointAccumulator_.Count == 0)
                return;
            if (pointAccumulator_.Count == 1)
                pointAccumulator_.Add(pointAccumulator_[0]);

            if (lines == lineRenderers.Count)
            {
                lineRenderers.Add(Instantiate(lineRendererPrefab, transform).GetComponent<LineRenderer>());
            }
            var line = lineRenderers[lines];
            line.enabled = true;
            line.positionCount = pointAccumulator_.Count;
            line.SetPositions(pointAccumulator_.ToArray());
            line.material.color = currentColor_;

            lines++;
            pointAccumulator_.Clear();
        }

        void HideUnusedLines()
        {
            for (int i = lines; i < lineRenderers.Count; i++)
            {
                lineRenderers[i].enabled = false;
            }
        }
    }
}


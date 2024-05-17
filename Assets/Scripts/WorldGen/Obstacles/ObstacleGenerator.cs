
using Data.WorldGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;
using static WorldGen.WorldGenerator;

namespace WorldGen.Obstacles
{
    public class ObstacleGenerator : MonoBehaviour
    {
        RandomSet<Vector2Int> tilesLeft_;
        Array2D<float>[] weightFields_;
        List<Vector2Int> emptyTiles_;
        /// <summary>
        /// Places all the various obstacles on tiles according to their parameters and ensuring the shortest path from each path start is the specified length.
        /// </summary>
        public void PlaceObstacles(Vector2Int[] pathStarts, int[] pathLengths, Vector2Int hubPosition)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Picking Obstacles");
            // draw paths starts and hub position
            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(pathStarts) { hubPosition }.Select(p => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(p), 0.5f)));
            // end debug

            var layers = WorldGenerator.TerrainType.Obstacles.Layers;
            weightFields_ = new Array2D<float>[layers.Length];
            weightFields_.Fill(() => new(WorldUtils.WORLD_SIZE));

            emptyTiles_ = new();
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
            {
                if (Tiles[v].dist == int.MaxValue)
                    emptyTiles_.Add(v);
            }
            // first generate obstacles on random tiles that don't have a planned path going through them
            for (var layer = 0; layer < layers.Length; layer++)
            {
                // debug
                DrawGizmos(StepType.Step);
                WaitForStep(StepType.Step);
                // end debug

                GenerateLayer(layers[layer], layer);
            }
            // debug
            DrawGizmos(StepType.Step);
            WaitForStep(StepType.Step);
            // end debug

            // then mark all tiles as impassable and remove them in a random order, keeping only those that would allow for a shorter path than intended from any start
            EnsurePathLengths(pathStarts, pathLengths, hubPosition);

            Tiles.RecalculateDistances(hubPosition);

            // debug
            DrawGizmos(StepType.Phase);
            print("Obstacles Picked");
            // end debug
        }

        /// <summary>
        /// Places all obstacles of the given layer, ensuring a valid path still exists from each start.
        /// </summary>
        void GenerateLayer(IEnumerable<ObstacleData> layer, int index)
        {
            var obstacleCounts = layer.Where(o => o.Max > 0).ToDictionary(o => o, _ => 0);
            while (obstacleCounts.Count > 0)
            {
                tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
                while (tilesLeft_.Count > 0)
                {
                    // debug
                    DrawGizmos(StepType.MicroStep);
                    WaitForStep(StepType.MicroStep);
                    // end debug

                    Vector2Int tile = tilesLeft_.PopRandom();
                    TryPlace(tile, obstacleCounts, index);
                }

                if (obstacleCounts.All(p => p.Value >= p.Key.Min))
                    break;
            }
        }

        /// <summary>
        /// Tries to place an obstacle at the given tile.
        /// </summary>
        void TryPlace(Vector2Int pos, Dictionary<ObstacleData, int> available, int layer)
        {
            var placeable = GetValidPlacements(pos, available.Keys, false);
            if (placeable.Count == 0)
                return;

            var obstacle = placeable.PopRandom();
            if (++available[obstacle] >= obstacle.Max)
                available.Remove(obstacle);
            Place(pos, obstacle, layer);
        }

        /// <summary>
        /// Fills tiles with virtual obstacles to make sure the shortest paths to the hub have the specified length.
        /// Fills all tiles and one by one removes each virtual obstacle, unless that would allow for a shorter path than specified.
        /// </summary>
        void EnsurePathLengths(Vector2Int[] pathStarts, int[] pathLengths, Vector2Int hubPosition)
        {
            tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
            foreach (var pos in tilesLeft_)
                Tiles[pos].passable = false;

            while (tilesLeft_.Count > 0)
            {
                // debug
                DrawGizmos(StepType.MicroStep);
                WaitForStep(StepType.MicroStep);
                // end debug

                Vector2Int pos = tilesLeft_.PopRandom();
                Tiles[pos].passable = true;
                Tiles.RecalculateDistances(hubPosition);
                bool mustKeep = Enumerable.Range(0, pathStarts.Length).Any(i => Tiles[pathStarts[i]].dist != pathLengths[i]);
                if (!mustKeep)
                    continue;

                Place(pos, null);
            }
        }

        /// <summary>
        /// Checks whether each obstacle can be placed on this tile and returns the set along with their probabilities.
        /// If forcePlace is true, all valid obstacles can be placed regardless of their chance.
        /// </summary>
        WeightedRandomSet<ObstacleData> GetValidPlacements(Vector2Int pos, IEnumerable<ObstacleData> available, bool forcePlace)
        {
            WeightedRandomSet<ObstacleData> placeable = new(WorldGenerator.Random.NewSeed());
            foreach (var o in available)
            {
                float probability = o.BaseProbability;
                if (!o.OnSlants && Tiles[pos].slant != WorldUtils.Slant.None)
                    continue;

                for (int layer = 0; layer < o.Forces.Length; layer++)
                {
                    probability += o.Forces[layer] * weightFields_[layer][pos];
                }

                probability = Mathf.Clamp01(probability);
                if (forcePlace)
                    placeable.AddOrUpdate(o, probability + 0.01f);
                else if (WorldGenerator.Random.Bool(probability))
                    placeable.AddOrUpdate(o, probability);
            }

            return placeable;
        }

        /// <summary>
        /// Actually place the obstacle at the given tile and update all the relevant fields.
        /// </summary>
        void Place(Vector2Int pos, ObstacleData? obstacle, int layer = -1)
        {
            emptyTiles_.Remove(pos);
            Tiles[pos].passable = false;
            Tiles[pos].obstacle = obstacle;
            if (layer == -1)
                return;
            foreach (var p in emptyTiles_)
            {
                Vector2Int dist = p - pos;
                weightFields_[layer][p] += 1f / dist.sqrMagnitude;
            }
        }

        void DrawGizmos(StepType duration)
        {
            if (tilesLeft_ != null)
                RegisterGizmos(duration, () => tilesLeft_.Select(pos => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.3f)));
            RegisterGizmos(duration, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TilePosToWorldPos(tile.pos);
                    Color c = !tile.passable ? Color.magenta : (tile.dist == int.MaxValue ? Color.red : Color.green);
                    gizmos.Add(new GizmoManager.Cube(c, pos, 0.2f));
                    gizmos.AddRange(
                        tile.neighbors.Where(n => n is not null).Select(n => WorldUtils.TilePosToWorldPos(n.pos))
                            .Select(other => new GizmoManager.Line(c, pos, Vector3.Lerp(pos, other, 0.5f)))
                    );
                }
                return gizmos;
            });
        }
    }
}

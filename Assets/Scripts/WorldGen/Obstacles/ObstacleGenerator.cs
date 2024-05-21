using System.Collections.Generic;
using System.Linq;
using Data.WorldGen;
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
        /// Places all the various obstacles on tiles according to their parameters.
        /// Makes sure to not block paths.
        /// </summary>
        public void PlaceObstacles()
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Picking Obstacles");
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

            for (var layer = 0; layer < layers.Length; layer++)
            {
                // debug
                DrawGizmos(StepType.Step);
                WaitForStep(StepType.Step);
                // end debug

                GenerateLayer(layers[layer], layer);
            }

            // debug
            DrawGizmos(StepType.Phase);
            print("Obstacles Picked");
            // end debug
        }

        /// <summary>
        /// Places all obstacles of the given layer randomly, based on their parameters.
        /// </summary>
        void GenerateLayer(IEnumerable<ObstacleData> layer, int index)
        {
            // keep track of how many obstacles were placed of each type, but only keep those that have not reached the maximum count
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
        /// Tries to place an obstacle at the given tile, selected from the given dictionary.
        /// Removes the obstacle entry from the dictionary if the max count is reached.
        /// </summary>
        void TryPlace(Vector2Int pos, Dictionary<ObstacleData, int> available, int layer)
        {
            var placeable = GetValidPlacements(pos, available.Keys);
            if (placeable.Count == 0)
                return;

            var obstacle = placeable.PopRandom();
            if (++available[obstacle] >= obstacle.Max)
                available.Remove(obstacle);
            Place(pos, obstacle, layer);
        }

        /// <summary>
        /// For each obstacle, calculates its probability to be placed on this tile, and adds it to the resulting set with the calculated probability.
        /// </summary>
        WeightedRandomSet<ObstacleData> GetValidPlacements(Vector2Int pos, IEnumerable<ObstacleData> available)
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
                if (WorldGenerator.Random.Bool(probability))
                    placeable.AddOrUpdate(o, probability);
            }

            return placeable;
        }

        /// <summary>
        /// Actually place the obstacle at the given tile and update all the relevant fields.
        /// </summary>
        void Place(Vector2Int pos, ObstacleData obstacle, int layer)
        {
            emptyTiles_.Remove(pos);
            Tiles[pos].passable = false;
            Tiles[pos].obstacle = obstacle;
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
                    Color c = tile.passable ? tile.dist == int.MaxValue ? Color.red : Color.green : Color.magenta;
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
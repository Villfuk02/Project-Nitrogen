
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
        public void PlaceObstacles(Vector2Int[] pathStarts, int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            print("Picking Obstacles");

            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(pathStarts) { WorldUtils.WORLD_CENTER }.Select(p => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(p), 0.5f)));

            var layers = WorldGenerator.TerrainType.Obstacles.Layers;
            var fillers = WorldGenerator.TerrainType.Obstacles.Fillers;
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
                DrawGizmos(StepType.Step);
                WaitForStep(StepType.Step);
                GenerateLayer(layers[layer], layer);
            }
            DrawGizmos(StepType.Step);
            WaitForStep(StepType.Step);

            // then fill all tiles with the filler obstacle and remove them in a random order, keeping only those that would allow for a shorter path than intended from any start
            EnsurePathLengths(pathStarts, pathLengths, fillers);

            Tiles.RecalculateDistances();
            DrawGizmos(StepType.Phase);
            print("Obstacles Picked");
        }

        void EnsurePathLengths(Vector2Int[] pathStarts, int[] pathLengths, ObstacleData[] fillers)
        {
            tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
            foreach (var pos in tilesLeft_)
                Tiles[pos].passable = false;

            while (tilesLeft_.Count > 0)
            {
                DrawGizmos(StepType.MicroStep);
                WaitForStep(StepType.MicroStep);
                Vector2Int pos = tilesLeft_.PopRandom();
                Tiles[pos].passable = true;
                Tiles.RecalculateDistances();
                bool mustKeep = Enumerable.Range(0, pathStarts.Length).Any(i => Tiles[pathStarts[i]].dist != pathLengths[i]);
                if (!mustKeep)
                    continue;

                var placeable = TryPlaceAll(pos, fillers, true);
                var obstacle = placeable.PopRandom();
                Place(pos, obstacle);
            }
        }

        void GenerateLayer(IEnumerable<ObstacleData> layer, int index)
        {
            var obstacleCounts = layer.Where(o => o.Max > 0).ToDictionary(o => o, _ => 0);
            while (obstacleCounts.Count > 0)
            {
                tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
                while (tilesLeft_.Count > 0)
                {
                    DrawGizmos(StepType.MicroStep);
                    WaitForStep(StepType.MicroStep);
                    Vector2Int tile = tilesLeft_.PopRandom();
                    TryPlace(tile, obstacleCounts, index);
                }

                if (obstacleCounts.All(p => p.Value >= p.Key.Min))
                    break;
            }
        }

        void TryPlace(Vector2Int pos, Dictionary<ObstacleData, int> available, int layer)
        {
            var placeable = TryPlaceAll(pos, available.Keys, false);
            if (placeable.Count == 0)
                return;

            var obstacle = placeable.PopRandom();
            if (++available[obstacle] >= obstacle.Max)
                available.Remove(obstacle);
            Place(pos, obstacle, layer);
        }

        WeightedRandomSet<ObstacleData> TryPlaceAll(Vector2Int pos, IEnumerable<ObstacleData> available, bool forcePlace)
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
                    placeable.Add(o, probability + 0.01f);
                else if (WorldGenerator.Random.Bool(probability))
                    placeable.Add(o, probability);
            }

            return placeable;
        }

        void Place(Vector2Int pos, ObstacleData obstacle, int layer = -1)
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

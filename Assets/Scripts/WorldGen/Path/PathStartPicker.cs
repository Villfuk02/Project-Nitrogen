using Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathStartPicker : MonoBehaviour
    {
        [SerializeField] float startSpacingMultiplier;

        public Vector2Int[] PickStarts(int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Picking Starts");

            RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(WorldUtils.ORIGIN), 0.4f));

            GetPossibleStarts(out var oddStarts, out var evenStarts);

            int pathCount = pathLengths.Length;
            int perimeter = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) * 2;
            float minDistSqr = perimeter * startSpacingMultiplier / pathCount;
            minDistSqr *= minDistSqr;
            List<Vector2Int> picked = new();
            for (int i = 0; i < pathCount; i++)
            {
                RegisterGizmos(StepType.MicroStep, () => oddStarts.AllEntries.Concat(evenStarts.AllEntries)
                    .Where(t => picked.All(u => (t - u).sqrMagnitude >= minDistSqr))
                    .Select(t => new GizmoManager.Cube(Color.green, WorldUtils.TileToWorldPos(t), 0.3f)));
                WaitForStep(StepType.MicroStep);

                var tp = ChooseStart(pathLengths[i], minDistSqr, pathLengths[i] % 2 == 0 ? evenStarts : oddStarts, picked);
                picked.Add(tp);

                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(tp), 0.4f));
            }
            return picked.ToArray();
        }

        static void GetPossibleStarts(out RandomSet<Vector2Int> oddStarts, out RandomSet<Vector2Int> evenStart)
        {
            oddStarts = new(WorldGenerator.Random.NewSeed());
            evenStart = new(WorldGenerator.Random.NewSeed());
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
            {
                if (v.x > 0 && v.x < WorldUtils.WORLD_SIZE.x - 1 && v.y > 0 && v.y < WorldUtils.WORLD_SIZE.y - 1)
                    continue;
                if ((v.x + v.y - WorldUtils.ORIGIN.x - WorldUtils.ORIGIN.y) % 2 == 0)
                    evenStart.Add(v);
                else
                    oddStarts.Add(v);
            }
        }

        static Vector2Int ChooseStart(int length, float minDistSqr, RandomSet<Vector2Int> available, IReadOnlyCollection<Vector2Int> currentlyPicked)
        {
            Vector2Int result;
            List<Vector2Int> tooFar = new();
            while (true)
            {
                result = available.PopRandom();

                RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(result), 0.2f));

                if (result.ManhattanDistance(WorldUtils.ORIGIN) > length)
                {
                    tooFar.Add(result);
                    continue;
                }

                if (currentlyPicked.All(t => (result - t).sqrMagnitude >= minDistSqr))
                    break;
            }
            available.AddRange(tooFar);
            return result;
        }
    }
}

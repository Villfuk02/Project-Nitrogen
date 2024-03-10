using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathStartPicker : MonoBehaviour
    {
        [SerializeField] float startSpacingMultiplier;

        /// <summary>
        /// Picks out a starting tile for each path.
        /// </summary>
        public Vector2Int[] PickStarts(int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            print("Picking Starts");

            RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(WorldUtils.WORLD_CENTER), 0.4f));

            GetPossibleStarts(out var oddStarts, out var evenStarts);

            // prepare variables
            int pathCount = pathLengths.Length;
            int perimeter = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) * 2;
            float minDistSqr = perimeter * startSpacingMultiplier / pathCount;
            minDistSqr *= minDistSqr;
            var picked = new List<Vector2Int>();
            // pick a start for each path
            for (int i = 0; i < pathCount; i++)
            {
                RegisterGizmos(StepType.MicroStep, () => oddStarts.Concat(evenStarts)
                    .Where(t => picked.All(u => (t - u).sqrMagnitude >= minDistSqr))
                    .Select(t => new GizmoManager.Cube(Color.green, WorldUtils.TilePosToWorldPos(t), 0.3f)));
                WaitForStep(StepType.MicroStep);

                var tp = ChooseStart(pathLengths[i], minDistSqr, pathLengths[i] % 2 == 0 ? evenStarts : oddStarts, picked);
                picked.Add(tp);

                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(tp), 0.4f));
            }
            return picked.ToArray();
        }
        /// <summary>
        /// Selects the possible path starting tiles - those at the edge of the world. Due to parity, paths of odd length cannot start at the same spots as paths of even length.
        /// </summary>
        static void GetPossibleStarts(out RandomSet<Vector2Int> oddStarts, out RandomSet<Vector2Int> evenStart)
        {
            oddStarts = new(WorldGenerator.Random.NewSeed());
            evenStart = new(WorldGenerator.Random.NewSeed());
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
            {
                if (v.x > 0 && v.x < WorldUtils.WORLD_SIZE.x - 1 && v.y > 0 && v.y < WorldUtils.WORLD_SIZE.y - 1)
                    continue;
                if ((v.x + v.y - WorldUtils.WORLD_CENTER.x - WorldUtils.WORLD_CENTER.y) % 2 == 0)
                    evenStart.Add(v);
                else
                    oddStarts.Add(v);
            }
        }

        /// <summary>
        /// Pick a start position for the given path.
        /// </summary>
        /// <param name="length">Length of the path.</param>
        /// <param name="minDistSqr">The square of the minimum distance to all other starts.</param>
        /// <param name="available">Available positions for the start.</param>
        /// <param name="currentlyPicked">Already picked starts.</param>
        static Vector2Int ChooseStart(int length, float minDistSqr, RandomSet<Vector2Int> available, IReadOnlyCollection<Vector2Int> currentlyPicked)
        {
            Vector2Int result;
            var tooFar = new List<Vector2Int>();
            // rejection sampling
            while (true)
            {
                result = available.PopRandom();

                RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(result), 0.2f));

                // if the candidate is too far from the center, reject it, but don't forget to add it back into the set of available starts
                if (result.ManhattanDistance(WorldUtils.WORLD_CENTER) > length)
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

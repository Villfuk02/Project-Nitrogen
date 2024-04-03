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
        [Header("Settings")]
        [SerializeField] float startSpacingMultiplier;

        [Header("runtime variables")]
        float minDistanceSquared_;
        readonly List<Vector2Int> pickedStarts_ = new();
        RandomSet<Vector2Int> oddLengthCandidates_;
        RandomSet<Vector2Int> evenLengthCandidates_;

        /// <summary>
        /// Picks out a starting tile for each path based on path count and lengths.
        /// </summary>
        public Vector2Int[] PickStarts(int[] pathLengths)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Picking Starts");
            // draw the center of the world
            RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(WorldUtils.WORLD_CENTER), 0.4f));
            // end debug

            pickedStarts_.Clear();

            GenerateCandidates();

            int pathCount = pathLengths.Length;
            int worldPerimeter = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) * 2;
            float minDist = worldPerimeter * startSpacingMultiplier / pathCount;
            minDistanceSquared_ = minDist * minDist;

            for (int i = 0; i < pathCount; i++)
            {
                // debug
                // draw all valid starts for this path
                RegisterGizmos(StepType.MicroStep, () => oddLengthCandidates_.Concat(evenLengthCandidates_)
                    .Where(t => pickedStarts_.All(u => (t - u).sqrMagnitude >= minDistanceSquared_))
                    .Select(t => new GizmoManager.Cube(Color.green, WorldUtils.TilePosToWorldPos(t), 0.3f)));
                WaitForStep(StepType.MicroStep);
                // end debug

                var startPosition = PickStart(pathLengths[i]);
                pickedStarts_.Add(startPosition);

                // debug
                // draw the picked start position
                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(startPosition), 0.4f));
                // end debug
            }
            return pickedStarts_.ToArray();
        }

        /// <summary>
        /// Selects the possible path starting tiles - those at the edge of the world.
        /// Due to parity, paths of odd length cannot start at the same spots as paths of even length.
        /// </summary>
        void GenerateCandidates()
        {
            oddLengthCandidates_ = new(WorldGenerator.Random.NewSeed());
            evenLengthCandidates_ = new(WorldGenerator.Random.NewSeed());
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                AddCandidate(new(x, 0));
                AddCandidate(new(x, WorldUtils.WORLD_SIZE.y - 1));
            }
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                AddCandidate(new(0, y));
                AddCandidate(new(WorldUtils.WORLD_SIZE.x - 1, y));
            }
        }
        /// <summary>
        /// Add the given position to the right candidates set based on the position's parity
        /// </summary>
        void AddCandidate(Vector2Int position)
        {
            if ((position.x + position.y - WorldUtils.WORLD_CENTER.x - WorldUtils.WORLD_CENTER.y) % 2 == 0)
                evenLengthCandidates_.Add(position);
            else
                oddLengthCandidates_.Add(position);
        }


        /// <summary>
        /// Pick a start position for a path of the given length.
        /// </summary>
        Vector2Int PickStart(int length)
        {
            var candidates = length % 2 == 0 ? evenLengthCandidates_ : oddLengthCandidates_;
            Vector2Int result;
            var tooFar = new List<Vector2Int>();

            // rejection sampling
            while (true)
            {
                result = candidates.PopRandom();

                // debug
                // draw the considered position
                RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(result), 0.2f));
                // end debug

                // if the candidate is too far from the center, reject it, but don't forget to add it back into the set of available starts
                if (result.ManhattanDistance(WorldUtils.WORLD_CENTER) > length)
                {
                    tooFar.Add(result);
                    continue;
                }

                if (pickedStarts_.All(t => (result - t).sqrMagnitude >= minDistanceSquared_))
                    break;
            }
            candidates.AddRange(tooFar);
            return result;
        }
    }
}

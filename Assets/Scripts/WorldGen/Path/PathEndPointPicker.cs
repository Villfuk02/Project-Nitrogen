using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathEndPointPicker : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float startSpacingMultiplier;

        [Header("Runtime variables")]
        float minDistanceSquared_;
        readonly List<Vector2Int> pickedStarts_ = new();
        RandomSet<Vector2Int> oddLengthCandidates_;
        RandomSet<Vector2Int> evenLengthCandidates_;
        Vector2Int hubPosition_;

        /// <summary>
        /// Picks out a tile for the hub and a starting tile for each path based on path count and lengths.
        /// </summary>
        public void PickEndPoints(float maxHubDistFromCenter, int[] pathLengths, out Vector2Int hubPos, out Vector2Int[] pathStarts)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Picking End Points");
            // end debug

            hubPosition_ = GenerateHubPos(maxHubDistFromCenter);
            hubPos = hubPosition_;

            // draw the hub position
            RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(hubPosition_), 0.4f));
            // end debug

            pickedStarts_.Clear();

            int worldPerimeter = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) * 2;

            GenerateCandidates(pathLengths.Length == 1 ? Mathf.Min(pathLengths[0] - 2, worldPerimeter / 8f) : 2);

            int pathCount = pathLengths.Length;
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

            pathStarts = pickedStarts_.ToArray();
        }

        /// <summary>
        /// Picks the hub position uniformly from all tiles that are at most maxHubDistFromCenter from center.
        /// </summary>
        static Vector2Int GenerateHubPos(float maxHubDistFromCenter)
        {
            List<Vector2Int> validPositions = new();
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                if ((pos - WorldUtils.WORLD_CENTER).sqrMagnitude <= maxHubDistFromCenter * maxHubDistFromCenter)
                    validPositions.Add(pos);
            }

            return validPositions[WorldGenerator.Random.Int(validPositions.Count)];
        }

        /// <summary>
        /// Selects the possible path starting tiles - those at the edge of the world.
        /// Due to parity, paths of odd length cannot start at the same spots as paths of even length.
        /// Discards those that are closer to hub than minDistFromHub.
        /// </summary>
        void GenerateCandidates(float minDistFromHub)
        {
            oddLengthCandidates_ = new(WorldGenerator.Random.NewSeed());
            evenLengthCandidates_ = new(WorldGenerator.Random.NewSeed());
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                AddCandidateIfFarEnoughFromHub(new(x, 0), minDistFromHub);
                AddCandidateIfFarEnoughFromHub(new(x, WorldUtils.WORLD_SIZE.y - 1), minDistFromHub);
            }

            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                AddCandidateIfFarEnoughFromHub(new(0, y), minDistFromHub);
                AddCandidateIfFarEnoughFromHub(new(WorldUtils.WORLD_SIZE.x - 1, y), minDistFromHub);
            }
        }

        /// <summary>
        /// Add the given position to the right candidates set based on the position's parity, but only if it's further than minDistFromHub from the hub.
        /// </summary>
        void AddCandidateIfFarEnoughFromHub(Vector2Int position, float minDistFromHub)
        {
            if ((position - hubPosition_).sqrMagnitude < minDistFromHub * minDistFromHub)
                return;
            if ((position.x + position.y - hubPosition_.x - hubPosition_.y) % 2 == 0)
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
                if (result.ManhattanDistance(hubPosition_) > length)
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
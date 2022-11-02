using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Blockers;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Path;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Utils;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC;
using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.World.World;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Debug Stepping")]
        [SerializeField] bool stepped;
        [SerializeField] StepType stepType;
        [Header("References")]
        public static LevelGenerator inst;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator WFC;
        [SerializeField] BlockerGenerator blockerGenerator;
        [SerializeField] Scatterer.Scatterer scatterer;
        [SerializeField] WorldBuilder wb;
        //[Header("Settings")]
        [Header("Runtime Values")]
        LevelGenTiles tiles;
        public static LevelGenTiles Tiles { get => inst.tiles; }
        public readonly static StepType[] STEP_TYPES = (StepType[])Enum.GetValues(typeof(StepType));
        public enum StepType { None, Phase, Step, Substep }

        private void Awake()
        {
            if (inst == null)
                inst = this;
            else
                Debug.LogError("There can be only one!");
        }
        void Start()
        {
            StartCoroutine(Generate());
            StartCoroutine(Animate());
        }

        void Update()
        {
            if (stepType != StepType.None)
            {
                if (Input.GetKeyDown(KeyCode.T) || Input.GetKey(KeyCode.H))
                    stepped = false;

                if (Input.GetKeyDown(KeyCode.N))
                    stepType = StepType.None;
                else if (Input.GetKeyDown(KeyCode.P))
                    stepType = StepType.Phase;
                else if (Input.GetKeyDown(KeyCode.S))
                    stepType = StepType.Step;
                else if (Input.GetKeyDown(KeyCode.M))
                    stepType = StepType.Substep;
            }
        }

        IEnumerator Generate()
        {
            WORLD_DATA = new();
            WFC.Prepare();
            blockerGenerator.Prepare();
            scatterer.Prepare();
            wb.Begin();
            Vector2Int[] targets;
            do
            {
                JobDataInterface pickTargets = pathPlanner.PickTargets(out targets);
                yield return new WaitUntil(() => pickTargets.IsFinished);
                JobDataInterface pickPaths = pathPlanner.PickPaths(targets, out int[] nodes);
                yield return new WaitUntil(() => pickPaths.IsFinished);
                if (pickPaths.Failed)
                {
                    continue;
                }
                JobDataInterface WFCGenerate = WFC.Generate(nodes, out int[] modules, out int[] heights);
                yield return new WaitUntil(() => WFCGenerate.IsFinished);
                if (WFCGenerate.Failed)
                {
                    continue;
                }
                tiles = new(modules, heights, nodes);
                WORLD_DATA.tiles = tiles;
                int[,] modules2d = new int[WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1];
                int[,] heights2d = new int[WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1];
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        int index = x + y * (WorldUtils.WORLD_SIZE.x + 1);
                        modules2d[x, y] = modules[index];
                        heights2d[x, y] = heights[index];
                    }
                }
                WORLD_DATA.modules = modules2d;
                WORLD_DATA.moduleHeights = heights2d;
                break;
            } while (true);
            JobDataInterface placeBlockers = blockerGenerator.PlaceBlockers(targets, pathPlanner.targetLengths);
            yield return new WaitUntil(() => placeBlockers.IsFinished);
            JobDataInterface finalizePaths = pathPlanner.FinalisePaths(targets);
            yield return new WaitUntil(() => finalizePaths.IsFinished);
            JobDataInterface scatter = scatterer.Scatter(out List<int> typeCounts, out List<Vector2> positions, out List<float> scales);
            yield return new WaitUntil(() => scatter.IsFinished);
            WORLD_DATA.decorationPositions = new List<Vector2>[typeCounts.Count];
            WORLD_DATA.decorationScales = new List<float>[typeCounts.Count];
            int p = 0;
            for (int i = 0; i < typeCounts.Count; i++)
            {
                int t = typeCounts[i];
                WORLD_DATA.decorationPositions[i] = positions.GetRange(p, t);
                WORLD_DATA.decorationScales[i] = scales.GetRange(p, t);
                p += t;
            }
            Debug.Log("DONE");
            yield break;
        }
        IEnumerator Animate()
        {
            yield break;
        }

        public static void RegisterGizmos(StepType duration, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider)
        {
            if (duration <= inst.stepType)
            {
                inst.gizmos.Add(duration, objectProvider());
            }
        }
        public static void RegisterGizmos(StepType duration, Func<GizmoManager.GizmoObject> objectProvider)
        {
            if (duration <= inst.stepType)
            {
                inst.gizmos.Add(duration, objectProvider());
            }
        }
        public static void RegisterGizmosIfExactly(StepType duration, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider)
        {
            if (duration == inst.stepType)
            {
                inst.gizmos.Add(duration, objectProvider());
            }
        }

        public static bool CanStep(StepType type)
        {
            if (type <= inst.stepType)
            {
                if (inst.stepped)
                    return false;
                for (StepType t = type; t <= STEP_TYPES[^1]; t++)
                {
                    inst.gizmos.Expire(t);
                }
                inst.stepped = true;
            }
            return true;
        }

        public static void WaitForStep(StepType type)
        {
            while (!CanStep(type))
            {
                Thread.Sleep(15);
            }
        }
    }
}

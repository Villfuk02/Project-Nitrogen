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
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKey(KeyCode.M))
                stepped = false;
        }

        IEnumerator Generate()
        {

            WFC.Prepare();
            blockerGenerator.Prepare();
            scatterer.Prepare();
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
                break;
            } while (true);
            JobDataInterface placeBlockers = blockerGenerator.PlaceBlockers(targets, pathPlanner.targetLengths, out List<Vector2Int> blockerPositions, out List<int> blockerTypes);
            yield return new WaitUntil(() => placeBlockers.IsFinished);
            JobDataInterface finalizePaths = pathPlanner.FinalisePaths(targets);
            yield return new WaitUntil(() => finalizePaths.IsFinished);
            JobDataInterface scatter = scatterer.Scatter(out List<int> typeCounts, out List<Vector2> positions, out List<float> scales);
            yield return new WaitUntil(() => scatter.IsFinished);
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

using Data.WorldGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;
using WorldGen.Blockers;
using WorldGen.Path;
using WorldGen.Utils;
using WorldGen.WFC;

namespace WorldGen
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Debug Stepping")]
        [SerializeField] bool stepped;
        [SerializeField] StepType stepType;
        [Header("References")]
        static WorldGenerator inst_;
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] WorldSettings.WorldSettings worldSettings;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathStartPicker pathStartPicker;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator wfc;
        [SerializeField] BlockerGenerator blockerGenerator;
        [SerializeField] PathFinalizer pathFinalizer;
        //[SerializeField] Scatterer.Scatterer scatterer;
        [Header("Settings")]
        [SerializeField] int tries;

        //Runtime Values
        public static Random.Random Random { get; private set; }
        public static TerrainType TerrainType { get; private set; }
        public static WorldGenTiles Tiles { get; private set; }

        readonly AutoResetEvent waitForStepEvent_ = new(false);
        readonly object steppedLock_ = new();

        public enum StepType { None, Phase, Step, MicroStep }
        public static readonly StepType[] STEP_TYPES = (StepType[])Enum.GetValues(typeof(StepType));

        void Awake()
        {
            if (inst_ == null)
                inst_ = this;
            else
                Debug.LogError("There can be only one!");
            worldData.Clear();
            Random = new(worldSettings.seed);
            Tiles = null;
        }
        void Start()
        {
            StartCoroutine(Generate());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) || Input.GetKey(KeyCode.H))
                Step();

            // DON'T STOP ME NOOOOOW
            if (stepType == StepType.None)
                return;

            if (Input.GetKeyDown(KeyCode.N))
                stepType = StepType.None;
            else if (Input.GetKeyDown(KeyCode.P))
                stepType = StepType.Phase;
            else if (Input.GetKeyDown(KeyCode.S))
                stepType = StepType.Step;
            else if (Input.GetKeyDown(KeyCode.M))
                stepType = StepType.MicroStep;
        }

        IEnumerator Generate()
        {
            Task generating = GenerateAsync();
            yield return new WaitUntil(() => generating.IsCompleted);
            Debug.Log("GENERATING FINISHED");
        }

        async Task GenerateAsync()
        {
            worldData.Clear();

            TerrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            await Task.Yield();

            //blockerGenerator.Prepare();
            //scatterer.Prepare();

            Vector2Int[] starts;
            while (true)
            {
                if (tries <= 0)
                    throw new("Failed to generate world");
                tries--;

                starts = await Task.Run(() => pathStartPicker.PickStarts(worldSettings.pathLengths));

                var paths = await Task.Run(() => pathPlanner.PlanPaths(starts, worldSettings.pathLengths));
                if (paths is null)
                    continue;

                var terrain = await Task.Run(() => wfc.Generate(paths));
                if (terrain is null)
                    continue;

                Tiles = new(terrain.GetCollapsedSlots(), terrain.GetPassageAtTile, paths);
                break;
            }

            await Task.Run(() => blockerGenerator.PlaceBlockers(starts, worldSettings.pathLengths));
            await Task.Run(() => pathFinalizer.FinalizePaths(starts, worldSettings.maxExtraPaths));
            /*
            JobDataInterface finalizePaths = pathPlanner.FinalisePaths(targets);
            yield return new WaitUntil(() => finalizePaths.IsFinished);
            WORLD_DATA.firstPathNodes = targets;
            Vector2Int[] pathStarts = new Vector2Int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                pathStarts[i] = targets[i] + GetMainDir(ORIGIN, targets[i]);
            }
            WORLD_DATA.pathStarts = pathStarts;
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
            */
            Debug.Log("GENERATING SUCCESS");
        }

        void Step()
        {
            if (!stepped)
                return;
            lock (steppedLock_)
            {
                waitForStepEvent_.Set();
                stepped = false;
            }
        }

        void GizmoStep(StepType type)
        {
            for (StepType t = type; t <= STEP_TYPES[^1]; t++)
            {
                gizmos.Expire(t);
            }
        }
        void Stepped()
        {
            lock (steppedLock_)
            {
                stepped = true;
            }
        }
        public static void WaitForStep(StepType type)
        {
            if (type > inst_.stepType)
                return;

            if (inst_.stepped)
                inst_.waitForStepEvent_.WaitOne();
            inst_.GizmoStep(type);
            inst_.Stepped();
        }

        public static void RegisterGizmos(StepType duration, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider, object gizmoDuration = null)
        {
            if (duration <= inst_.stepType)
                inst_.gizmos.Add(gizmoDuration ?? duration, objectProvider());
        }
        public static void RegisterGizmos(StepType duration, Func<GizmoManager.GizmoObject> objectProvider, object gizmoDuration = null)
        {
            if (duration <= inst_.stepType)
                inst_.gizmos.Add(gizmoDuration ?? duration, objectProvider());
        }
        public static void RegisterGizmosIfExactly(StepType duration, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider, object gizmoDuration = null)
        {
            if (duration == inst_.stepType)
                inst_.gizmos.Add(gizmoDuration ?? duration, objectProvider());
        }

        public static void ExpireGizmos(object duration)
        {
            if (StepType.None < inst_.stepType)
                inst_.gizmos.Expire(duration);
        }
    }
}

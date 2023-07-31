using Data.WorldGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using World.WorldData;
using WorldGen.Blockers;
using WorldGen.Decorations;
using WorldGen.Path;
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
        [SerializeField] WorldData worldData;
        [SerializeField] WorldSettings.WorldSettings worldSettings;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathStartPicker pathStartPicker;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator wfc;
        [SerializeField] BlockerGenerator blockerGenerator;
        [SerializeField] PathFinalizer pathFinalizer;
        [SerializeField] Scatterer scatterer;
        [Header("Settings")]
        [SerializeField] int tries;

        [Header("Events")]
        [SerializeField] UnityEvent onGeneratedTerrain;
        [SerializeField] UnityEvent onFinalizedPaths;
        [SerializeField] UnityEvent onScatteredDecorations;

        //Runtime Values
        public static Random.Random Random { get; private set; }
        public static TerrainType TerrainType { get; private set; }
        public static TilesData Tiles { get; private set; }

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
        }

        async Task GenerateAsync()
        {
            worldData.Clear();

            TerrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            await Task.Yield();

            Vector2Int[] starts;
            WFCState terrain;
            while (true)
            {
                if (tries <= 0)
                    throw new("Failed to generate world");
                tries--;

                starts = await Task.Run(() => pathStartPicker.PickStarts(worldSettings.pathLengths));

                var paths = await Task.Run(() => pathPlanner.PlanPaths(starts, worldSettings.pathLengths));
                if (paths is null)
                    continue;

                terrain = await Task.Run(() => wfc.Generate(paths));
                if (terrain is null)
                    continue;

                Tiles = new(terrain.GetCollapsedSlots(), terrain.GetPassageAtTile, paths);
                break;
            }
            worldData.firstPathNodes = starts;
            worldData.pathStarts = starts.Select(s => s + WorldUtils.GetMainDir(WorldUtils.ORIGIN, s, Random)).ToArray();
            worldData.terrain = terrain.GetCollapsedSlots();
            worldData.tiles = Tiles;

            onGeneratedTerrain.Invoke();

            await Task.Run(() => blockerGenerator.PlaceBlockers(starts, worldSettings.pathLengths));
            await Task.Run(() => pathFinalizer.FinalizePaths(starts, worldSettings.maxExtraPaths));

            onFinalizedPaths.Invoke();

            await Task.Run(() => scatterer.Scatter());

            onScatteredDecorations.Invoke();

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

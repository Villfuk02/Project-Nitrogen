using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BattleSimulation.World.WorldData;
using Data.WorldGen;
using Game.Run;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using WorldGen.Decorations;
using WorldGen.Obstacles;
using WorldGen.Path;
using WorldGen.WFC;
using Debug = UnityEngine.Debug;
using Random = Utils.Random.Random;

namespace WorldGen
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Debug Stepping")]
        [SerializeField] bool stepped;
        [SerializeField] StepType stepType;
        [Header("References")]
        static WorldGenerator instance_;
        [SerializeField] WorldData worldData;
        [SerializeField] WorldSettings.WorldSettings worldSettings;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathEndPointPicker pathEndPointPicker;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator wfc;
        [SerializeField] ObstacleGenerator obstacleGenerator;
        [SerializeField] PathFinalizer pathFinalizer;
        [SerializeField] ObstacleModelScatterer obstacleModelScatterer;
        [Header("Settings")]
        [SerializeField] int tries;
        [SerializeField] UnityEvent onGeneratedTerrain;
        [SerializeField] UnityEvent onFinalizedPaths;
        [SerializeField] UnityEvent onScatteredDecorations;
        [Header("Benchmarking settings")]
        [SerializeField] bool benchmarkingMode;
        [SerializeField] int repeats;
        [Header("Runtime variables")]
        int wfcFails_;
        readonly object steppedLock_ = new();
        public static Random Random { get; private set; }
        public static TerrainType TerrainType { get; private set; }
        public static TilesData Tiles { get; private set; }

        readonly AutoResetEvent waitForStepEvent_ = new(false);

        public enum StepType { None, Phase, Step, MicroStep }

        public static readonly StepType[] STEP_TYPES = (StepType[])Enum.GetValues(typeof(StepType));

        void Awake()
        {
            instance_ = this;

            GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunPersistence>().SetupLevel();
            Random = new(worldSettings.seed);
            Tiles = null;
        }

        void Start()
        {
            if (benchmarkingMode)
                StartCoroutine(Benchmark());
            else
                StartCoroutine(Generate());
        }

        void Update()
        {
            HandleDebugStepping();
        }

        void HandleDebugStepping()
        {
            // Z for tap, H for hold
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKey(KeyCode.H))
                Step();

            if (stepType == StepType.None)
                return;

            // change the step type
            // these only work when we're stepping already
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
            worldData.Clear();
            worldData.seed = worldSettings.seed;

            // we prefer generation algorithms running on a background thread
            Task generatingTerrain = Task.Run(GenerateTerrain);
            yield return new WaitUntil(() => generatingTerrain.IsCompleted);
            generatingTerrain.Wait();

            // but some actions still have to run on the main thread
            onGeneratedTerrain.Invoke();

            Task placingObstacles = Task.Run(() => obstacleGenerator.PlaceObstacles());
            yield return new WaitUntil(() => placingObstacles.IsCompleted);
            placingObstacles.Wait();
            Task finalizingPaths = Task.Run(() => pathFinalizer.FinalizePaths(worldData.firstPathTiles, worldSettings.maxExtraPaths, worldData.hubPosition));
            yield return new WaitUntil(() => finalizingPaths.IsCompleted);
            finalizingPaths.Wait();

            onFinalizedPaths.Invoke();

            Task scattering = Task.Run(obstacleModelScatterer.Scatter);
            yield return new WaitUntil(() => scattering.IsCompleted);
            scattering.Wait();

            onScatteredDecorations.Invoke();

            print("SUCCESSFULLY GENERATED");
        }

        IEnumerator Benchmark()
        {
            if (!worldSettings.overrideRun)
                throw new WorldGeneratorException("World settings have to be be constant for benchmarking!");
            Debug.LogWarning("RUNNING IN BENCHMARK MODE");
            tries = 0;
            Stopwatch s = new();
            s.Start();
            for (int i = 0; i < repeats; i++)
            {
                worldData.Clear();
                worldData.seed = worldSettings.seed;
                Task generatingTerrain = Task.Run(GenerateTerrain);
                yield return new WaitUntil(() => generatingTerrain.IsCompleted);
                generatingTerrain.Wait();
                Debug.LogWarning($"generated world #{i + 1}");
            }

            Debug.LogError($"BENCHMARK COMPLETE!  worlds: {repeats}, attempts: {-tries}, WFC fails: {wfcFails_}, milliseconds: {s.ElapsedMilliseconds}");
        }

        void GenerateTerrain()
        {
            TerrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            Vector2Int[] pathStarts;
            Vector2Int hubPosition;
            WFCState terrain;
            while (true)
            {
                if (!benchmarkingMode && tries <= 0)
                    throw new WorldGeneratorException("Failed to generate terrain");
                tries--;

                pathEndPointPicker.PickEndPoints(worldSettings.maxHubDistFromCenter, worldSettings.pathLengths, out hubPosition, out pathStarts);

                pathPlanner.Init(pathStarts, worldSettings.pathLengths, hubPosition);

                var pathPrototypes = pathPlanner.PrototypePaths();
                var paths = pathPlanner.RefinePaths(pathPrototypes);
                if (paths is null)
                    continue;

                terrain = wfc.Generate(paths, hubPosition);
                if (terrain is null)
                {
                    wfcFails_++;
                    continue;
                }

                Tiles = new(terrain.GetCollapsedSlots(), terrain.GetPassageAtTile, paths);
                break;
            }

            worldData.hubPosition = hubPosition;
            worldData.firstPathTiles = pathStarts;
            worldData.pathStarts = pathStarts.Select(s => s + WorldUtils.GetMainDir(WorldUtils.WORLD_CENTER, s, Random)).ToArray();
            worldData.terrain = terrain.GetCollapsedSlots();
            worldData.tiles = Tiles;
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

        void ExpireGizmosBecauseOfStep(StepType type)
        {
            for (StepType t = type; t <= STEP_TYPES[^1]; t++)
            {
                gizmos.Expire(t);
            }
        }

        void SetStepped()
        {
            lock (steppedLock_)
            {
                stepped = true;
            }
        }

        /// <summary>
        /// If we're going by steps of type 'type' or smaller, blocks until next step is allowed.
        /// </summary>
        public static void WaitForStep(StepType type)
        {
            if (type > instance_.stepType)
                return;

            if (instance_.stepped)
                instance_.waitForStepEvent_.WaitOne();
            instance_.ExpireGizmosBecauseOfStep(type);
            instance_.SetStepped();
        }

        /// <summary>
        /// If we're going by steps of type 'stepType' or smaller, calls 'objectProvider' to generate gizmos to draw until <see cref="ExpireGizmos"/> with 'duration' is called.
        /// If 'duration' is null, uses 'stepType' as duration.
        /// Gizmos with duration of type <see cref="StepType"/> expire automatically whenever the corresponding step is taken.
        /// </summary>
        public static void RegisterGizmos(StepType stepType, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider, object duration = null)
        {
            if (stepType <= instance_.stepType)
                instance_.gizmos.Add(duration ?? stepType, objectProvider());
        }

        /// <summary>
        /// If we're going by steps of type 'stepType' or smaller, calls 'objectProvider' to generate a gizmo to draw until <see cref="ExpireGizmos"/> with 'duration' is called.
        /// If 'duration' is null, uses 'stepType' as duration.
        /// Gizmos with duration of type <see cref="StepType"/> expire automatically whenever the corresponding step is taken.
        /// </summary>
        public static void RegisterGizmos(StepType stepType, Func<GizmoManager.GizmoObject> objectProvider, object duration = null)
        {
            if (stepType <= instance_.stepType)
                instance_.gizmos.Add(duration ?? stepType, objectProvider());
        }

        /// <summary>
        /// If we're going by steps of type 'stepType', calls 'objectProvider' to generate gizmos to draw until <see cref="ExpireGizmos"/> with 'duration' is called.
        /// If 'duration' is null, uses 'stepType' as duration.
        /// Gizmos with duration of type <see cref="StepType"/> expire automatically whenever the corresponding step is taken.
        /// </summary>
        public static void RegisterGizmosIfExactly(StepType stepType, Func<IEnumerable<GizmoManager.GizmoObject>> objectProvider, object duration = null)
        {
            if (stepType == instance_.stepType)
                instance_.gizmos.Add(duration ?? stepType, objectProvider());
        }

        /// <summary>
        /// Stops gizmos registered with duration 'duration' from drawing from now on.
        /// </summary>
        public static void ExpireGizmos(object duration)
        {
            if (StepType.None < instance_.stepType)
                instance_.gizmos.Expire(duration);
        }

        public class WorldGeneratorException : Exception
        {
            public WorldGeneratorException(string message) : base(message) { }
        }
    }
}
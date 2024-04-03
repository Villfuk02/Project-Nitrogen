using BattleSimulation.World.WorldData;
using Data.WorldGen;
using Game.Run;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using WorldGen.Decorations;
using WorldGen.Obstacles;
using WorldGen.Path;
using WorldGen.WFC;
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
        [SerializeField] PathStartPicker pathStartPicker;
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

        [Header("Runtime variables")]
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
            StartCoroutine(Generate());
        }

        void Update()
        {
            HandleDebugStepping();
        }

        void HandleDebugStepping()
        {
            // T for tap, H for hold
            if (Input.GetKeyDown(KeyCode.T) || Input.GetKey(KeyCode.H))
                Step();

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
            worldData.Clear();
            worldData.seed = worldSettings.seed;

            Task generatingTerrain = Task.Run(GenerateTerrain);
            yield return new WaitUntil(() => generatingTerrain.IsCompleted);
            generatingTerrain.Wait();

            onGeneratedTerrain.Invoke();

            Task placingObstacles = Task.Run(() => obstacleGenerator.PlaceObstacles(worldData.firstPathTiles, worldSettings.pathLengths));
            yield return new WaitUntil(() => placingObstacles.IsCompleted);
            placingObstacles.Wait();
            Task finalizingPaths = Task.Run(() => pathFinalizer.FinalizePaths(worldData.firstPathTiles, worldSettings.maxExtraPaths));
            yield return new WaitUntil(() => finalizingPaths.IsCompleted);
            finalizingPaths.Wait();

            onFinalizedPaths.Invoke();

            Task scattering = Task.Run(obstacleModelScatterer.Scatter);
            yield return new WaitUntil(() => scattering.IsCompleted);
            scattering.Wait();

            onScatteredDecorations.Invoke();

            print("SUCCESSFULLY GENERATED");
        }

        void GenerateTerrain()
        {
            TerrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            Vector2Int[] pathStarts;
            WFCState terrain;
            while (true)
            {
                if (tries <= 0)
                    throw new WorldGeneratorException("Failed to generate terrain");
                tries--;

                pathStarts = pathStartPicker.PickStarts(worldSettings.pathLengths);

                var paths = pathPlanner.PlanPaths(pathStarts, worldSettings.pathLengths);
                if (paths is null)
                    continue;

                terrain = wfc.Generate(paths);
                if (terrain is null)
                    continue;

                Tiles = new(terrain.GetCollapsedSlots(), terrain.GetPassageAtTile, paths);
                break;
            }

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
        /// <summary>
        /// If we're going by steps of type 'type' or smaller, blocks the thread until next step is allowed.
        /// </summary>
        public static void WaitForStep(StepType type)
        {
            if (type > instance_.stepType)
                return;

            if (instance_.stepped)
                instance_.waitForStepEvent_.WaitOne();
            instance_.GizmoStep(type);
            instance_.Stepped();
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

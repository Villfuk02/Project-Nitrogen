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
        [SerializeField] Scatterer scatterer;
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
            if (instance_ == null)
                instance_ = this;
            else
                throw new("There can be only one instance of WorldGenerator");

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
            Task generating = GenerateAsync();
            yield return new WaitUntil(() => generating.IsCompleted);
            // make sure we don't miss any exceptions
            generating.Wait();
        }

        async Task GenerateAsync()
        {
            worldData.Clear();
            worldData.seed = worldSettings.seed;

            TerrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            await Task.Yield();

            Vector2Int[] starts;
            WFCState terrain;
            while (true)
            {
                if (tries <= 0)
                    throw new("Failed to generate a world");
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
            worldData.firstPathTiles = starts;
            worldData.pathStarts = starts.Select(s => s + WorldUtils.GetMainDir(WorldUtils.WORLD_CENTER, s, Random)).ToArray();
            worldData.terrain = terrain.GetCollapsedSlots();
            worldData.tiles = Tiles;

            onGeneratedTerrain.Invoke();

            await Task.Run(() => obstacleGenerator.PlaceObstacles(starts, worldSettings.pathLengths));
            await Task.Run(() => pathFinalizer.FinalizePaths(starts, worldSettings.maxExtraPaths));

            onFinalizedPaths.Invoke();

            await Task.Run(() => scatterer.Scatter());

            onScatteredDecorations.Invoke();

            print("SUCCESSFULLY GENERATED");
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
    }
}

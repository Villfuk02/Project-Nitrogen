using Data.LevelGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;
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
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] WorldSettings.WorldSettings worldSettings;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathStartPicker pathStartPicker;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator WFC;
        //[SerializeField] BlockerGenerator blockerGenerator;
        //[SerializeField] Scatterer.Scatterer scatterer;
        [Header("Settings")]
        [SerializeField] int tries;

        //Runtime Values
        public static Random.Random Random { get; private set; }
        public static TerrainType TerrainType { get; private set; }

        readonly AutoResetEvent waitForStepEvent_ = new(false);
        readonly object steppedLock_ = new();
        //LevelGenTiles tiles;
        //public static LevelGenTiles Tiles { get => inst.tiles; }

        public enum StepType { None, Phase, Step, MicroStep }
        public static readonly StepType[] STEP_TYPES = (StepType[])Enum.GetValues(typeof(StepType));

        void Awake()
        {
            if (inst_ == null)
                inst_ = this;
            else
                Debug.LogError("There can be only one!");
            worldData.Reset();
            Random = new(worldSettings.seed);
        }
        void Start()
        {
            StartCoroutine(Generate());
        }

        void Update()
        {
            // DON'T STOP ME NOOOOOW
            if (stepType == StepType.None)
                return;

            if (Input.GetKeyDown(KeyCode.T) || Input.GetKey(KeyCode.H))
                Step();

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
            worldData.Reset();

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

                var terrain = await Task.Run(() => WFC.Generate(paths));
                if (terrain is null)
                    continue;

                /*               
                !!! 2d arrays !!!
                bool[,] passable = new bool[(WORLD_SIZE.x + 1) * (WORLD_SIZE.y + 1), 4];
                Slant[] slants = new Slant[(WORLD_SIZE.x + 1) * (WORLD_SIZE.y + 1)];
                foreach (Vector2Int v in WORLD_SIZE)
                {
                    int index = (v.x + 1) + v.y * (WORLD_SIZE.x + 1);
                    WFCModule m = WFCGenerator.ALL_MODULES[modules[index]];
                    slants[index] = m.slants[0];
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int pos = v + CARDINAL_DIRS[i];
                        Vector2Int pp = i / 2 == 0 ? v + Vector2Int.one : v;
                        if (pos.x >= 0 && pos.y >= 0 && pos.x < WORLD_SIZE.x && pos.y < WORLD_SIZE.y
                            && WFCGenerator.ALL_MODULES[modules[pp.x + pp.y * (WORLD_SIZE.x + 1)]].passable[3 - i])
                            passable[index, i] = true; 
                    }
                }
                tiles = new(passable, heights, slants, nodes);
                WORLD_DATA.tiles = tiles;
                int[,] modules2d = new int[WORLD_SIZE.x + 1, WORLD_SIZE.y + 1];
                int[,] heights2d = new int[WORLD_SIZE.x + 1, WORLD_SIZE.y + 1];
                foreach (Vector2Int v in WORLD_SIZE + Vector2Int.one)
                {
                    int index = v.x + v.y * (WORLD_SIZE.x + 1);
                    modules2d[v.x, v.y] = modules[index];
                    heights2d[v.x, v.y] = heights[index];
                }
                WORLD_DATA.modules = modules2d;
                WORLD_DATA.moduleHeights = heights2d;
                */
                break;
            }
            /*
            JobDataInterface placeBlockers = blockerGenerator.PlaceBlockers(targets, pathPlanner.targetLengths);
            yield return new WaitUntil(() => placeBlockers.IsFinished);
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
        void Stepped(StepType type)
        {
            for (StepType t = type; t <= STEP_TYPES[^1]; t++)
            {
                gizmos.Expire(t);
            }
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
            inst_.Stepped(type);
        }

        static IEnumerator ProcessTask<T>(Func<T> func)
        {
            var task = Task.Run(func);
            yield return new WaitUntil(() => task.IsCompleted);
            bool success = task.IsCompletedSuccessfully;
            T result = task.Result;
            success &= result is not null;
            yield return success;
            if (success)
                yield return result;
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

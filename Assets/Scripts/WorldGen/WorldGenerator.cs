using Data.LevelGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Utils;
using WorldGen.Path;
using WorldGen.WFC;
using static Utils.TimingUtils;

namespace WorldGen
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Debug Stepping")]
        [SerializeField] bool stepped;
        [SerializeField] StepType stepType;
        [Header("References")]
        public static WorldGenerator inst;
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] WorldSettings.WorldSettings worldSettings;
        [SerializeField] GizmoManager gizmos;
        [SerializeField] PathPlanner pathPlanner;
        [SerializeField] WFCGenerator WFC;
        //[SerializeField] BlockerGenerator blockerGenerator;
        //[SerializeField] Scatterer.Scatterer scatterer;
        //[Header("Settings")]
        [Header("Runtime Values")]
        public Random.Random random;
        public TerrainType terrainType;
        //LevelGenTiles tiles;
        //public static LevelGenTiles Tiles { get => inst.tiles; }

        void Awake()
        {
            if (inst == null)
                inst = this;
            else
                Debug.LogError("There can be only one!");
            worldData.Reset();
            random = new(worldSettings.seed);
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
                stepped = false;

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
            worldData.Reset();

            terrainType = TerrainTypes.GetTerrainType(worldSettings.terrainType);

            pathPlanner.Init(worldSettings.pathLengths, random.NewSeed());
            WFC.Init(terrainType, random.NewSeed());

            //blockerGenerator.Prepare();
            //scatterer.Prepare();
            Vector2Int[] starts;
            do
            {
                JobDataInterface pickStarts = pathPlanner.PickStarts(out starts);
                yield return new WaitUntil(() => pickStarts.IsFinished);

                JobDataInterface pickPaths = pathPlanner.PlanPaths(starts, out var flatPaths);
                yield return new WaitUntil(() => pickPaths.IsFinished);
                if (pickPaths.Failed)
                    continue;
                pathPlanner.UnpackPlannedPaths(flatPaths, out var paths);

                JobDataInterface wfcGenerate = WFC.Generate(paths, out var modules, out var heights);
                yield return new WaitUntil(() => wfcGenerate.IsFinished);
                if (wfcGenerate.Failed)
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
            } while (true);
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
            Debug.Log("DONE");
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

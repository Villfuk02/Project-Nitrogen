using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.LevelGenerator;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer
{
    public class Scatterer : MonoBehaviour
    {
        [SerializeField] ScattererObjectModule[] SOMSetup;
        public static ScattererObjectModule[] SCATTERER_MODULES;
        static bool isDone = false;
        static readonly RandomSet<Vector2Int> allTiles = new();

        public void Prepare()
        {
            SCATTERER_MODULES = SOMSetup;
            allTiles.Clear();
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                {
                    allTiles.Add(new(x, y));
                }
            }
        }

        public JobDataInterface Scatter(out List<int> typeCounts, out List<Vector2> positions, out List<float> scales)
        {
            JobDataInterface jobData = new(Allocator.Persistent);
            isDone = false;

            JobHandle handle = new ScattererMonitorJob().Schedule();
            jobData.RegisterHandle(this, handle);

            typeCounts = new();
            positions = new();
            scales = new();
            StartCoroutine(ScatterMain(typeCounts, positions, scales));

            return jobData;
        }

        struct ScattererMonitorJob : IJob
        {
            public void Execute()
            {
                while (!isDone)
                {
                    Thread.Sleep(15);
                }
            }
        }

        IEnumerator ScatterMain(List<int> typeCounts, List<Vector2> positions, List<float> scales)
        {
            while (!CanStep(StepType.Phase))
            {
                yield return null;
            }
            Debug.Log("Scattering");
            Dictionary<Vector2Int, List<Vector3>> colliders = new();
            for (int i = 0; i < SCATTERER_MODULES.Length; i++)
            {
                List<Vector2> stepPositions = new();
                List<float> stepScales = new();
                yield return ScatterStep(i, colliders, stepPositions, stepScales);
                positions.AddRange(stepPositions);
                scales.AddRange(stepScales);
                typeCounts.Add(stepPositions.Count);
            }
            Debug.Log("Scattered");
            isDone = true;
            yield break;
        }

        IEnumerator ScatterStep(int index, Dictionary<Vector2Int, List<Vector3>> colliders, List<Vector2> positions, List<float> scales)
        {
            ScattererObjectModule som = SCATTERER_MODULES[index];
            if (som.enabled)
            {
                while (!CanStep(StepType.Step))
                {
                    yield return null;
                }
                Debug.Log($"Step {index}");
                Dictionary<Vector2Int, List<Vector3>> tempColliders = new();
                RandomSet<Vector2Int> tilesLeft = new(allTiles);
                while (tilesLeft.Count > 0)
                {
                    while (!CanStep(StepType.Substep))
                    {
                        yield return null;
                    }
                    RandomSet<Vector2Int> leftToTry = new(tilesLeft);
                    List<Vector2Int> selected = new();
                    HashSet<Vector2Int> blocked = new();
                    while (leftToTry.Count > 0)
                    {
                        Vector2Int pos = leftToTry.PopRandom();
                        if (!blocked.Contains(pos))
                        {
                            selected.Add(pos);
                            tilesLeft.Remove(pos);
                            foreach (Vector2Int d in WorldUtils.ADJACENT_DIRS)
                            {
                                blocked.Add(pos + d);
                            }
                        }
                    }
                    RegisterGizmos(StepType.Substep, () => selected.Select((p) => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(p), 0.6f)));
                    List<(JobDataInterface job, List<Vector2> generated, List<Vector2> colliderSizes, List<float> tileScales)> jobs = new();
                    foreach (var tile in selected)
                    {
                        JobDataInterface job = ScatterTiles(index, tile, colliders, tempColliders, out List<Vector2> generated, out List<Vector2> colliderSizes, out List<float> tileScales);
                        jobs.Add((job, generated, colliderSizes, tileScales));
                    }
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        while (!jobs[i].job.IsFinished)
                        {
                            yield return null;
                        }
                        List<Vector3> pCol = new();
                        List<Vector3> tCol = new();
                        for (int j = 0; j < jobs[i].generated.Count; j++)
                        {
                            Vector2 p = jobs[i].generated[j];
                            positions.Add(p);
                            scales.Add(jobs[i].tileScales[j]);
                            Vector2 col = jobs[i].colliderSizes[j];
                            if (col.x > 0)
                                pCol.Add(new(p.x, p.y, col.x));
                            if (col.y > 0 && col.y > col.x)
                                tCol.Add(new(p.x, p.y, col.y));
                        }
                        if (colliders.ContainsKey(selected[i]))
                            colliders[selected[i]].AddRange(pCol);
                        else
                            colliders.Add(selected[i], pCol);
                        tempColliders.Add(selected[i], tCol);
                    }
                }
            }
            yield break;
        }

        JobDataInterface ScatterTiles(int index, Vector2Int tile, Dictionary<Vector2Int, List<Vector3>> colliders, Dictionary<Vector2Int, List<Vector3>> tempColliders, out List<Vector2> generated, out List<Vector2> colliderSizes, out List<float> scales)
        {
            JobDataInterface jobData = new(Allocator.Persistent);
            generated = new();
            colliderSizes = new();
            scales = new();
            Dictionary<Vector2, Vector3> merging = new();
            foreach (Vector2Int d in WorldUtils.ADJACENT_AND_ZERO)
            {
                Vector2Int t = tile + d;
                if (tempColliders.ContainsKey(t))
                {
                    foreach (Vector3 col in tempColliders[t])
                    {
                        merging.Add(new(col.x, col.y), col);
                    }
                }
                if (colliders.ContainsKey(t))
                {
                    foreach (Vector3 col in colliders[t])
                    {
                        Vector2 p = new(col.x, col.y);
                        if (!merging.ContainsKey(p))
                            merging.Add(p, col);
                    }
                }
            }
            JobHandle handle = new ScatterTileJob
            {
                SOMIndex = index,
                tile = tile,
                generated = jobData.Register(generated, true),
                colliderSizes = jobData.Register(colliderSizes, true),
                scales = jobData.Register(scales, true),
                colliders = jobData.Register(merging.Select((p) => p.Value).ToArray(), false)
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct ScatterTileJob : IJob
        {
            public int SOMIndex;
            public Vector2Int tile;
            public NativeList<Vector2> generated;
            public NativeList<Vector2> colliderSizes;
            public NativeList<float> scales;
            public NativeArray<Vector3> colliders;
            public void Execute()
            {
                generated.Add(tile);
                colliderSizes.Add(new(0.3f, 0.4f));
                scales.Add(0.2f);
            }
        }
    }
}
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Path;
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
        //[Header("Settings")]
        //[Header("Runtime Values")]
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
            do
            {
                (JobDataInterface pickTargets, Vector2Int[] targets) = pathPlanner.PickTargets();
                yield return new WaitUntil(() => pickTargets.IsFinished);
                (JobDataInterface pickPaths, int[] nodes) = pathPlanner.PickPaths(targets);
                yield return new WaitUntil(() => pickPaths.IsFinished);
                if (pickPaths.Failed)
                {
                    continue;
                }
                (JobDataInterface WFCGenerate, int[] modules, int[] heights) = WFC.Generate(nodes);
                yield return new WaitUntil(() => WFCGenerate.IsFinished);
                if (WFCGenerate.Failed)
                {
                    continue;
                }
                Debug.Log("DONE");
                RegisterGizmos(StepType.None, () =>
                {
                    int sampleCount = 2000;
                    GizmoManager.GizmoObject[] gizmos = new GizmoManager.GizmoObject[sampleCount];
                    for (int i = 0; i < sampleCount; i++)
                    {
                        Vector2Int slot = new(UnityEngine.Random.Range(0, WorldUtils.WORLD_SIZE.x + 1), UnityEngine.Random.Range(0, WorldUtils.WORLD_SIZE.y + 1));
                        Vector2 offset = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value) - Vector2.one * 0.5f;
                        int index = slot.x + slot.y * (WorldUtils.WORLD_SIZE.x + 1);
                        float h = WFCGenerator.ALL_MODULES[modules[index]].GetBaseHeight(offset.x, offset.y) + heights[index];
                        Vector3 box = WorldUtils.SlotToWorldPos(slot.x + offset.x, slot.y + offset.y, h);
                        gizmos[i] = new GizmoManager.Cube(Color.red, box, 0.1f);
                    }
                    return gizmos;
                });

                yield break;
            } while (true);

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

        bool CanStep(StepType type)
        {
            if (type <= stepType)
            {
                if (stepped)
                    return false;
                for (StepType t = type; t <= STEP_TYPES[^1]; t++)
                {
                    gizmos.Expire(t);
                }
                stepped = true;
            }
            return true;
        }

        public static void WaitForStep(StepType type)
        {
            while (!inst.CanStep(type))
            {
                Thread.Sleep(15);
            }
        }
    }
}

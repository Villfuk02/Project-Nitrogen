using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Path;
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
            PlannedPath[] paths;
            do
            {
                (JobDataInterface pickTargets, Vector2Int[] targets) = pathPlanner.PickTargets();
                yield return new WaitUntil(() => pickTargets.IsFinished);
                (JobDataInterface pickPaths, PlannedPath[] ps) = pathPlanner.PickPaths(targets);
                yield return new WaitUntil(() => pickPaths.IsFinished);
                paths = ps;
            } while (paths == null);
        }
        IEnumerator Animate()
        {
            yield break;
        }

        public static void RegisterGizmos(StepType duration, Func<List<GizmoManager.GizmoObject>> objectProvider)
        {
            if (duration <= inst.stepType)
            {
                inst.gizmos.Add(duration, objectProvider());
            }
        }
        public static void RegisterGizmos(StepType duration, Func<GizmoManager.GizmoObject[]> objectProvider)
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
